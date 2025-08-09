using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private bool isDirty = true;
        
        private IStatModifier[] modifiers = new IStatModifier[8];
        private int modifierCount;
        
        private static readonly Dictionary<string, StatRegistry> registriesByStatId = new(512);
        private static readonly Dictionary<string, List<Stat>> dependentsCache = new(256);
        
        private static readonly Dictionary<string, Stat> globalStats = new(512);
        
        [NonSerialized] private StatContainer parentContainer;
        [NonSerialized] private bool hasOverrideModifier;
        [NonSerialized] private float lastCalculatedBaseValue;
        [NonSerialized] private float lastFormulaResult;
        [NonSerialized] private bool formulaEvaluated;
        [NonSerialized] private string ownerPrefix;
        
        [NonSerialized] private bool isNotifying;
        [NonSerialized] private static readonly HashSet<string> currentlyNotifying = new(32);
        
        [NonSerialized] private float additiveCache;
        [NonSerialized] private float multiplicativeCache = 1f;
        [NonSerialized] private float percentageCache;
        [NonSerialized] private IStatModifier overrideCache;
        [NonSerialized] private bool modifierCacheValid;
        
        public StatType StatType => statType;
        public string Name => statType ? statType.DisplayName : "None";
        public string ShortName => statType ? statType.ShortName : "";
        public StatValueType ValueType => statType ? statType.ValueType : StatValueType.Normal;
        public string Id 
        { 
            get 
            { 
                if (string.IsNullOrEmpty(statId))
                    statId = StatIdPool.GetId();
                return statId; 
            } 
        }
        
        public IReadOnlyList<IStatModifier> Modifiers
        {
            get
            {
                var list = new List<IStatModifier>(modifierCount);
                for (int i = 0; i < modifierCount; i++)
                {
                    if (modifiers[i] != null)
                        list.Add(modifiers[i]);
                }
                return list;
            }
        }
        
        public float BaseValue 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => baseValue;
            set 
            {
                if (Math.Abs(baseValue - value) > float.Epsilon)
                {
                    var oldBase = baseValue;
                    baseValue = value;
                    OnBaseValueChanged?.Invoke(this, oldBase, value);
                    InvalidateCache();
                }
            }
        }
        
        public float Value 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                RegisterIfNeeded();
                EnsureDefaultValue();
                
                if (needsRecalculation || isDirty || Math.Abs(lastCalculatedBaseValue - baseValue) > float.Epsilon)
                    RecalculateValue();
                return cachedValue;
            }
        }
        
        public string FormattedValue 
        { 
            get 
            {
                if (statType != null)
                    return statType.FormatValue(Value);
                
                return Math.Round(Value, MidpointRounding.AwayFromZero).ToString("F0");
            }
        }
        
        public int IntValue 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)Math.Round(Value, MidpointRounding.AwayFromZero);
        }
        
        public float PercentageNormalized => ValueType == StatValueType.Percentage ? Value * 0.01f : Value;
        
        public event Action<Stat, float, float> OnValueChanged;
        public event Action<Stat, IStatModifier> OnModifierAdded;
        public event Action<Stat, IStatModifier> OnModifierRemoved;
        public event Action<Stat, float, float> OnBaseValueChanged;
        
        public Stat() 
        {
            statId = StatIdPool.GetId();
            InitializeDefaults();
            if (statType != null)
            {
                baseValue = statType.DefaultValue;
                lastCalculatedBaseValue = baseValue;
                cachedValue = baseValue;
            }
        }
        
        public Stat(StatType type, float value = 0f, string customId = null)
        {
            statType = type;
            baseValue = value;
            statId = customId ?? StatIdPool.GetId();
            lastCalculatedBaseValue = value;
            InitializeDefaults();
        }
        
        private Stat(float value)
        {
            statType = null;
            baseValue = value;
            statId = StatIdPool.GetId();
            lastCalculatedBaseValue = value;
            InitializeDefaults();
        }
        
        private void InitializeDefaults()
        {
            cachedValue = baseValue;
            additiveCache = 0f;
            multiplicativeCache = 1f;
            percentageCache = 0f;
            overrideCache = null;
            modifierCacheValid = false;
            hasOverrideModifier = false;
            formulaEvaluated = false;
            lastFormulaResult = 0f;
            ownerPrefix = "Global";
            isNotifying = false;
            
            RegisterIfNeeded();
        }
        
        private void RegisterIfNeeded()
        {
            if (statType == null) return;
            
            if (ownerPrefix == "Global")
            {
                ownerPrefix = DetectOwnerPrefix();
            }
            
            if (!string.IsNullOrEmpty(statType.DisplayName))
            {
                globalStats[$"{ownerPrefix}_{statType.DisplayName}"] = this;
            }
            
            if (!string.IsNullOrEmpty(statType.ShortName))
            {
                globalStats[$"{ownerPrefix}_{statType.ShortName}"] = this;
            }
            
            globalStats[statType.DisplayName] = this;
            globalStats[statType.ShortName] = this;
        }
        
        private string DetectOwnerPrefix()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    
                    if (method?.DeclaringType != null && method.DeclaringType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        return method.DeclaringType.Name;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return "Global";
        }
        
        public void SetContainer(StatContainer container)
        {
            if (parentContainer != container)
            {
                parentContainer = container;
                ownerPrefix = container?.Name ?? "Global";
                InvalidateCache();
                InvalidateFormula();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateCache()
        {
            needsRecalculation = true;
            isDirty = true;
            modifierCacheValid = false;
            NotifyDependents();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateFormula()
        {
            formulaEvaluated = false;
            lastFormulaResult = 0f;
            InvalidateCache();
        }
        
        private void NotifyDependents()
        {
            if (isNotifying || currentlyNotifying.Contains(Id))
                return;
                
            isNotifying = true;
            currentlyNotifying.Add(Id);
            
            try
            {
                if (dependentsCache.TryGetValue(Id, out var dependents))
                {
                    foreach (var dependent in dependents)
                    {
                        if (dependent != null && !currentlyNotifying.Contains(dependent.Id))
                        {
                            dependent.InvalidateCache();
                        }
                    }
                }
                
                parentContainer?.NotifyStatChanged(this);
                
                if (registriesByStatId.TryGetValue(Id, out var registry))
                {
                    registry.NotifyStatChanged(this);
                }
            }
            finally
            {
                currentlyNotifying.Remove(Id);
                isNotifying = false;
            }
        }
        
        private void RecalculateValue()
        {
            var oldValue = cachedValue;
            
            var newValue = CalculateFinalValue();
            newValue = ApplyClampingIfNeeded(newValue);
            
            cachedValue = newValue;
            needsRecalculation = false;
            isDirty = false;
            lastCalculatedBaseValue = baseValue;
            
            if (Math.Abs(oldValue - newValue) > float.Epsilon)
            {
                OnValueChanged?.Invoke(this, oldValue, newValue);
            }
        }
        
        private void EnsureDefaultValue()
        {
            if (Math.Abs(baseValue) < float.Epsilon && statType != null && Math.Abs(statType.DefaultValue) > float.Epsilon)
            {
                baseValue = statType.DefaultValue;
                lastCalculatedBaseValue = baseValue;
                cachedValue = baseValue;
            }
        }
        private float CalculateFinalValue()
        {
            var result = baseValue;
            
            if (statType?.HasFormula == true)
            {
                var formulaResult = EvaluateFormula();
                result = baseValue + formulaResult;
            }
            
            result = ApplyModifiers(result);
            return result;
        }
        
        private float ApplyModifiers(float inputValue)
        {
            if (modifierCount == 0)
            {
                hasOverrideModifier = false;
                return inputValue;
            }
            
            if (!modifierCacheValid)
                RecalculateModifierCache();
            
            if (overrideCache != null)
            {
                hasOverrideModifier = true;
                return overrideCache.Value;
            }
            
            hasOverrideModifier = false;
         
            var result = inputValue;
            
            result += additiveCache;
            
            result += baseValue * (percentageCache * 0.01f);
            
            result *= multiplicativeCache;
            
            return result;
        }
        
        private float ApplyClampingIfNeeded(float value)
        {
            if (hasOverrideModifier)
                return value;
                
            if (statType == null)
                return value;
            
            if (statType.ValueType == StatValueType.Percentage)
            {
                return Mathf.Clamp(value, statType.MinValue, statType.MaxValue);
            }
            
            return value;
        }
        
        private float EvaluateFormula()
        {
            if (formulaEvaluated)
                return lastFormulaResult;
                
            try
            {
                float result = 0f;
                
                if (parentContainer != null)
                {
                    result = FormulaEvaluator.Evaluate(statType.Formula, parentContainer);
                }
                else if (registriesByStatId.TryGetValue(Id, out var registry))
                {
                    result = FormulaEvaluator.Evaluate(statType.Formula, registry);
                }
                else
                {
                    result = FormulaEvaluator.EvaluateGlobal(statType.Formula, ownerPrefix, globalStats);
                }
                
                lastFormulaResult = result;
                formulaEvaluated = true;
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatForge] Error evaluating formula '{statType.Formula}' for stat '{Name}': {e.Message}");
                lastFormulaResult = 0f;
                formulaEvaluated = true;
                return 0f;
            }
        }
        
        private void RecalculateModifierCache()
        {
            additiveCache = 0f;
            multiplicativeCache = 1f;
            percentageCache = 0f;
            overrideCache = null;
            
            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null) continue;
                
                switch (modifier.Type)
                {
                    case ModifierType.Override:
                        if (overrideCache == null || modifier.Priority >= overrideCache.Priority)
                            overrideCache = modifier;
                        break;
                        
                    case ModifierType.Additive:
                        additiveCache += modifier.Value;
                        break;
                        
                    case ModifierType.Subtractive:
                        additiveCache -= modifier.Value;
                        break;
                        
                    case ModifierType.Multiplicative:
                        multiplicativeCache *= modifier.Value;
                        break;
                        
                    case ModifierType.Percentage:
                        percentageCache += modifier.Value;
                        break;
                }
            }
            
            modifierCacheValid = true;
        }
        
        public IStatModifier AddModifier(float value, ModifierType type = ModifierType.Additive, 
                                        ModifierDuration duration = ModifierDuration.Permanent, 
                                        float time = 0f, ModifierPriority priority = ModifierPriority.Normal,
                                        string source = "", object tag = null)
        {
            var modifier = new StatModifier(this, value, type, duration, time, priority, source, tag);
            return AddModifierInternal(modifier);
        }
        
        public IStatModifier AddModifier(IStatModifier modifier) => AddModifierInternal(modifier);
        
        private IStatModifier AddModifierInternal(IStatModifier modifier)
        {
            if (modifier == null) return null;
            
            if (modifierCount >= modifiers.Length)
            {
                var newArray = new IStatModifier[modifiers.Length * 2];
                Array.Copy(modifiers, newArray, modifiers.Length);
                modifiers = newArray;
            }
            
            modifiers[modifierCount++] = modifier;
            InvalidateModifierCache();
            OnModifierAdded?.Invoke(this, modifier);
            
            return modifier;
        }
        
        public bool RemoveModifier(IStatModifier modifier)
        {
            for (int i = 0; i < modifierCount; i++)
            {
                if (modifiers[i] == modifier)
                {
                    RemoveModifierAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                    return true;
                }
            }
            return false;
        }
        
        public bool RemoveModifier(string id)
        {
            for (int i = 0; i < modifierCount; i++)
            {
                if (modifiers[i]?.Id == id)
                {
                    var modifier = modifiers[i];
                    RemoveModifierAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                    return true;
                }
            }
            return false;
        }
        
        private void RemoveModifierAt(int index)
        {
            for (int i = index; i < modifierCount - 1; i++)
            {
                modifiers[i] = modifiers[i + 1];
            }
            
            modifiers[modifierCount - 1] = null;
            modifierCount--;
            InvalidateModifierCache();
        }
        
        public void RemoveModifiersBySource(string source)
        {
            for (int i = modifierCount - 1; i >= 0; i--)
            {
                if (modifiers[i]?.Source == source)
                {
                    var modifier = modifiers[i];
                    RemoveModifierAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
        }
        
        public void RemoveModifiersByTag(object tag)
        {
            for (int i = modifierCount - 1; i >= 0; i--)
            {
                if (Equals(modifiers[i]?.Tag, tag))
                {
                    var modifier = modifiers[i];
                    RemoveModifierAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
        }
        
        public void ClearModifiers()
        {
            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiers[i];
                modifiers[i] = null;
                OnModifierRemoved?.Invoke(this, modifier);
            }
            
            modifierCount = 0;
            InvalidateModifierCache();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateModifierCache()
        {
            modifierCacheValid = false;
            InvalidateCache();
        }
        
        public IStatModifier AddBonus(float value, string source = "") => 
            AddModifier(value, source: source);
        
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
        
        public void ForceRecalculate()
        {
            InvalidateCache();
            InvalidateFormula();
            var _ = Value;
        }
        
        public void UpdateModifiers(float deltaTime)
        {
            bool removedAny = false;
            
            for (int i = modifierCount - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier != null && modifier.Update(deltaTime))
                {
                    RemoveModifierAt(i);
                    removedAny = true;
                }
            }
            
            if (removedAny)
                InvalidateModifierCache();
        }
        
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
        
        public float GetValueFast()
        {
            return needsRecalculation ? Value : cachedValue;
        }
        
        public bool HasModifiers => modifierCount > 0;
        public bool HasFormula => statType?.HasFormula == true;
        public bool IsDirty => isDirty || needsRecalculation;
        
      
        public static implicit operator float(Stat stat) => stat?.Value ?? 0f;
        public static implicit operator Stat(float value) => new(value);
        public static implicit operator Stat(int value) => new(value);
        
        public override string ToString() => $"{Name}: {FormattedValue}";
        
       
        public static void CleanupStat(string statId)
        {
            if (string.IsNullOrEmpty(statId)) return;
            
            registriesByStatId.Remove(statId);
            dependentsCache.Remove(statId);
            currentlyNotifying.Remove(statId);
            
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
            
            foreach (var key in keysToRemove)
            {
                dependentsCache.Remove(key);
            }
        }
        
        public static void ClearFieldCache() => StatRegistry.ClearFieldCache();
        
        public static void ClearAllCaches()
        {
            registriesByStatId.Clear();
            dependentsCache.Clear();
            globalStats.Clear();
            currentlyNotifying.Clear();
            StatRegistry.ClearFieldCache();
        }
        
        public float GetBaseValueRaw() => baseValue;
        public float GetCachedValueRaw() => cachedValue;
        public int GetModifierCount() => modifierCount;
        public bool GetNeedsRecalculation() => needsRecalculation;
        public string GetDebugInfo()
        {
            return $"Stat[{Name}] Base:{baseValue:F2} Cached:{cachedValue:F2} Mods:{modifierCount} Dirty:{isDirty}";
        }
    }
}