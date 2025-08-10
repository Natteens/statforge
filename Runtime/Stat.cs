using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private bool debug;
        
        private float cachedValue;
        private bool needsRecalculation;
        private bool isDirty;
        
        private List<IStatModifier> modifiers = new();
        
        private static readonly Dictionary<string, StatRegistry> registriesByStatId = new(512);
        private static readonly Dictionary<string, List<Stat>> dependentsCache = new(256);
        private static readonly Dictionary<string, Stat> globalStats = new(512);
        
        [NonSerialized] private Container parentContainer;
        [NonSerialized] private bool hasOverrideModifier;
        [NonSerialized] private float lastCalculatedBaseValue;
        [NonSerialized] private float lastFormulaResult;
        [NonSerialized] private bool formulaEvaluated;
        [NonSerialized] private string ownerPrefix;
        [NonSerialized] private bool hasBeenInitialized;
        
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
        
        public IReadOnlyList<IStatModifier> Modifiers => modifiers.AsReadOnly();
        
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
                    hasBeenInitialized = true;
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
                EnsureInitialized();
                
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
                SetBaseValueAndInitialize(statType.DefaultValue);
            }
        }
        
        public Stat(StatType type, float value = 0f, string customId = null)
        {
            statType = type;
            statId = customId ?? StatIdPool.GetId();
            InitializeDefaults();
            
            var finalValue = (Math.Abs(value) < float.Epsilon && type != null && Math.Abs(type.DefaultValue) > float.Epsilon) 
                ? type.DefaultValue 
                : value;
                
            SetBaseValueAndInitialize(finalValue);
        }
        
        private Stat(float value)
        {
            statType = null;
            statId = StatIdPool.GetId();
            InitializeDefaults();
            SetBaseValueAndInitialize(value);
        }
        
        private void InitializeDefaults()
        {
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
            needsRecalculation = false;
            isDirty = false;
            hasBeenInitialized = false;
        }
        
        private void SetBaseValueAndInitialize(float value)
        {
            baseValue = value;
            cachedValue = value;
            lastCalculatedBaseValue = value;
            hasBeenInitialized = true;
            
            RegisterIfNeeded();
        }
        
        private void EnsureInitialized()
        {
            if (!hasBeenInitialized)
            {
                var defaultVal = statType?.DefaultValue ?? 0f;
                SetBaseValueAndInitialize(Math.Abs(baseValue) > float.Epsilon ? baseValue : defaultVal);
            }
            
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
                globalStats[statType.DisplayName] = this;
            }
            
            if (!string.IsNullOrEmpty(statType.ShortName))
            {
                globalStats[$"{ownerPrefix}_{statType.ShortName}"] = this;
                globalStats[statType.ShortName] = this;
            }
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
                // ignore
            }

            return "Global";
        }
        
        public void SetContainer(Container container)
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
            if (modifiers.Count == 0)
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
            if (hasOverrideModifier || statType == null)
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
                float result;
                
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
                if (debug)
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
            
            foreach (var modifier in modifiers)
            {
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
            
            modifiers.Add(modifier);
            modifierCacheValid = false;
            OnModifierAdded?.Invoke(this, modifier);
            
            if (debug)
                Debug.Log($"[StatForge] Modificador adicionado: {value}{GetModifierSymbol(type)} para {Name} " + $"({duration}, {(duration == ModifierDuration.Temporary ? $"{time}s" : "permanente")})");
            
            if (duration == ModifierDuration.Temporary)
            {
                StatModifierManager.RegisterTemporaryModifier(this, modifier);
            }
            
            InvalidateCache();
            return modifier;
        }
        
        public bool RemoveModifier(IStatModifier modifier)
        {
            if (modifiers.Remove(modifier))
            {
                OnModifierRemoved?.Invoke(this, modifier);
                modifierCacheValid = false;
                if (debug)
                    Debug.Log($"[StatForge] Modificador removido: {modifier.Id} de {Name}");
                UpdateTemporaryTrackingAfterRemoval();
                InvalidateCache();
                return true;
            }
            return false;
        }
        
        public bool RemoveModifier(string id)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i]?.Id == id)
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                    modifierCacheValid = false;
                    if (debug)
                        Debug.Log($"[StatForge] Modificador removido por ID: {id} de {Name}");
                    UpdateTemporaryTrackingAfterRemoval();
                    InvalidateCache();
                    return true;
                }
            }
            return false;
        }
        
        public void RemoveModifiersBySource(string source)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i]?.Source == source)
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
            modifierCacheValid = false;
            UpdateTemporaryTrackingAfterRemoval();
            InvalidateCache();
        }
        
        public void RemoveModifiersByTag(object tag)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (Equals(modifiers[i]?.Tag, tag))
                {
                    var modifier = modifiers[i];
                    modifiers.RemoveAt(i);
                    OnModifierRemoved?.Invoke(this, modifier);
                }
            }
            modifierCacheValid = false;
            UpdateTemporaryTrackingAfterRemoval();
            InvalidateCache();
        }
        
        public void ClearModifiers()
        {
            var modifiersToRemove = new List<IStatModifier>(modifiers);
            modifiers.Clear();
            
            foreach (var modifier in modifiersToRemove)
            {
                OnModifierRemoved?.Invoke(this, modifier);
            }
            
            modifierCacheValid = false;
            UpdateTemporaryTrackingAfterRemoval();
            InvalidateCache();
        }
        
        private void UpdateTemporaryTrackingAfterRemoval()
        {
            var hasAnyTemporary = modifiers.Any(m => m.Duration == ModifierDuration.Temporary);
            
            if (!hasAnyTemporary)
            {
                StatModifierManager.UnregisterStat(this);
            }
        }
        
        private string GetModifierSymbol(ModifierType type)
        {
            return type switch
            {
                ModifierType.Additive => "+",
                ModifierType.Subtractive => "-",
                ModifierType.Multiplicative => "x",
                ModifierType.Override => "=",
                ModifierType.Percentage => "%",
                _ => ""
            };
        }
        
        public void ForceRecalculate()
        {
            InvalidateCache();
            InvalidateFormula();
            var _ = Value;
        }
        
        public void ForceUpdateModifiers(float deltaTime)
        {
            bool removedAny = false;
    
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier != null && modifier.Duration == ModifierDuration.Temporary)
                {
                    if (modifier.Update(deltaTime) || modifier.ShouldRemove())
                    {
                        if (debug)
                            Debug.Log($"[StatForge] Modificador {modifier.Id} expirou em {Name}");
                        modifiers.RemoveAt(i);
                        OnModifierRemoved?.Invoke(this, modifier);
                        removedAny = true;
                    }
                }
                else if (modifier != null && modifier.Duration == ModifierDuration.Conditional)
                {
                    if (modifier.ShouldRemove())
                    {
                        if (debug)
                            Debug.Log($"[StatForge] Modificador condicional {modifier.Id} removido de {Name}");
                        modifiers.RemoveAt(i);
                        OnModifierRemoved?.Invoke(this, modifier);
                        removedAny = true;
                    }
                }
            }
    
            if (removedAny)
            {
                modifierCacheValid = false;
                UpdateTemporaryTrackingAfterRemoval();
                InvalidateCache();
            }
        }
        
        public void SetDebug(bool enabled)
        {
            debug = enabled;
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
        
        public bool HasModifiers => modifiers.Count > 0;
        public bool HasFormula => statType?.HasFormula == true;
        public bool IsDirty => isDirty || needsRecalculation;
        
        public static string GetGlobalDebugInfo()
        {
            return StatModifierManager.GetGlobalDebugInfo();
        }
        
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
            StatModifierManager.ClearAllCaches();
            StatRegistry.ClearFieldCache();
            StatIdPool.ClearPool();
        }
        
        public float GetBaseValueRaw() => baseValue;
        public float GetCachedValueRaw() => cachedValue;
        public int GetModifierCount() => modifiers.Count;
        public bool GetNeedsRecalculation() => needsRecalculation;
        public string GetDebugInfo()
        {
            var tempMods = modifiers.Count(m => m.Duration == ModifierDuration.Temporary);
            var hasTemporary = StatModifierManager.IsTrackedForTemporary(this);
            return $"Stat[{Name}] Base:{baseValue:F2} Cached:{cachedValue:F2} " +
                   $"Mods:{modifiers.Count} Temp:{tempMods} Dirty:{isDirty} InGlobalList:{hasTemporary}";
        }
    }
}