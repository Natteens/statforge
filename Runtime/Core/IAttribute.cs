using System;

namespace StatForge.Core
{
    /// <summary>
    /// Core interface for all attribute types in the new StatForge API
    /// </summary>
    public interface IAttribute<T> where T : struct, IComparable<T>
    {
        /// <summary>
        /// The current value of the attribute
        /// </summary>
        T Value { get; set; }
        
        /// <summary>
        /// The base value before any modifications
        /// </summary>
        T BaseValue { get; set; }
        
        /// <summary>
        /// Minimum allowed value
        /// </summary>
        T MinValue { get; }
        
        /// <summary>
        /// Maximum allowed value
        /// </summary>
        T MaxValue { get; }
        
        /// <summary>
        /// Event fired when the value changes
        /// </summary>
        event Action<T, T> OnValueChanged; // oldValue, newValue
        
        /// <summary>
        /// Add a temporary modifier to this attribute
        /// </summary>
        void AddModifier(T value, float duration = 0f);
        
        /// <summary>
        /// Remove a temporary modifier
        /// </summary>
        void RemoveModifier(T value);
        
        /// <summary>
        /// Clear all temporary modifiers
        /// </summary>
        void ClearModifiers();
        
        /// <summary>
        /// Validate if a value is within acceptable range
        /// </summary>
        bool IsValidValue(T value);
        
        /// <summary>
        /// Clamp a value to the valid range
        /// </summary>
        T ClampValue(T value);
    }
}