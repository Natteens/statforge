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
    public class Stat
    {
        [SerializeField] private string _name;
        [SerializeField] private float _baseValue;
        [SerializeField] private string _formula;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = float.MaxValue;
        [SerializeField] private bool _allowModifiers = true;
        
        // Runtime data (not serialized)
        [NonSerialized] private List<IStatModifier> _modifiers = new List<IStatModifier>();
        [NonSerialized] private float? _cachedValue;
        [NonSerialized] private bool _isDirty = true;
        [NonSerialized] private GameObject _owner;
        [NonSerialized] private StatCollection _parentCollection;
        [NonSerialized] private bool _isCalculating = false; // Prevent circular references
        
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
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"{_name}: {Value:F2}" + (IsDerived ? $" (formula: {_formula})" : "");
        }
        
        #endregion
    }
}