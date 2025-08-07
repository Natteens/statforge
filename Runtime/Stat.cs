using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public class Stat
    {
        [SerializeField] private StatType statType;
        [SerializeField] private StatDefinition statDefinition;
        [SerializeField] private float baseValue;
        [SerializeField] private float bonusValue;
        
        private float temporaryBonus;
        private List<StatModifier> modifiers;
        private StatEvents.StatEventHandler eventHandler;
        
        public event Action<Stat> OnValueChanged;
        public StatEvents.StatEventHandler Events => eventHandler ??= new StatEvents.StatEventHandler(this);
        
        public StatType StatType 
        { 
            get => statType ?? statDefinition?.ToStatType(); 
            set 
            {
                statType = value;
                statDefinition = null; // Clear definition when setting type
                if (StatType != null && baseValue == 0f)
                    baseValue = StatType.DefaultValue;
                OnValueChanged?.Invoke(this);
            }
        }
        
        public StatDefinition StatDefinition
        {
            get => statDefinition;
            set
            {
                statDefinition = value;
                statType = null; // Clear type when setting definition
                if (StatDefinition != null && baseValue == 0f)
                    baseValue = StatDefinition.DefaultValue;
                OnValueChanged?.Invoke(this);
            }
        }
        
        // Unified access to the current stat info (StatType or StatDefinition)
        private object CurrentStat => (object)statDefinition ?? statType;
        
        public float BaseValue 
        { 
            get => baseValue; 
            set 
            {
                var oldValue = Value;
                var currentStat = StatType ?? StatDefinition?.ToStatType();
                var clampedValue = currentStat != null ? currentStat.ClampValue(value) : value;
                if (!Mathf.Approximately(baseValue, clampedValue))
                {
                    baseValue = clampedValue;
                    var newValue = Value;
                    OnValueChanged?.Invoke(this);
                    Events?.NotifyValueChanged(oldValue, newValue);
                    StatEvents.Global.NotifyValueChanged(this, oldValue, newValue);
                }
            }
        }
        
        public float BonusValue 
        { 
            get => bonusValue; 
            set 
            {
                if (!Mathf.Approximately(bonusValue, value))
                {
                    var oldValue = Value;
                    bonusValue = value;
                    var newValue = Value;
                    OnValueChanged?.Invoke(this);
                    Events?.NotifyValueChanged(oldValue, newValue);
                    StatEvents.Global.NotifyValueChanged(this, oldValue, newValue);
                }
            }
        }
        
        public float TemporaryBonus => temporaryBonus;
        
        public float Value 
        {
            get 
            {
                float total = CalculateValue();
                var currentStat = StatType ?? StatDefinition?.ToStatType();
                return currentStat != null ? currentStat.ClampValue(total) : total;
            }
            set 
            {
                BaseValue = value;
            }
        }
        
        public string Name => StatDefinition?.DisplayName ?? StatType?.DisplayName ?? "Unknown";
        public string ShortName => StatDefinition?.ShortName ?? StatType?.ShortName ?? "";
        public string Abbreviation => StatDefinition?.Abbreviation ?? StatType?.Abbreviation ?? "";
        public Color StatColor => StatDefinition?.StatColor ?? Color.white;
        public Sprite Icon => StatDefinition?.Icon;
        
        // Constructors
        public Stat()
        {
            baseValue = 0f;
            bonusValue = 0f;
            temporaryBonus = 0f;
            modifiers = new List<StatModifier>();
        }
        
        public Stat(StatType type) : this()
        {
            StatType = type;
        }
        
        public Stat(StatDefinition definition) : this()
        {
            StatDefinition = definition;
        }
        
        public Stat(StatType type, float initialValue) : this(type)
        {
            BaseValue = initialValue;
        }
        
        public Stat(StatDefinition definition, float initialValue) : this(definition)
        {
            BaseValue = initialValue;
        }
        
        // Value calculation with modifiers
        private float CalculateValue()
        {
            UpdateModifiers();
            
            float result = baseValue + bonusValue + temporaryBonus;
            
            // Apply flat modifiers first
            var flatModifiers = modifiers.Where(m => m.IsActive && m.Type == StatModifier.ModifierType.Flat);
            foreach (var modifier in flatModifiers)
            {
                result += modifier.Value;
            }
            
            // Apply percentage modifiers
            var percentageModifiers = modifiers.Where(m => m.IsActive && m.Type == StatModifier.ModifierType.Percentage);
            foreach (var modifier in percentageModifiers)
            {
                result *= (1f + modifier.Value / 100f);
            }
            
            // Apply override modifiers (last one wins)
            var overrideModifier = modifiers.Where(m => m.IsActive && m.Type == StatModifier.ModifierType.Override).LastOrDefault();
            if (overrideModifier != null)
            {
                result = overrideModifier.Value;
            }
            
            return result;
        }
        
        private void UpdateModifiers()
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                modifiers[i].UpdateModifier();
                if (!modifiers[i].IsActive && !modifiers[i].IsPermanent)
                {
                    modifiers.RemoveAt(i);
                }
            }
        }
        
        // Basic methods
        public void SetValue(float value)
        {
            BaseValue = value;
        }
        
        public void AddBonus(float bonus)
        {
            BonusValue += bonus;
        }
        
        public void RemoveBonus(float bonus)
        {
            BonusValue -= bonus;
        }
        
        public void SetBonus(float bonus)
        {
            BonusValue = bonus;
        }
        
        public void AddTemporaryBonus(float bonus, float duration = 0f)
        {
            if (duration > 0f)
            {
                var modifier = new StatModifier(bonus, StatModifier.ModifierType.Flat, duration, "Temporary");
                AddModifier(modifier);
            }
            else
            {
                temporaryBonus += bonus;
                OnValueChanged?.Invoke(this);
            }
        }
        
        public void RemoveTemporaryBonus(float bonus)
        {
            temporaryBonus -= bonus;
            OnValueChanged?.Invoke(this);
        }
        
        public void SetTemporaryBonus(float bonus)
        {
            temporaryBonus = bonus;
            OnValueChanged?.Invoke(this);
        }
        
        public void ClearTemporaryBonus()
        {
            temporaryBonus = 0f;
            OnValueChanged?.Invoke(this);
        }
        
        // Modifier methods
        public void AddModifier(StatModifier modifier)
        {
            if (modifier != null)
            {
                var oldValue = Value;
                modifiers.Add(modifier);
                modifier.Activate();
                modifier.OnExpired += OnModifierExpired;
                var newValue = Value;
                OnValueChanged?.Invoke(this);
                Events?.NotifyModifierAdded(modifier);
                Events?.NotifyValueChanged(oldValue, newValue);
                StatEvents.Global.NotifyModifierAdded(this, modifier);
                StatEvents.Global.NotifyValueChanged(this, oldValue, newValue);
            }
        }
        
        public void RemoveModifier(StatModifier modifier)
        {
            if (modifiers.Remove(modifier))
            {
                var oldValue = Value;
                modifier.OnExpired -= OnModifierExpired;
                var newValue = Value;
                OnValueChanged?.Invoke(this);
                Events?.NotifyModifierRemoved(modifier);
                Events?.NotifyValueChanged(oldValue, newValue);
                StatEvents.Global.NotifyModifierRemoved(this, modifier);
                StatEvents.Global.NotifyValueChanged(this, oldValue, newValue);
            }
        }
        
        public void RemoveModifiersFromSource(string source)
        {
            var toRemove = modifiers.Where(m => m.Source == source).ToList();
            foreach (var modifier in toRemove)
            {
                RemoveModifier(modifier);
            }
        }
        
        public void ClearAllModifiers()
        {
            foreach (var modifier in modifiers)
            {
                modifier.OnExpired -= OnModifierExpired;
            }
            modifiers.Clear();
            OnValueChanged?.Invoke(this);
        }
        
        public List<StatModifier> GetActiveModifiers()
        {
            UpdateModifiers();
            return modifiers.Where(m => m.IsActive).ToList();
        }
        
        private void OnModifierExpired(StatModifier modifier)
        {
            RemoveModifier(modifier);
        }
        
        // Utility methods
        public bool IsValid => StatType != null || StatDefinition != null;
        
        public float GetPercentage()
        {
            var currentStat = StatType ?? StatDefinition?.ToStatType();
            if (currentStat == null) return 0f;
            float range = currentStat.MaxValue - currentStat.MinValue;
            if (range <= 0f) return 1f;
            return Mathf.Clamp01((Value - currentStat.MinValue) / range);
        }
        
        public void SetPercentage(float percentage)
        {
            var currentStat = StatType ?? StatDefinition?.ToStatType();
            if (currentStat == null) return;
            percentage = Mathf.Clamp01(percentage);
            float range = currentStat.MaxValue - currentStat.MinValue;
            BaseValue = currentStat.MinValue + (range * percentage);
        }
        
        public override string ToString()
        {
            if (!IsValid) return "Invalid Stat";
            return $"{Name}: {Value:F1}";
        }
        
        // Static utility methods
        public static implicit operator float(Stat stat)
        {
            return stat?.Value ?? 0f;
        }
    }
}