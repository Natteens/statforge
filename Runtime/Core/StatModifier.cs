using System;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Interface for stat modifiers that can be applied to stats.
    /// </summary>
    public interface IStatModifier : IDisposable
    {
        /// <summary>
        /// Unique identifier for this modifier.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Type of modification to apply.
        /// </summary>
        ModifierType Type { get; }
        
        /// <summary>
        /// The value of the modifier.
        /// </summary>
        float Value { get; }
        
        /// <summary>
        /// Priority for applying modifiers. Higher priority applies later.
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this modifier has a duration limit.
        /// </summary>
        bool HasDuration { get; }
        
        /// <summary>
        /// Remaining duration for this modifier. -1 means infinite.
        /// </summary>
        float RemainingDuration { get; }
        
        /// <summary>
        /// Whether this modifier is currently active.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Update the modifier (called each frame if it has duration).
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <returns>True if modifier is still active, false if it should be removed</returns>
        bool Update(float deltaTime);
    }
    
    /// <summary>
    /// Types of stat modifications.
    /// </summary>
    public enum ModifierType
    {
        /// <summary>
        /// Adds a flat value to the stat.
        /// </summary>
        Additive,
        
        /// <summary>
        /// Multiplies the stat by a percentage (e.g., 1.5f = +50%).
        /// </summary>
        Multiplicative,
        
        /// <summary>
        /// Overrides the stat value completely.
        /// </summary>
        Override
    }
    
    /// <summary>
    /// Standard implementation of a stat modifier with auto-dispose functionality.
    /// </summary>
    public class StatModifier : IStatModifier
    {
        private static int _nextId = 1;
        
        public string Id { get; private set; }
        public ModifierType Type { get; private set; }
        public float Value { get; private set; }
        public int Priority { get; private set; }
        public bool HasDuration { get; private set; }
        public float RemainingDuration { get; private set; }
        public bool IsActive { get; private set; } = true;
        
        private readonly GameObject _owner;
        private readonly string _statName;
        private bool _disposed = false;
        
        /// <summary>
        /// Creates a new stat modifier.
        /// </summary>
        /// <param name="type">Type of modification</param>
        /// <param name="value">Modifier value</param>
        /// <param name="duration">Duration in seconds (-1 for infinite)</param>
        /// <param name="priority">Application priority (higher applies later)</param>
        /// <param name="id">Optional custom ID</param>
        public StatModifier(ModifierType type, float value, float duration = -1f, int priority = 0, string id = null)
        {
            Id = id ?? $"modifier_{_nextId++}";
            Type = type;
            Value = value;
            Priority = priority;
            HasDuration = duration > 0f;
            RemainingDuration = duration;
        }
        
        /// <summary>
        /// Internal constructor for modifiers that need to track their owner and stat.
        /// </summary>
        internal StatModifier(GameObject owner, string statName, ModifierType type, float value, float duration = -1f, int priority = 0, string id = null)
            : this(type, value, duration, priority, id)
        {
            _owner = owner;
            _statName = statName;
        }
        
        public bool Update(float deltaTime)
        {
            if (!IsActive || !HasDuration) return IsActive;
            
            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f)
            {
                IsActive = false;
                Dispose();
                return false;
            }
            
            return true;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            IsActive = false;
            _disposed = true;
            
            // Auto-remove from stat if we have owner info
            if (_owner != null && !string.IsNullOrEmpty(_statName))
            {
                _owner.RemoveStatModifier(_statName, this);
            }
        }
        
        /// <summary>
        /// Creates an additive modifier.
        /// </summary>
        public static StatModifier Additive(float value, float duration = -1f, int priority = 0, string id = null)
        {
            return new StatModifier(ModifierType.Additive, value, duration, priority, id);
        }
        
        /// <summary>
        /// Creates a multiplicative modifier.
        /// </summary>
        public static StatModifier Multiplicative(float multiplier, float duration = -1f, int priority = 100, string id = null)
        {
            return new StatModifier(ModifierType.Multiplicative, multiplier, duration, priority, id);
        }
        
        /// <summary>
        /// Creates an override modifier.
        /// </summary>
        public static StatModifier Override(float value, float duration = -1f, int priority = 200, string id = null)
        {
            return new StatModifier(ModifierType.Override, value, duration, priority, id);
        }
    }
}