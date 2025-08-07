using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Individual stat that can work independently with its own modifiers, formulas, and values.
    /// This is the core class for the new simplified StatForge API.
    /// </summary>
    [Serializable]
    public class Stat : ISerializationCallbackReceiver
    {
        [SerializeField] private string _name;
        [SerializeField] private float _baseValue;
        [SerializeField] private string _formula;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = float.MaxValue;
        [SerializeField] private bool _allowModifiers = true;
        [SerializeField] private string _definitionGuid; // Reference to global StatDefinition
        
        // Runtime data (not serialized)
        [NonSerialized] private List<IStatModifier> _modifiers = new List<IStatModifier>();
        [NonSerialized] private float? _cachedValue;
        [NonSerialized] private bool _isDirty = true;
        [NonSerialized] private GameObject _owner;
        [NonSerialized] private StatCollection _parentCollection;
        [NonSerialized] private bool _isCalculating = false; // Prevent circular references
        [NonSerialized] private StatDefinition _cachedDefinition; // Cached reference to definition
        [NonSerialized] private bool _isInitialized = false;
        
        /// <summary>
        /// Event fired when this stat's value changes.
        /// Parameters: (oldValue, newValue)
        /// </summary>
        public event System.Action<float, float> OnValueChanged;
        
        #region Properties
        
        /// <summary>
        /// Name of this stat.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// Base value of this stat (before modifiers and formulas).
        /// </summary>
        public float BaseValue
        {
            get => _baseValue;
            set
            {
                var clampedValue = Mathf.Clamp(value, _minValue, _maxValue);
                if (!Mathf.Approximately(_baseValue, clampedValue))
                {
                    var oldValue = Value;
                    _baseValue = clampedValue;
                    MarkDirty();
                    TriggerValueChanged(oldValue, Value);
                }
            }
        }
        
        /// <summary>
        /// Formula for calculating this stat (if it's a derived stat).
        /// </summary>
        public string Formula
        {
            get => _formula;
            set
            {
                if (_formula != value)
                {
                    _formula = value;
                    MarkDirty();
                    var oldValue = Value;
                    TriggerValueChanged(oldValue, Value);
                }
            }
        }
        
        /// <summary>
        /// Final calculated value including base value, formula, and modifiers.
        /// </summary>
        public float Value
        {
            get
            {
                EnsureInitialized();
                
                if (_isDirty || !_cachedValue.HasValue)
                {
                    _cachedValue = CalculateValue();
                    _isDirty = false;
                }
                return _cachedValue.Value;
            }
            set => BaseValue = value; // Setting Value sets BaseValue
        }
        
        /// <summary>
        /// Minimum allowed value for this stat.
        /// </summary>
        public float MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                if (_baseValue < _minValue)
                    BaseValue = _minValue;
            }
        }
        
        /// <summary>
        /// Maximum allowed value for this stat.
        /// </summary>
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                if (_baseValue > _maxValue)
                    BaseValue = _maxValue;
            }
        }
        
        /// <summary>
        /// Whether this stat allows modifiers to be applied.
        /// </summary>
        public bool AllowModifiers
        {
            get => _allowModifiers;
            set => _allowModifiers = value;
        }
        
        /// <summary>
        /// Whether this is a derived stat (has a formula).
        /// </summary>
        public bool IsDerived => !string.IsNullOrEmpty(_formula);
        
        /// <summary>
        /// All modifiers currently applied to this stat.
        /// </summary>
        public IReadOnlyList<IStatModifier> Modifiers => _modifiers;
        
        /// <summary>
        /// Owner GameObject for this stat (for event notifications).
        /// </summary>
        public GameObject Owner
        {
            get => _owner;
            internal set => _owner = value;
        }
        
        /// <summary>
        /// The StatDefinition this stat is based on (if any).
        /// </summary>
        public StatDefinition Definition
        {
            get => _cachedDefinition;
            set => SetDefinition(value);
        }
        
        #endregion
        
        #region Definition Management
        
        /// <summary>
        /// Sets a StatDefinition for this stat and applies its configuration.
        /// </summary>
        public void SetDefinition(StatDefinition definition)
        {
            _cachedDefinition = definition;
            
            if (definition != null)
            {
#if UNITY_EDITOR
                var path = UnityEditor.AssetDatabase.GetAssetPath(definition);
                if (!string.IsNullOrEmpty(path))
                {
                    _definitionGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                }
#endif
                ApplyDefinition(definition);
            }
            else
            {
                _definitionGuid = null;
            }
        }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new stat with the specified name and base value.
        /// </summary>
        public Stat(string name, float baseValue = 0f)
        {
            _name = name;
            _baseValue = baseValue;
            InitializeRuntime();
        }
        
        /// <summary>
        /// Creates a new derived stat with the specified name and formula.
        /// </summary>
        public Stat(string name, string formula)
        {
            _name = name;
            _formula = formula;
            _baseValue = 0f;
            InitializeRuntime();
        }
        
        /// <summary>
        /// Creates a new stat with full configuration.
        /// </summary>
        public Stat(string name, float baseValue, float minValue, float maxValue, bool allowModifiers = true)
        {
            _name = name;
            _baseValue = baseValue;
            _minValue = minValue;
            _maxValue = maxValue;
            _allowModifiers = allowModifiers;
            InitializeRuntime();
        }
        
        #endregion
        
        #region Modifier Management
        
        /// <summary>
        /// Adds a modifier to this stat.
        /// </summary>
        public void AddModifier(IStatModifier modifier)
        {
            if (modifier == null || !_allowModifiers) return;
            
            if (!_modifiers.Contains(modifier))
            {
                var oldValue = Value;
                _modifiers.Add(modifier);
                MarkDirty();
                
                // Trigger events
                var newValue = Value;
                if (_owner != null)
                {
                    StatEvents.TriggerModifierAdded(_owner, _name, modifier);
                }
                TriggerValueChanged(oldValue, newValue);
            }
        }
        
        /// <summary>
        /// Removes a modifier from this stat.
        /// </summary>
        public bool RemoveModifier(IStatModifier modifier)
        {
            if (modifier == null) return false;
            
            if (_modifiers.Remove(modifier))
            {
                var oldValue = Value;
                MarkDirty();
                
                // Trigger events
                var newValue = Value;
                if (_owner != null)
                {
                    StatEvents.TriggerModifierRemoved(_owner, _name, modifier);
                }
                TriggerValueChanged(oldValue, newValue);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes a modifier by ID.
        /// </summary>
        public bool RemoveModifier(string modifierId)
        {
            var modifier = _modifiers.FirstOrDefault(m => m.Id == modifierId);
            return modifier != null && RemoveModifier(modifier);
        }
        
        /// <summary>
        /// Gets a modifier by ID.
        /// </summary>
        public IStatModifier GetModifier(string modifierId)
        {
            return _modifiers.FirstOrDefault(m => m.Id == modifierId);
        }
        
        /// <summary>
        /// Clears all modifiers from this stat.
        /// </summary>
        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;
            
            var oldValue = Value;
            
            // Dispose all modifiers
            foreach (var modifier in _modifiers.ToList())
            {
                modifier.Dispose();
            }
            
            _modifiers.Clear();
            MarkDirty();
            
            var newValue = Value;
            TriggerValueChanged(oldValue, newValue);
        }
        
        #endregion
        
        #region Value Calculation
        
        private float CalculateValue()
        {
            if (_isCalculating)
            {
                Debug.LogWarning($"Circular reference detected in stat '{_name}' formula. Returning base value.");
                return _baseValue;
            }
            
            _isCalculating = true;
            
            try
            {
                float value = _baseValue;
                
                // Apply formula if this is a derived stat
                if (IsDerived)
                {
                    value += EvaluateFormula();
                }
                
                // Apply modifiers if allowed
                if (_allowModifiers && _modifiers.Count > 0)
                {
                    value = ApplyModifiers(value);
                }
                
                // Clamp to min/max values
                value = Mathf.Clamp(value, _minValue, _maxValue);
                
                return value;
            }
            finally
            {
                _isCalculating = false;
            }
        }
        
        private float EvaluateFormula()
        {
            if (string.IsNullOrEmpty(_formula)) return 0f;
            
            try
            {
                // Use the enhanced formula evaluator for individual stats
                return IndividualStatFormulaEvaluator.Evaluate(_formula, this, _parentCollection, _owner);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error evaluating formula '{_formula}' for stat '{_name}': {e.Message}");
                return 0f;
            }
        }
        
        private float ApplyModifiers(float baseValue)
        {
            var value = baseValue;
            
            // Apply modifiers in priority order
            var activeModifiers = _modifiers.Where(m => m.IsActive).OrderBy(m => m.Priority);
            
            foreach (var modifier in activeModifiers)
            {
                switch (modifier.Type)
                {
                    case ModifierType.Additive:
                        value += modifier.Value;
                        break;
                    case ModifierType.Multiplicative:
                        value *= modifier.Value;
                        break;
                    case ModifierType.Override:
                        value = modifier.Value;
                        break;
                }
            }
            
            return value;
        }
        
        #endregion
        
        #region Internal Methods
        
        internal void InitializeRuntime()
        {
            if (_modifiers == null)
                _modifiers = new List<IStatModifier>();
            _isDirty = true;
            _isInitialized = true;
        }
        
        internal void SetParentCollection(StatCollection collection)
        {
            _parentCollection = collection;
        }
        
        internal void Update(float deltaTime)
        {
            // Update modifiers with duration
            bool anyRemoved = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (!modifier.Update(deltaTime))
                {
                    _modifiers.RemoveAt(i);
                    anyRemoved = true;
                }
            }
            
            if (anyRemoved)
            {
                MarkDirty();
            }
        }
        
        private void MarkDirty()
        {
            _isDirty = true;
            _cachedValue = null;
        }
        
        private void TriggerValueChanged(float oldValue, float newValue)
        {
            if (!Mathf.Approximately(oldValue, newValue))
            {
                try
                {
                    OnValueChanged?.Invoke(oldValue, newValue);
                    
                    // Also trigger global event if we have an owner
                    if (_owner != null)
                    {
                        StatEvents.TriggerStatChanged(_owner, _name, oldValue, newValue);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in stat value changed event for '{_name}': {e.Message}");
                }
            }
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Applies a temporary additive modifier.
        /// </summary>
        public IStatModifier AddTemporaryBonus(float value, float duration = -1f, int priority = 0)
        {
            var modifier = StatModifier.Additive(value, duration, priority);
            AddModifier(modifier);
            return modifier;
        }
        
        /// <summary>
        /// Applies a temporary multiplicative modifier.
        /// </summary>
        public IStatModifier AddTemporaryMultiplier(float multiplier, float duration = -1f, int priority = 100)
        {
            var modifier = StatModifier.Multiplicative(multiplier, duration, priority);
            AddModifier(modifier);
            return modifier;
        }
        
        /// <summary>
        /// Sets the value temporarily with an override modifier.
        /// </summary>
        public IStatModifier SetTemporaryValue(float value, float duration = -1f, int priority = 200)
        {
            var modifier = StatModifier.Override(value, duration, priority);
            AddModifier(modifier);
            return modifier;
        }
        
        #endregion
        
        #region Operators and Implicit Conversions
        
        /// <summary>
        /// Implicit conversion to float (returns Value).
        /// </summary>
        public static implicit operator float(Stat stat)
        {
            return stat?.Value ?? 0f;
        }
        
        /// <summary>
        /// Implicit conversion from float (creates new Stat).
        /// </summary>
        public static implicit operator Stat(float value)
        {
            return new Stat("AutoCreated", value);
        }
        
        // Arithmetic operators
        public static float operator +(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) + value;
        }
        
        public static float operator +(float value, Stat stat)
        {
            return value + (stat?.Value ?? 0f);
        }
        
        public static float operator -(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) - value;
        }
        
        public static float operator -(float value, Stat stat)
        {
            return value - (stat?.Value ?? 0f);
        }
        
        public static float operator *(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) * value;
        }
        
        public static float operator *(float value, Stat stat)
        {
            return value * (stat?.Value ?? 0f);
        }
        
        public static float operator /(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) / value;
        }
        
        public static float operator /(float value, Stat stat)
        {
            return value / (stat?.Value ?? 0f);
        }
        
        // Comparison operators
        public static bool operator >(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) > value;
        }
        
        public static bool operator <(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) < value;
        }
        
        public static bool operator >=(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) >= value;
        }
        
        public static bool operator <=(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) <= value;
        }
        
        public static bool operator ==(Stat stat, float value)
        {
            return Mathf.Approximately(stat?.Value ?? 0f, value);
        }
        
        public static bool operator !=(Stat stat, float value)
        {
            return !Mathf.Approximately(stat?.Value ?? 0f, value);
        }
        
        // Comparison with other stats
        public static bool operator >(Stat stat1, Stat stat2)
        {
            return (stat1?.Value ?? 0f) > (stat2?.Value ?? 0f);
        }
        
        public static bool operator <(Stat stat1, Stat stat2)
        {
            return (stat1?.Value ?? 0f) < (stat2?.Value ?? 0f);
        }
        
        public static bool operator >=(Stat stat1, Stat stat2)
        {
            return (stat1?.Value ?? 0f) >= (stat2?.Value ?? 0f);
        }
        
        public static bool operator <=(Stat stat1, Stat stat2)
        {
            return (stat1?.Value ?? 0f) <= (stat2?.Value ?? 0f);
        }
        
        public static bool operator ==(Stat stat1, Stat stat2)
        {
            if (ReferenceEquals(stat1, stat2)) return true;
            if (stat1 is null || stat2 is null) return false;
            return Mathf.Approximately(stat1.Value, stat2.Value);
        }
        
        public static bool operator !=(Stat stat1, Stat stat2)
        {
            return !(stat1 == stat2);
        }
        
        /// <summary>
        /// Override for equals to support operator overloads.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Stat other)
                return this == other;
            if (obj is float floatValue)
                return this == floatValue;
            return false;
        }
        
        /// <summary>
        /// Override for GetHashCode to support operator overloads.
        /// </summary>
        public override int GetHashCode()
        {
            return _name?.GetHashCode() ?? 0;
        }
        
        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"{_name}: {Value:F2}" + (IsDerived ? $" (formula: {_formula})" : "");
        }
        
        #endregion
        
        #region Enhanced API Methods
        
        /// <summary>
        /// Adds a value directly to the base value (syntactic sugar for natural usage).
        /// </summary>
        public void Add(float value)
        {
            BaseValue += value;
        }
        
        /// <summary>
        /// Subtracts a value directly from the base value.
        /// </summary>
        public void Subtract(float value)
        {
            BaseValue -= value;
        }
        
        /// <summary>
        /// Multiplies the base value by a factor.
        /// </summary>
        public void Multiply(float factor)
        {
            BaseValue *= factor;
        }
        
        /// <summary>
        /// Divides the base value by a factor.
        /// </summary>
        public void Divide(float factor)
        {
            BaseValue /= factor;
        }
        
        /// <summary>
        /// Applies a temporary buff (positive modifier).
        /// </summary>
        public IStatModifier Buff(float amount, float duration = -1f)
        {
            return AddTemporaryBonus(amount, duration);
        }
        
        /// <summary>
        /// Applies a temporary debuff (negative modifier).
        /// </summary>
        public IStatModifier Debuff(float amount, float duration = -1f)
        {
            return AddTemporaryBonus(-amount, duration);
        }
        
        /// <summary>
        /// Checks if the stat value is zero or negative.
        /// </summary>
        public bool IsEmpty => Value <= 0f;
        
        /// <summary>
        /// Checks if the stat value is at maximum.
        /// </summary>
        public bool IsAtMax => Mathf.Approximately(Value, MaxValue);
        
        /// <summary>
        /// Checks if the stat value is at minimum.
        /// </summary>
        public bool IsAtMin => Mathf.Approximately(Value, MinValue);
        
        /// <summary>
        /// Gets the percentage of current value relative to max value.
        /// </summary>
        public float Percentage => MaxValue > 0f ? Mathf.Clamp01(Value / MaxValue) : 0f;
        
        #endregion
        
        #region Serialization Callbacks
        
        /// <summary>
        /// Called before Unity serializes this object.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // Save current state if we have a definition
            if (_cachedDefinition != null && string.IsNullOrEmpty(_definitionGuid))
            {
#if UNITY_EDITOR
                var path = UnityEditor.AssetDatabase.GetAssetPath(_cachedDefinition);
                if (!string.IsNullOrEmpty(path))
                {
                    _definitionGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                }
#endif
            }
        }
        
        /// <summary>
        /// Called after Unity deserializes this object.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Ensure runtime initialization
            EnsureInitialized();
        }
        
        /// <summary>
        /// Ensures the stat is properly initialized (lazy initialization).
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            
            InitializeRuntime();
            
            // Try to load definition from GUID if available
            if (!string.IsNullOrEmpty(_definitionGuid) && _cachedDefinition == null)
            {
                LoadDefinitionFromGuid();
            }
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Loads StatDefinition from GUID reference.
        /// </summary>
        private void LoadDefinitionFromGuid()
        {
            if (string.IsNullOrEmpty(_definitionGuid)) return;
            
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(_definitionGuid);
            if (!string.IsNullOrEmpty(path))
            {
                _cachedDefinition = UnityEditor.AssetDatabase.LoadAssetAtPath<StatDefinition>(path);
                if (_cachedDefinition != null)
                {
                    ApplyDefinition(_cachedDefinition);
                }
            }
#else
            // Runtime: Search through loaded StatDefinitions
            var definitions = StatDefinition.GetAllDefinitions();
            foreach (var def in definitions)
            {
                if (def != null && def.name == _name) // Fallback match by name
                {
                    _cachedDefinition = def;
                    ApplyDefinition(_cachedDefinition);
                    break;
                }
            }
#endif
        }
        
        /// <summary>
        /// Applies a StatDefinition to this stat (if values aren't already customized).
        /// </summary>
        private void ApplyDefinition(StatDefinition definition)
        {
            if (definition == null) return;
            
            // Only apply if current values are default/unset
            if (string.IsNullOrEmpty(_name) || _name == "NewStat")
                _name = definition.StatName;
            
            if (string.IsNullOrEmpty(_formula))
                _formula = definition.DefaultFormula;
            
            if (_minValue == 0f && _maxValue == float.MaxValue)
            {
                _minValue = definition.MinValue;
                _maxValue = definition.MaxValue;
            }
            
            if (_baseValue == 0f)
                _baseValue = definition.DefaultBaseValue;
            
            _allowModifiers = definition.AllowModifiers;
        }
        
        #endregion
    }
}