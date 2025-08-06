using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using StatForge.Core;

namespace StatForge
{
    /// <summary>
    /// Thread-safe collection for managing attributes with auto-discovery and performance optimizations
    /// </summary>
    [Serializable]
    public class AttributeCollection
    {
        [SerializeField] private List<AttributeEntry> attributes = new List<AttributeEntry>();
        private Dictionary<string, IAttributeBase> attributeLookup;
        private readonly object lockObject = new object();
        private bool isInitialized;
        
        // Performance optimizations
        private static readonly StatCache cache = new StatCache();
        private static readonly ObjectPool<AttributeChangedEvent> eventPool = 
            new ObjectPool<AttributeChangedEvent>();
        
        public event Action<string, object, object> OnAttributeChanged; // name, oldValue, newValue
        
        /// <summary>
        /// Initialize the collection and auto-discover attributes
        /// </summary>
        public void Initialize(object target = null)
        {
            lock (lockObject)
            {
                if (isInitialized) return;
                
                attributeLookup = new Dictionary<string, IAttributeBase>();
                
                if (target != null)
                {
                    DiscoverAttributes(target);
                }
                
                // Build lookup from serialized attributes
                foreach (var attr in attributes)
                {
                    if (!string.IsNullOrEmpty(attr.name) && attr.attribute != null)
                    {
                        attributeLookup[attr.name] = attr.attribute;
                    }
                }
                
                isInitialized = true;
            }
        }
        
        /// <summary>
        /// Auto-discover attributes marked with [Stat] in the target object
        /// </summary>
        private void DiscoverAttributes(object target)
        {
            var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var statAttr = field.GetCustomAttribute<StatAttribute>();
                if (statAttr != null)
                {
                    RegisterAttributeFromField(field, target, statAttr);
                }
            }
            
            var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                var statAttr = property.GetCustomAttribute<StatAttribute>();
                if (statAttr != null && property.CanRead && property.CanWrite)
                {
                    RegisterAttributeFromProperty(property, target, statAttr);
                }
            }
        }
        
        private void RegisterAttributeFromField(FieldInfo field, object target, StatAttribute statAttr)
        {
            var fieldType = field.FieldType;
            var name = string.IsNullOrEmpty(statAttr.DisplayName) ? field.Name : statAttr.DisplayName;
            
            // Create appropriate attribute type
            if (fieldType == typeof(int))
            {
                var currentValue = (int)field.GetValue(target);
                var min = statAttr.MinValue != null ? Convert.ToInt32(statAttr.MinValue) : int.MinValue;
                var max = statAttr.MaxValue != null ? Convert.ToInt32(statAttr.MaxValue) : int.MaxValue;
                var attribute = new Attribute<int>(currentValue, min, max);
                
                // Create wrapper that syncs with the field
                var wrapper = new FieldAttributeWrapper<int>(field, target, attribute);
                RegisterAttribute(name, wrapper);
            }
            else if (fieldType == typeof(float))
            {
                var currentValue = (float)field.GetValue(target);
                var min = statAttr.MinValue != null ? Convert.ToSingle(statAttr.MinValue) : float.MinValue;
                var max = statAttr.MaxValue != null ? Convert.ToSingle(statAttr.MaxValue) : float.MaxValue;
                var attribute = new Attribute<float>(currentValue, min, max);
                
                var wrapper = new FieldAttributeWrapper<float>(field, target, attribute);
                RegisterAttribute(name, wrapper);
            }
            // Add more types as needed
        }
        
        private void RegisterAttributeFromProperty(PropertyInfo property, object target, StatAttribute statAttr)
        {
            var propertyType = property.PropertyType;
            var name = string.IsNullOrEmpty(statAttr.DisplayName) ? property.Name : statAttr.DisplayName;
            
            // Similar implementation for properties
            if (propertyType == typeof(int))
            {
                var currentValue = (int)property.GetValue(target);
                var min = statAttr.MinValue != null ? Convert.ToInt32(statAttr.MinValue) : int.MinValue;
                var max = statAttr.MaxValue != null ? Convert.ToInt32(statAttr.MaxValue) : int.MaxValue;
                var attribute = new Attribute<int>(currentValue, min, max);
                
                var wrapper = new PropertyAttributeWrapper<int>(property, target, attribute);
                RegisterAttribute(name, wrapper);
            }
            else if (propertyType == typeof(float))
            {
                var currentValue = (float)property.GetValue(target);
                var min = statAttr.MinValue != null ? Convert.ToSingle(statAttr.MinValue) : float.MinValue;
                var max = statAttr.MaxValue != null ? Convert.ToSingle(statAttr.MaxValue) : float.MaxValue;
                var attribute = new Attribute<float>(currentValue, min, max);
                
                var wrapper = new PropertyAttributeWrapper<float>(property, target, attribute);
                RegisterAttribute(name, wrapper);
            }
        }
        
        /// <summary>
        /// Register a new attribute in the collection
        /// </summary>
        public void RegisterAttribute<T>(string name, IAttribute<T> attribute) where T : struct, IComparable<T>
        {
            lock (lockObject)
            {
                if (attributeLookup == null) attributeLookup = new Dictionary<string, IAttributeBase>();
                
                var wrapper = new AttributeWrapper<T>(attribute);
                attributeLookup[name] = wrapper;
                
                // Add to serialized list
                var entry = attributes.FirstOrDefault(a => a.name == name);
                if (entry == null)
                {
                    entry = new AttributeEntry { name = name };
                    attributes.Add(entry);
                }
                entry.attribute = wrapper;
                
                // Subscribe to changes for event forwarding
                attribute.OnValueChanged += (oldVal, newVal) => 
                {
                    OnAttributeChanged?.Invoke(name, oldVal, newVal);
                    
                    // Also publish through event bus
                    EventBus.Publish(new AttributeChangedEvent
                    {
                        AttributeName = name,
                        OldValue = oldVal,
                        NewValue = newVal,
                        Source = this
                    });
                };
            }
        }
        
        /// <summary>
        /// Get an attribute by name
        /// </summary>
        public IAttribute<T> GetAttribute<T>(string name) where T : struct, IComparable<T>
        {
            lock (lockObject)
            {
                if (attributeLookup != null && attributeLookup.TryGetValue(name, out var attr))
                {
                    if (attr is AttributeWrapper<T> wrapper)
                    {
                        return wrapper.Attribute;
                    }
                }
                return null;
            }
        }
        
        /// <summary>
        /// Get attribute value with caching
        /// </summary>
        public T GetValue<T>(string name) where T : struct, IComparable<T>
        {
            // Use cache for frequently accessed values
            var cacheKey = $"attr_value_{name}_{typeof(T).Name}";
            return cache.GetOrCalculate(cacheKey, () =>
            {
                var attr = GetAttribute<T>(name);
                return attr != null ? attr.Value : default(T);
            }, 0.1f); // 100ms cache
        }
        
        /// <summary>
        /// Set attribute value with validation and events
        /// </summary>
        public void SetValue<T>(string name, T value) where T : struct, IComparable<T>
        {
            // Validate before setting
            if (!AttributeValidation.Validate(name, value))
            {
                Debug.LogWarning($"Validation failed for attribute '{name}' with value '{value}'");
                return;
            }
            
            var attr = GetAttribute<T>(name);
            if (attr != null)
            {
                var oldValue = attr.Value;
                attr.Value = value;
                
                // Invalidate cache
                var cacheKey = $"attr_value_{name}_{typeof(T).Name}";
                cache.Invalidate(cacheKey);
                
                // Publish event through event bus
                EventBus.Publish(new AttributeChangedEvent
                {
                    AttributeName = name,
                    OldValue = oldValue,
                    NewValue = value,
                    Source = this
                });
            }
        }
        
        /// <summary>
        /// Get all attribute names
        /// </summary>
        public IEnumerable<string> GetAttributeNames()
        {
            lock (lockObject)
            {
                return attributeLookup?.Keys ?? Enumerable.Empty<string>();
            }
        }
        
        /// <summary>
        /// Check if attribute exists
        /// </summary>
        public bool HasAttribute(string name)
        {
            lock (lockObject)
            {
                return attributeLookup?.ContainsKey(name) ?? false;
            }
        }
    }
    
    /// <summary>
    /// Serializable entry for attribute storage
    /// </summary>
    [Serializable]
    internal class AttributeEntry
    {
        public string name;
        public IAttributeBase attribute;
    }
    
    /// <summary>
    /// Base interface for type-erased attribute access
    /// </summary>
    public interface IAttributeBase
    {
        object GetValue();
        void SetValue(object value);
        Type GetValueType();
    }
    
    /// <summary>
    /// Wrapper for type-erased attribute access
    /// </summary>
    internal class AttributeWrapper<T> : IAttributeBase where T : struct, IComparable<T>
    {
        public IAttribute<T> Attribute { get; }
        
        public AttributeWrapper(IAttribute<T> attribute)
        {
            Attribute = attribute;
        }
        
        public object GetValue() => Attribute.Value;
        public void SetValue(object value) => Attribute.Value = (T)value;
        public Type GetValueType() => typeof(T);
    }
    
    /// <summary>
    /// Wrapper that syncs attribute with a field
    /// </summary>
    internal class FieldAttributeWrapper<T> : IAttributeBase where T : struct, IComparable<T>
    {
        private readonly FieldInfo field;
        private readonly object target;
        private readonly IAttribute<T> attribute;
        
        public FieldAttributeWrapper(FieldInfo field, object target, IAttribute<T> attribute)
        {
            this.field = field;
            this.target = target;
            this.attribute = attribute;
            
            // Sync changes back to field
            attribute.OnValueChanged += (oldVal, newVal) => field.SetValue(target, newVal);
        }
        
        public object GetValue() => attribute.Value;
        public void SetValue(object value) => attribute.Value = (T)value;
        public Type GetValueType() => typeof(T);
    }
    
    /// <summary>
    /// Wrapper that syncs attribute with a property
    /// </summary>
    internal class PropertyAttributeWrapper<T> : IAttributeBase where T : struct, IComparable<T>
    {
        private readonly PropertyInfo property;
        private readonly object target;
        private readonly IAttribute<T> attribute;
        
        public PropertyAttributeWrapper(PropertyInfo property, object target, IAttribute<T> attribute)
        {
            this.property = property;
            this.target = target;
            this.attribute = attribute;
            
            // Sync changes back to property
            attribute.OnValueChanged += (oldVal, newVal) => property.SetValue(target, newVal);
        }
        
        public object GetValue() => attribute.Value;
        public void SetValue(object value) => attribute.Value = (T)value;
        public Type GetValueType() => typeof(T);
    }
}