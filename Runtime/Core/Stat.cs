using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Individual stat that can work independently with its own modifiers, formulas, and values.
    /// This is the core class for the new simplified StatForge API with auto-initialization.
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
        [NonSerialized] private bool _isInitialized = false; // Track initialization state
        [NonSerialized] private StatDefinition _cachedDefinition; // Cached reference to definition
        
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
        /// Automatically initializes if needed.
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
        /// Reference to the StatDefinition this stat is based on (if any).
        /// </summary>
        public StatDefinition Definition
        {
            get
            {
                if (_cachedDefinition == null && !string.IsNullOrEmpty(_definitionGuid))
                {
                    _cachedDefinition = StatDefinition.FindByGuid(_definitionGuid);
                }
                return _cachedDefinition;
            }
            set
            {
                _cachedDefinition = value;
                _definitionGuid = value?.Guid ?? "";
                if (value != null)
                {
                    LoadFromDefinition();
                }
            }
        }
        
        #endregion
        
        #region Auto-Initialization
        
        /// <summary>
        /// Ensures the stat is properly initialized before use.
        /// This enables the "zero-setup" experience.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                AutoInitialize();
            }
        }
        
        /// <summary>
        /// Automatically initializes the stat with sensible defaults.
        /// </summary>
        private void AutoInitialize()
        {
            InitializeRuntime();
            
            // If we have a definition GUID, try to load it
            if (!string.IsNullOrEmpty(_definitionGuid))
            {
                LoadFromDefinition();
            }
            
            // If no name is set, create a default one
            if (string.IsNullOrEmpty(_name))
            {
                _name = "Stat_" + GetHashCode().ToString("X");
            }
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Loads stat configuration from a StatDefinition if available.
        /// </summary>
        private void LoadFromDefinition()
        {
            if (string.IsNullOrEmpty(_definitionGuid)) return;
            
            if (_cachedDefinition == null)
            {
                _cachedDefinition = StatDefinition.FindByGuid(_definitionGuid);
            }
            
            if (_cachedDefinition != null)
            {
                // Only apply definition values if they haven't been manually set
                if (string.IsNullOrEmpty(_name))
                    _name = _cachedDefinition.StatName;
                    
                if (_baseValue == 0f && _cachedDefinition.DefaultBaseValue != 0f)
                    _baseValue = _cachedDefinition.DefaultBaseValue;
                    
                if (string.IsNullOrEmpty(_formula) && !string.IsNullOrEmpty(_cachedDefinition.DefaultFormula))
                    _formula = _cachedDefinition.DefaultFormula;
                    
                _minValue = _cachedDefinition.MinValue;
                _maxValue = _cachedDefinition.MaxValue;
                _allowModifiers = _cachedDefinition.AllowModifiers;
            }
        }
        
        #endregion
        
        #region Unity Serialization
        
        /// <summary>
        /// Called before Unity serializes this object.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // Nothing special needed for serialization
        }
        
        /// <summary>
        /// Called after Unity deserializes this object.
        /// Ensures proper initialization after loading.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Reset initialization state so it will be re-initialized on first use
            _isInitialized = false;
            _isDirty = true;
            _cachedValue = null;
            _cachedDefinition = null;
            
            // Initialize runtime data
            if (_modifiers == null)
                _modifiers = new List<IStatModifier>();
        }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new stat with zero setup - will auto-initialize on first use.
        /// This enables the ultra-simplified experience where you just declare and use.
        /// </summary>
        public Stat()
        {
            // Leave everything as default - will auto-initialize on first access
            InitializeRuntime();
        }
        
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
        /// Addition operator - adds a temporary additive modifier.
        /// </summary>
        public static Stat operator +(Stat stat, float value)
        {
            if (stat != null)
            {
                stat.AddTemporaryBonus(value);
            }
            return stat;
        }
        
        /// <summary>
        /// Subtraction operator - adds a temporary negative modifier.
        /// </summary>
        public static Stat operator -(Stat stat, float value)
        {
            if (stat != null)
            {
                stat.AddTemporaryBonus(-value);
            }
            return stat;
        }
        
        /// <summary>
        /// Multiplication operator - adds a temporary multiplicative modifier.
        /// </summary>
        public static Stat operator *(Stat stat, float multiplier)
        {
            if (stat != null)
            {
                stat.AddTemporaryMultiplier(multiplier);
            }
            return stat;
        }
        
        /// <summary>
        /// Division operator - adds a temporary divisive modifier.
        /// </summary>
        public static Stat operator /(Stat stat, float divisor)
        {
            if (stat != null && divisor != 0f)
            {
                stat.AddTemporaryMultiplier(1f / divisor);
            }
            return stat;
        }
        
        /// <summary>
        /// Greater than comparison.
        /// </summary>
        public static bool operator >(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) > value;
        }
        
        /// <summary>
        /// Less than comparison.
        /// </summary>
        public static bool operator <(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) < value;
        }
        
        /// <summary>
        /// Greater than or equal comparison.
        /// </summary>
        public static bool operator >=(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) >= value;
        }
        
        /// <summary>
        /// Less than or equal comparison.
        /// </summary>
        public static bool operator <=(Stat stat, float value)
        {
            return (stat?.Value ?? 0f) <= value;
        }
        
        /// <summary>
        /// Equality comparison.
        /// </summary>
        public static bool operator ==(Stat stat, float value)
        {
            return Mathf.Approximately(stat?.Value ?? 0f, value);
        }
        
        /// <summary>
        /// Inequality comparison.
        /// </summary>
        public static bool operator !=(Stat stat, float value)
        {
            return !Mathf.Approximately(stat?.Value ?? 0f, value);
        }
        
        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            EnsureInitialized();
            return $"{_name}: {Value:F2}" + (IsDerived ? $" (formula: {_formula})" : "");
        }
        
        /// <summary>
        /// Override GetHashCode to support equality operators.
        /// </summary>
        public override int GetHashCode()
        {
            return _name?.GetHashCode() ?? 0;
        }
        
        /// <summary>
        /// Override Equals to support equality operators.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Stat other)
            {
                return _name == other._name && Mathf.Approximately(Value, other.Value);
            }
            if (obj is float floatValue)
            {
                return Mathf.Approximately(Value, floatValue);
            }
            return false;
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Applies a temporary additive modifier (buff).
        /// </summary>
        public IStatModifier Buff(float value, float duration = -1f)
        {
            return AddTemporaryBonus(value, duration);
        }
        
        /// <summary>
        /// Applies a temporary negative modifier (debuff).
        /// </summary>
        public IStatModifier Debuff(float value, float duration = -1f)
        {
            return AddTemporaryBonus(-value, duration);
        }
        
        /// <summary>
        /// Applies a permanent additive bonus.
        /// </summary>
        public IStatModifier AddBonus(float value)
        {
            return AddTemporaryBonus(value, -1f);
        }
        
        /// <summary>
        /// Applies a permanent multiplicative bonus.
        /// </summary>
        public IStatModifier AddMultiplier(float multiplier)
        {
            return AddTemporaryMultiplier(multiplier, -1f);
        }
        
        /// <summary>
        /// Sets a temporary override value.
        /// </summary>
        public IStatModifier Override(float value, float duration = -1f)
        {
            return SetTemporaryValue(value, duration);
        }
        
        #endregion
    }
}