using System.Linq;
using UnityEngine;
using StatForge.Core;

namespace StatForge
{
    /// <summary>
    /// Modern, simplified component that provides the [Stat] API while maintaining compatibility
    /// Can be used standalone or alongside the traditional AttributeSystem
    /// </summary>
    public class StatForgeComponent : MonoBehaviour
    {
        [Header("Auto-Discovery")]
        [SerializeField] private bool autoDiscoverOnAwake = true;
        [SerializeField] private bool syncWithLegacySystem = true;
        
        [Header("Runtime Info")]
        [SerializeField] private AttributeCollection attributes = new AttributeCollection();
        
        // Legacy system integration
        private AttributeSystem legacySystem;
        
        public AttributeCollection Attributes => attributes;
        
        private void Awake()
        {
            if (autoDiscoverOnAwake)
            {
                InitializeAttributes();
            }
            
            if (syncWithLegacySystem)
            {
                legacySystem = GetComponent<AttributeSystem>();
            }
        }
        
        /// <summary>
        /// Initialize the attribute system and discover [Stat] fields
        /// </summary>
        public void InitializeAttributes()
        {
            attributes.Initialize(this);
            
            // Subscribe to changes for legacy system sync
            if (syncWithLegacySystem && legacySystem != null)
            {
                attributes.OnAttributeChanged += OnAttributeChangedHandler;
            }
        }
        
        private void OnAttributeChangedHandler(string name, object oldValue, object newValue)
        {
            // Sync changes to legacy system if available
            if (legacySystem?.RuntimeContainer != null)
            {
                // Try to find matching StatType by name
                var matchingStats = legacySystem.GetAllStats()
                    .Where(s => s.statType.DisplayName == name || s.statType.ShortName == name);
                    
                foreach (var stat in matchingStats)
                {
                    if (newValue is float floatVal)
                    {
                        stat.SetBaseValue(floatVal);
                    }
                    else if (newValue is int intVal)
                    {
                        stat.SetBaseValue(intVal);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get attribute value with natural syntax
        /// </summary>
        public T Get<T>(string name) where T : struct, System.IComparable<T>
        {
            return attributes.GetValue<T>(name);
        }
        
        /// <summary>
        /// Set attribute value with natural syntax
        /// </summary>
        public void Set<T>(string name, T value) where T : struct, System.IComparable<T>
        {
            attributes.SetValue(name, value);
        }
        
        /// <summary>
        /// Add temporary modifier to an attribute
        /// </summary>
        public void AddModifier<T>(string name, T value, float duration = 0f) where T : struct, System.IComparable<T>
        {
            var attr = attributes.GetAttribute<T>(name);
            attr?.AddModifier(value, duration);
        }
        
        /// <summary>
        /// Remove modifier from an attribute
        /// </summary>
        public void RemoveModifier<T>(string name, T value) where T : struct, System.IComparable<T>
        {
            var attr = attributes.GetAttribute<T>(name);
            attr?.RemoveModifier(value);
        }
        
        /// <summary>
        /// Subscribe to attribute changes
        /// </summary>
        public void OnAttributeChanged(string name, System.Action<object, object> callback)
        {
            attributes.OnAttributeChanged += (attrName, oldVal, newVal) =>
            {
                if (attrName == name)
                {
                    callback(oldVal, newVal);
                }
            };
        }
        
        /// <summary>
        /// Query system for fluent operations
        /// </summary>
        public AttributeQuery Query()
        {
            return new AttributeQuery(attributes);
        }
    }
    
    /// <summary>
    /// Fluent query system for attributes
    /// </summary>
    public class AttributeQuery
    {
        private readonly AttributeCollection attributes;
        private System.Func<string, bool> filter;
        
        public AttributeQuery(AttributeCollection attributes)
        {
            this.attributes = attributes;
        }
        
        public AttributeQuery Where(System.Func<string, bool> predicate)
        {
            filter = predicate;
            return this;
        }
        
        public System.Collections.Generic.IEnumerable<string> Select()
        {
            var names = attributes.GetAttributeNames();
            return filter != null ? names.Where(filter) : names;
        }
        
        public T Sum<T>() where T : struct, System.IComparable<T>
        {
            dynamic total = default(T);
            foreach (var name in Select())
            {
                total += (dynamic)attributes.GetValue<T>(name);
            }
            return (T)total;
        }
        
        public T Max<T>() where T : struct, System.IComparable<T>
        {
            var values = Select().Select(name => attributes.GetValue<T>(name));
            return values.Any() ? values.Max() : default(T);
        }
        
        public T Min<T>() where T : struct, System.IComparable<T>
        {
            var values = Select().Select(name => attributes.GetValue<T>(name));
            return values.Any() ? values.Min() : default(T);
        }
    }
}