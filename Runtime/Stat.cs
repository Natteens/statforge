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
        [SerializeField] private string statId;
        
        private float cachedValue;
        private bool needsRecalculation = true;
        private List<IStatModifier> modifiers = new List<IStatModifier>();
        
        private static readonly Dictionary<string, StatRegistry> registriesByStatId = new(512);
        private static readonly Dictionary<string, List<Stat>> dependentsCache = new(256);
        private static readonly List<IStatModifier> tempModifierList = new(32);
        
        [NonSerialized] private StatContainer parentContainer;
        [NonSerialized] private float lastSerializedValue = float.MinValue;
        [NonSerialized] private bool hasOverrideCache;
        [NonSerialized] private int modifierVersion;
        [NonSerialized] private float lastCalculatedBaseValue = float.MinValue;
        
        public StatType StatType => statType;
        public string Name => statType ? statType.DisplayName : "None";
        public string ShortName => statType ? statType.ShortName : "";
        public string Id 
        { 
            get 
            { 
                if (string.IsNullOrEmpty(statId))
                    statId = System.Guid.NewGuid().ToString();
                return statId; 
            } 
        }
        public IReadOnlyList<IStatModifier> Modifiers => modifiers;
        
        public float BaseValue 
        { 
            get 
            {
                if (Application.isPlaying && Math.Abs(lastSerializedValue - baseValue) > float.Epsilon)
                {
                    lastSerializedValue = baseValue;
                    InvalidateCache();
                }
                return baseValue;
            }
            set 
            {
                if (Math.Abs(baseValue - value) > float.Epsilon)
                {
                    baseValue = value;
                    lastSerializedValue = value;
                    InvalidateCache();
                }
            }
        }
        
        public float Value 
        {
            get
            {
                if (needsRecalculation || Math.Abs(lastCalculatedBaseValue - baseValue) > float.Epsilon)
                    RecalculateValue();
                return cachedValue;
            }
        }
        
        public event Action<Stat, float, float> OnValueChanged;
        public event Action<Stat, IStatModifier> OnModifierAdded;
        public event Action<Stat, IStatModifier> OnModifierRemoved;
        
        public Stat() 
        {
            statId = System.Guid.NewGuid().ToString();
            lastSerializedValue = baseValue;
        }
        
        public Stat(StatType type, float value = 0f, string customId = null)
        {
            statType = type;
            baseValue = value;
            statId = customId ?? System.Guid.NewGuid().ToString();
            lastSerializedValue = value;
            lastCalculatedBaseValue = value;
        }
        
        public void SetContainer(StatContainer container)
        {
            parentContainer = container;
            InvalidateCache();
        }
        
        private void InvalidateCache()
        {
            needsRecalculation = true;
            NotifyDependentStats();
        }
        
        private void NotifyDependentStats()
        {
            if (dependentsCache.TryGetValue(Id, out var dependents))
            {
                for (int i = 0; i < dependents.Count; i++)
                {
                    var dependent = dependents[i];
                    if (dependent != null)
                        dependent.needsRecalculation = true;
                }
                return;
            }
            
            parentContainer?.NotifyStatChanged(this);
            
            var owner = GetOwner();
            if (owner != null)
            {
                EnsureRegistry(owner);
                if (registriesByStatId.TryGetValue(Id, out var registry))
                {
                    registry.NotifyStatChanged(this);
                }
            }
        }
        
        private void RecalculateValue()
        {
            if (!statType)
            {
                cachedValue = 0f;
                needsRecalculation = false;
                lastCalculatedBaseValue = baseValue;
                return;
            }
            
            var oldValue = cachedValue;
            float newValue = CalculateFinalValueOptimized();
            
            if (!hasOverrideCache)
            {
                newValue = Mathf.Clamp(newValue, statType.MinValue, statType.MaxValue);
            }
            
            cachedValue = newValue;
            needsRecalculation = false;
            lastCalculatedBaseValue = baseValue;
            
            if (Math.Abs(oldValue - newValue) > float.Epsilon)
            {
                OnValueChanged?.Invoke(this, oldValue, newValue);
            }
        }
        
        private float CalculateFinalValueOptimized()
        {
            var workingValue = baseValue;
            
            if (statType.HasFormula)
            {
                workingValue += EvaluateFormula();
            }
            
            return ApplyModifiersOptimized(workingValue);
        }
        
        private float ApplyModifiersOptimized(float inputValue)
        {
            UpdateExpiredModifiersOptimized();
            
            if (modifiers.Count == 0)
            {
                hasOverrideCache = false;
                return inputValue;
            }
            
            IStatModifier overrideModifier = null;
            float additiveSum = 0f;
            float multiplicativeProduct = 1f;
            float percentageSum = 0f;
            
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                
                switch (modifier.Type)
                {
                    case ModifierType.Override:
                        if (overrideModifier == null || modifier.Priority >= overrideModifier.Priority)
                            overrideModifier = modifier;
                        break;
                    case ModifierType.Additive:
                        additiveSum += modifier.Value;
                        break;
                    case ModifierType.Subtractive:
                        additiveSum -= modifier.Value;
                        break;
                    case ModifierType.Multiplicative:
                        multiplicativeProduct *= modifier.Value;
                        break;
                    case ModifierType.Percentage:
                        percentageSum += modifier.Value;
                        break;
                }
            }
            
            hasOverrideCache = overrideModifier != null;
            
            if (overrideModifier != null)
                return overrideModifier.Value;
            
            var result = inputValue + additiveSum;
            result += baseValue * percentageSum * 0.01f;
            result *= multiplicativeProduct;
            
            return result;
        }
        
        private void UpdateExpiredModifiersOptimized()
        {
            if (modifiers.Count == 0) return;
            
            var deltaTime = Application.isPlaying ? Time.deltaTime : 0f;
            var currentVersion = modifierVersion;
            
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier.Update(deltaTime) || modifier.ShouldRemove())
                {
                    modifiers.RemoveAt(i);
                    modifierVersion++;
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
            
            if (modifierVersion != currentVersion)
            {
                needsRecalculation = true;
                NotifyDependentStats();
            }
        }
        
        private float EvaluateFormula()
        {
            if (parentContainer != null)
                return FormulaEvaluator.Evaluate(statType.Formula, parentContainer);
            
            if (registriesByStatId.TryGetValue(Id, out var registry))
                return FormulaEvaluator.Evaluate(statType.Formula, registry);
            
            var owner = GetOwner();
            if (owner != null)
            {
                EnsureRegistry(owner);
                if (registriesByStatId.TryGetValue(Id, out var newRegistry))
                    return FormulaEvaluator.Evaluate(statType.Formula, newRegistry);
            }
            
            return 0f;
        }
        
        public IStatModifier AddModifier(float value, ModifierType type = ModifierType.Additive, 
                                        ModifierDuration duration = ModifierDuration.Permanent, 
                                        float time = 0f, ModifierPriority priority = ModifierPriority.Normal,
                                        string source = "", object tag = null)
        {
            var modifier = new StatModifier(this, value, type, duration, time, priority, source, tag);
            return AddModifierFast(modifier);
        }
        
        public IStatModifier AddModifier(IStatModifier modifier) => AddModifierFast(modifier);
        
        private IStatModifier AddModifierFast(IStatModifier modifier)
        {
            if (modifier == null) return null;
            
            modifiers.Add(modifier);
            modifierVersion++;
            InvalidateCache();
            OnModifierAdded?.Invoke(this, modifier);
            
            return modifier;
        }
        
        public bool RemoveModifier(IStatModifier modifier)
        {
            var index = modifiers.IndexOf(modifier);
            if (index >= 0)
            {
                modifiers.RemoveAt(index);
                modifierVersion++;
                InvalidateCache();
                OnModifierRemoved?.Invoke(this, modifier);
                return true;
            }
            return false;
        }
        
        public bool RemoveModifier(string id)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Id == id)
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    modifierVersion++;
                    InvalidateCache();
                    OnModifierRemoved?.Invoke(this, modifier);
                    return true;
                }
            }
            return false;
        }
        
        public void RemoveModifiersBySource(string source)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].Source == source)
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    modifierVersion++;
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
            InvalidateCache();
        }
        
        public void RemoveModifiersByTag(object tag)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (Equals(modifiers[i].Tag, tag))
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    modifierVersion++;
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
            InvalidateCache();
        }
        
        public void ClearModifiers()
        {
            if (modifiers.Count == 0) return;
            
            tempModifierList.Clear();
            tempModifierList.AddRange(modifiers);
            modifiers.Clear();
            modifierVersion++;
            InvalidateCache();
            
            for (int i = 0; i < tempModifierList.Count; i++)
            {
                OnModifierRemoved?.Invoke(this, tempModifierList[i]);
            }
            
            tempModifierList.Clear();
        }
        
        public IStatModifier AddBonus(float value, string source = "") => 
            AddModifier(value, ModifierType.Additive, source: source);
        
        public IStatModifier AddDebuff(float value, string source = "") => 
            AddModifier(value, ModifierType.Subtractive, source: source);
        
        public IStatModifier AddMultiplier(float multiplier, string source = "") => 
            AddModifier(multiplier, ModifierType.Multiplicative, source: source);
        
        public IStatModifier AddPercentage(float percentage, string source = "") => 
            AddModifier(percentage, ModifierType.Percentage, source: source);
        
        public IStatModifier AddTemporary(float value, float duration, string source = "") => 
            AddModifier(value, ModifierType.Additive, ModifierDuration.Temporary, duration, source: source);
        
        public IStatModifier SetOverride(float value, string source = "") => 
            AddModifier(value, ModifierType.Override, priority: ModifierPriority.Override, source: source);
        
        public static void RegisterDependency(string sourceStatId, Stat dependentStat)
        {
            if (!dependentsCache.TryGetValue(sourceStatId, out var dependents))
            {
                dependents = new List<Stat>(4);
                dependentsCache[sourceStatId] = dependents;
            }
            
            if (!dependents.Contains(dependentStat))
                dependents.Add(dependentStat);
        }
        
        public static void UnregisterDependency(string sourceStatId, Stat dependentStat)
        {
            if (dependentsCache.TryGetValue(sourceStatId, out var dependents))
            {
                dependents.Remove(dependentStat);
                if (dependents.Count == 0)
                    dependentsCache.Remove(sourceStatId);
            }
        }
        
        public static void RegisterStatRegistry(string statId, StatRegistry registry) =>
            registriesByStatId[statId] = registry;
        
        public static void UnregisterStatRegistry(string statId) =>
            registriesByStatId.Remove(statId);
        
        private object GetOwner()
        {
            var allObjects = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            return allObjects.FirstOrDefault(HasThisStat);
        }
        
        private bool HasThisStat(object owner)
        {
            var fields = owner.GetType().GetFields(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(Stat))
                {
                    var stat = fields[i].GetValue(owner) as Stat;
                    if (stat == this) return true;
                }
            }
            return false;
        }
        
        private void EnsureRegistry(object owner)
        {
            if (!registriesByStatId.ContainsKey(Id))
                registriesByStatId[Id] = new StatRegistry(owner);
        }
        
        public void ForceRecalculate()
        {
            needsRecalculation = true;
        }
        
        public static implicit operator float(Stat stat) => stat?.Value ?? 0f;
        
        public override string ToString() => $"{Name}: {Value:F1}";
        
        public static void CleanupStat(string statId)
        {
            if (string.IsNullOrEmpty(statId)) return;
            
            registriesByStatId.Remove(statId);
            dependentsCache.Remove(statId);
            
            var keysToRemove = new List<string>(8);
            
            foreach (var kvp in dependentsCache)
            {
                var dependents = kvp.Value;
                for (int i = dependents.Count - 1; i >= 0; i--)
                {
                    if (dependents[i] == null || dependents[i].Id == statId)
                    {
                        dependents.RemoveAt(i);
                    }
                }
                
                if (dependents.Count == 0)
                    keysToRemove.Add(kvp.Key);
            }
            
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                dependentsCache.Remove(keysToRemove[i]);
            }
        }
    }
}