using System;
using System.Collections.Generic;
using UnityEngine;
using StatForge.Core;

namespace StatForge
{
    /// <summary>
    /// Base implementation of IAttribute that provides natural syntax support
    /// </summary>
    [Serializable]
    public class Attribute<T> : IAttribute<T> where T : struct, IComparable<T>
    {
        [SerializeField] private T value;
        [SerializeField] private T baseValue;
        [SerializeField] private T minValue;
        [SerializeField] private T maxValue;
        
        private List<AttributeModifier<T>> modifiers;
        private bool hasChanged;
        
        public T Value 
        { 
            get => CalculateCurrentValue();
            set => SetValue(value);
        }
        
        public T BaseValue 
        { 
            get => baseValue;
            set => SetBaseValue(value);
        }
        
        public T MinValue => minValue;
        public T MaxValue => maxValue;
        
        public event Action<T, T> OnValueChanged;
        
        public Attribute(T initialValue, T min = default, T max = default)
        {
            baseValue = initialValue;
            value = initialValue;
            minValue = min;
            maxValue = max.CompareTo(default(T)) == 0 ? GetDefaultMax() : max;
            modifiers = new List<AttributeModifier<T>>();
        }
        
        private T GetDefaultMax()
        {
            if (typeof(T) == typeof(int)) return (T)(object)int.MaxValue;
            if (typeof(T) == typeof(float)) return (T)(object)float.MaxValue;
            if (typeof(T) == typeof(double)) return (T)(object)double.MaxValue;
            return default(T);
        }
        
        private void SetValue(T newValue)
        {
            var oldValue = value;
            value = ClampValue(newValue);
            
            if (!value.Equals(oldValue))
            {
                OnValueChanged?.Invoke(oldValue, value);
                hasChanged = true;
            }
        }
        
        private void SetBaseValue(T newValue)
        {
            var oldValue = Value;
            baseValue = ClampValue(newValue);
            var currentValue = Value;
            
            if (!currentValue.Equals(oldValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
                hasChanged = true;
            }
        }
        
        private T CalculateCurrentValue()
        {
            dynamic result = baseValue;
            
            // Apply modifiers
            foreach (var modifier in modifiers)
            {
                if (!modifier.IsExpired())
                {
                    result += (dynamic)modifier.Value;
                }
            }
            
            // Clean up expired modifiers
            modifiers.RemoveAll(m => m.IsExpired());
            
            return ClampValue((T)result);
        }
        
        public void AddModifier(T value, float duration = 0f)
        {
            var modifier = new AttributeModifier<T>(value, duration);
            modifiers.Add(modifier);
            
            var oldValue = Value;
            var newValue = CalculateCurrentValue();
            if (!newValue.Equals(oldValue))
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }
        
        public void RemoveModifier(T value)
        {
            var oldValue = Value;
            modifiers.RemoveAll(m => m.Value.Equals(value));
            var newValue = CalculateCurrentValue();
            
            if (!newValue.Equals(oldValue))
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }
        
        public void ClearModifiers()
        {
            var oldValue = Value;
            modifiers.Clear();
            var newValue = CalculateCurrentValue();
            
            if (!newValue.Equals(oldValue))
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }
        
        public bool IsValidValue(T value)
        {
            return value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0;
        }
        
        public T ClampValue(T value)
        {
            if (value.CompareTo(minValue) < 0) return minValue;
            if (value.CompareTo(maxValue) > 0) return maxValue;
            return value;
        }
        
        // Implicit conversion operators for natural syntax
        public static implicit operator T(Attribute<T> attribute)
        {
            return attribute?.Value ?? default(T);
        }
        
        // Assignment operators
        public static Attribute<T> operator +(Attribute<T> attr, T value)
        {
            if (attr != null)
            {
                attr.Value = AddValues(attr.Value, value);
            }
            return attr;
        }
        
        public static Attribute<T> operator -(Attribute<T> attr, T value)
        {
            if (attr != null)
            {
                attr.Value = SubtractValues(attr.Value, value);
            }
            return attr;
        }
        
        public static Attribute<T> operator *(Attribute<T> attr, T value)
        {
            if (attr != null)
            {
                attr.Value = MultiplyValues(attr.Value, value);
            }
            return attr;
        }
        
        public static Attribute<T> operator /(Attribute<T> attr, T value)
        {
            if (attr != null)
            {
                attr.Value = DivideValues(attr.Value, value);
            }
            return attr;
        }
        
        private static T AddValues(T a, T b)
        {
            return (T)((dynamic)a + (dynamic)b);
        }
        
        private static T SubtractValues(T a, T b)
        {
            return (T)((dynamic)a - (dynamic)b);
        }
        
        private static T MultiplyValues(T a, T b)
        {
            return (T)((dynamic)a * (dynamic)b);
        }
        
        private static T DivideValues(T a, T b)
        {
            return (T)((dynamic)a / (dynamic)b);
        }
    }
    
    /// <summary>
    /// Represents a temporary modifier applied to an attribute
    /// </summary>
    [Serializable]
    internal class AttributeModifier<T> where T : struct
    {
        public T Value { get; private set; }
        public float Duration { get; private set; }
        public float StartTime { get; private set; }
        
        public AttributeModifier(T value, float duration = 0f)
        {
            Value = value;
            Duration = duration;
            StartTime = Time.time;
        }
        
        public bool IsExpired()
        {
            return Duration > 0f && Time.time - StartTime >= Duration;
        }
    }
}