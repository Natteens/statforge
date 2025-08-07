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
        [SerializeField] private float baseValue;
        [SerializeField] private float bonusValue;
        
        private float temporaryBonus;
        private List<StatModifier> modifiers;
        
        public event Action<Stat> OnValueChanged;
        
        public StatType StatType 
        { 
            get => statType; 
            set 
            {
                statType = value;
                if (statType != null && baseValue == 0f)
                    baseValue = statType.DefaultValue;
                OnValueChanged?.Invoke(this);
            }
        }
        
        public float BaseValue 
        { 
            get => baseValue; 
            set 
            {
                var clampedValue = statType != null ? statType.ClampValue(value) : value;
                if (!Mathf.Approximately(baseValue, clampedValue))
                {
                    baseValue = clampedValue;
                    OnValueChanged?.Invoke(this);
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
                    bonusValue = value;
                    OnValueChanged?.Invoke(this);
                }
            }
        }
        
        public float TemporaryBonus => temporaryBonus;
        
        public float Value 
        {
            get 
            {
                float total = CalculateValue();
                return statType != null ? statType.ClampValue(total) : total;
            }
            set 
            {
                BaseValue = value;
            }
        }
        
        public string Name => statType?.DisplayName ?? "Unknown";
        public string ShortName => statType?.ShortName ?? "";
        public string Abbreviation => statType?.Abbreviation ?? "";
        
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
        
        public Stat(StatType type, float initialValue) : this(type)
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
                modifiers.Add(modifier);
                modifier.Activate();
                modifier.OnExpired += OnModifierExpired;
                OnValueChanged?.Invoke(this);
            }
        }
        
        public void RemoveModifier(StatModifier modifier)
        {
            if (modifiers.Remove(modifier))
            {
                modifier.OnExpired -= OnModifierExpired;
                OnValueChanged?.Invoke(this);
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
        public bool IsValid => statType != null;
        
        public float GetPercentage()
        {
            if (statType == null) return 0f;
            float range = statType.MaxValue - statType.MinValue;
            if (range <= 0f) return 1f;
            return Mathf.Clamp01((Value - statType.MinValue) / range);
        }
        
        public void SetPercentage(float percentage)
        {
            if (statType == null) return;
            percentage = Mathf.Clamp01(percentage);
            float range = statType.MaxValue - statType.MinValue;
            BaseValue = statType.MinValue + (range * percentage);
        }
        
        public override string ToString()
        {
            if (statType == null) return "Invalid Stat";
            return $"{Name}: {Value:F1}";
        }
        
        // Static utility methods
        public static implicit operator float(Stat stat)
        {
            return stat?.Value ?? 0f;
        }
    }
}