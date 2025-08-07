using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Independent stat collection that works without MonoBehaviour dependencies.
    /// Provides simple stat management with modifiers and events.
    /// </summary>
    [Serializable]
    public class StatCollection
    {
        [SerializeField] private List<StatData> _stats = new List<StatData>();
        private Dictionary<string, StatData> _statLookup = new Dictionary<string, StatData>();
        private GameObject _owner;
        private bool _initialized = false;
        
        /// <summary>
        /// All stats in this collection.
        /// </summary>
        public IReadOnlyList<StatData> Stats => _stats;
        
        /// <summary>
        /// Owner GameObject for event tracking.
        /// </summary>
        public GameObject Owner => _owner;
        
        /// <summary>
        /// Initializes the stat collection.
        /// </summary>
        /// <param name="owner">Optional owner GameObject for events</param>
        public void Initialize(GameObject owner = null)
        {
            _owner = owner;
            _statLookup.Clear();
            
            foreach (var stat in _stats)
            {
                if (!string.IsNullOrEmpty(stat.Name))
                {
                    _statLookup[stat.Name] = stat;
                    
                    if (_owner != null)
                    {
                        StatEvents.TriggerStatInitialized(_owner, stat.Name, stat.GetValue());
                    }
                }
            }
            
            _initialized = true;
        }
        
        /// <summary>
        /// Gets the value of a stat.
        /// </summary>
        public float Get(string statName)
        {
            if (string.IsNullOrEmpty(statName)) return 0f;
            
            if (_statLookup.TryGetValue(statName, out var stat))
            {
                return stat.GetValue();
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Sets the base value of a stat.
        /// </summary>
        public void Set(string statName, float value)
        {
            if (string.IsNullOrEmpty(statName)) return;
            
            var oldValue = Get(statName);
            var stat = GetOrCreateStat(statName);
            stat.BaseValue = value;
            
            var newValue = stat.GetValue();
            if (!Mathf.Approximately(oldValue, newValue) && _owner != null)
            {
                StatEvents.TriggerStatChanged(_owner, statName, oldValue, newValue);
            }
        }
        
        /// <summary>
        /// Adds a modifier to a stat.
        /// </summary>
        public void AddModifier(string statName, IStatModifier modifier)
        {
            if (string.IsNullOrEmpty(statName) || modifier == null) return;
            
            var oldValue = Get(statName);
            var stat = GetOrCreateStat(statName);
            stat.AddModifier(modifier);
            
            var newValue = stat.GetValue();
            
            if (_owner != null)
            {
                StatEvents.TriggerModifierAdded(_owner, statName, modifier);
                
                if (!Mathf.Approximately(oldValue, newValue))
                {
                    StatEvents.TriggerStatChanged(_owner, statName, oldValue, newValue);
                }
            }
        }
        
        /// <summary>
        /// Removes a modifier from a stat.
        /// </summary>
        public bool RemoveModifier(string statName, IStatModifier modifier)
        {
            if (string.IsNullOrEmpty(statName) || modifier == null) return false;
            
            if (_statLookup.TryGetValue(statName, out var stat))
            {
                var oldValue = stat.GetValue();
                var removed = stat.RemoveModifier(modifier);
                
                if (removed && _owner != null)
                {
                    StatEvents.TriggerModifierRemoved(_owner, statName, modifier);
                    
                    var newValue = stat.GetValue();
                    if (!Mathf.Approximately(oldValue, newValue))
                    {
                        StatEvents.TriggerStatChanged(_owner, statName, oldValue, newValue);
                    }
                }
                
                return removed;
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes a modifier by ID from a stat.
        /// </summary>
        public bool RemoveModifier(string statName, string modifierId)
        {
            if (string.IsNullOrEmpty(statName) || string.IsNullOrEmpty(modifierId)) return false;
            
            if (_statLookup.TryGetValue(statName, out var stat))
            {
                var modifier = stat.GetModifier(modifierId);
                return modifier != null && RemoveModifier(statName, modifier);
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all modifiers for a stat.
        /// </summary>
        public IReadOnlyList<IStatModifier> GetModifiers(string statName)
        {
            if (string.IsNullOrEmpty(statName)) return new List<IStatModifier>();
            
            return _statLookup.TryGetValue(statName, out var stat) 
                ? stat.Modifiers 
                : new List<IStatModifier>();
        }
        
        /// <summary>
        /// Clears all modifiers from a stat.
        /// </summary>
        public void ClearModifiers(string statName)
        {
            if (string.IsNullOrEmpty(statName)) return;
            
            if (_statLookup.TryGetValue(statName, out var stat))
            {
                var oldValue = stat.GetValue();
                stat.ClearModifiers();
                
                var newValue = stat.GetValue();
                if (!Mathf.Approximately(oldValue, newValue) && _owner != null)
                {
                    StatEvents.TriggerStatChanged(_owner, statName, oldValue, newValue);
                }
            }
        }
        
        /// <summary>
        /// Checks if a stat exists.
        /// </summary>
        public bool HasStat(string statName)
        {
            return !string.IsNullOrEmpty(statName) && _statLookup.ContainsKey(statName);
        }
        
        /// <summary>
        /// Updates all modifiers with duration.
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var stat in _stats)
            {
                stat.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Clears all stats and modifiers.
        /// </summary>
        public void Clear()
        {
            _stats.Clear();
            _statLookup.Clear();
            
            if (_owner != null)
            {
                StatEvents.TriggerStatsCleared(_owner);
            }
        }
        
        private StatData GetOrCreateStat(string statName)
        {
            if (_statLookup.TryGetValue(statName, out var stat))
            {
                return stat;
            }
            
            // Create new stat
            stat = new StatData(statName, 0f);
            _stats.Add(stat);
            _statLookup[statName] = stat;
            
            if (_initialized && _owner != null)
            {
                StatEvents.TriggerStatInitialized(_owner, statName, 0f);
            }
            
            return stat;
        }
    }
    
    /// <summary>
    /// Internal data structure for individual stats.
    /// </summary>
    [Serializable]
    public class StatData
    {
        [SerializeField] private string _name;
        [SerializeField] private float _baseValue;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = float.MaxValue;
        
        private List<IStatModifier> _modifiers = new List<IStatModifier>();
        
        public string Name => _name;
        public float BaseValue 
        { 
            get => _baseValue; 
            set => _baseValue = Mathf.Clamp(value, _minValue, _maxValue); 
        }
        public float MinValue 
        { 
            get => _minValue; 
            set => _minValue = value; 
        }
        public float MaxValue 
        { 
            get => _maxValue; 
            set => _maxValue = value; 
        }
        
        public IReadOnlyList<IStatModifier> Modifiers => _modifiers;
        
        public StatData(string name, float baseValue)
        {
            _name = name;
            _baseValue = baseValue;
        }
        
        public float GetValue()
        {
            var value = _baseValue;
            
            // Apply modifiers in priority order
            var sortedModifiers = _modifiers.Where(m => m.IsActive).OrderBy(m => m.Priority);
            
            foreach (var modifier in sortedModifiers)
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
            
            return Mathf.Clamp(value, _minValue, _maxValue);
        }
        
        public void AddModifier(IStatModifier modifier)
        {
            if (modifier != null && !_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
            }
        }
        
        public bool RemoveModifier(IStatModifier modifier)
        {
            return modifier != null && _modifiers.Remove(modifier);
        }
        
        public IStatModifier GetModifier(string id)
        {
            return _modifiers.FirstOrDefault(m => m.Id == id);
        }
        
        public void ClearModifiers()
        {
            foreach (var modifier in _modifiers.ToList())
            {
                modifier.Dispose();
            }
            _modifiers.Clear();
        }
        
        public void Update(float deltaTime)
        {
            // Update modifiers with duration
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (!modifier.Update(deltaTime))
                {
                    _modifiers.RemoveAt(i);
                }
            }
        }
    }
}