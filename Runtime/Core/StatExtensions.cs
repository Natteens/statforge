using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Extension methods for MonoBehaviour to provide simple stat management.
    /// Enables the simplified API: this.SetStat("health", 100f) and this.GetStat("health").
    /// </summary>
    public static class StatExtensions
    {
        private static readonly Dictionary<GameObject, StatCollection> _statCollections = new Dictionary<GameObject, StatCollection>();
        private static readonly Dictionary<GameObject, Dictionary<string, FieldInfo>> _fieldCache = new Dictionary<GameObject, Dictionary<string, FieldInfo>>();
        
        /// <summary>
        /// Gets the value of a stat on this GameObject.
        /// </summary>
        public static float GetStat(this MonoBehaviour behaviour, string statName)
        {
            return GetStat(behaviour.gameObject, statName);
        }
        
        /// <summary>
        /// Gets the value of a stat on a GameObject.
        /// </summary>
        public static float GetStat(this GameObject gameObject, string statName)
        {
            InitializeIfNeeded(gameObject);
            
            // First check if this is a [Stat] field
            var fieldValue = GetStatFieldValue(gameObject, statName);
            if (fieldValue.HasValue)
            {
                return fieldValue.Value;
            }
            
            // Otherwise use the stat collection
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                return collection.Get(statName);
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Sets the value of a stat on this GameObject.
        /// </summary>
        public static void SetStat(this MonoBehaviour behaviour, string statName, float value)
        {
            SetStat(behaviour.gameObject, statName, value);
        }
        
        /// <summary>
        /// Sets the value of a stat on a GameObject.
        /// </summary>
        public static void SetStat(this GameObject gameObject, string statName, float value)
        {
            InitializeIfNeeded(gameObject);
            
            // First try to set a [Stat] field
            if (TrySetStatField(gameObject, statName, value))
            {
                return;
            }
            
            // Otherwise use the stat collection
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                collection.Set(statName, value);
            }
        }
        
        /// <summary>
        /// Adds a modifier to a stat on this GameObject.
        /// </summary>
        public static void AddStatModifier(this MonoBehaviour behaviour, string statName, IStatModifier modifier)
        {
            AddStatModifier(behaviour.gameObject, statName, modifier);
        }
        
        /// <summary>
        /// Adds a modifier to a stat on a GameObject.
        /// </summary>
        public static void AddStatModifier(this GameObject gameObject, string statName, IStatModifier modifier)
        {
            InitializeIfNeeded(gameObject);
            
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                collection.AddModifier(statName, modifier);
            }
        }
        
        /// <summary>
        /// Removes a modifier from a stat on this GameObject.
        /// </summary>
        public static bool RemoveStatModifier(this MonoBehaviour behaviour, string statName, IStatModifier modifier)
        {
            return RemoveStatModifier(behaviour.gameObject, statName, modifier);
        }
        
        /// <summary>
        /// Removes a modifier from a stat on a GameObject.
        /// </summary>
        public static bool RemoveStatModifier(this GameObject gameObject, string statName, IStatModifier modifier)
        {
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                return collection.RemoveModifier(statName, modifier);
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes a modifier by ID from a stat on this GameObject.
        /// </summary>
        public static bool RemoveStatModifier(this MonoBehaviour behaviour, string statName, string modifierId)
        {
            return RemoveStatModifier(behaviour.gameObject, statName, modifierId);
        }
        
        /// <summary>
        /// Removes a modifier by ID from a stat on a GameObject.
        /// </summary>
        public static bool RemoveStatModifier(this GameObject gameObject, string statName, string modifierId)
        {
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                return collection.RemoveModifier(statName, modifierId);
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all modifiers for a stat on this GameObject.
        /// </summary>
        public static IReadOnlyList<IStatModifier> GetStatModifiers(this MonoBehaviour behaviour, string statName)
        {
            return GetStatModifiers(behaviour.gameObject, statName);
        }
        
        /// <summary>
        /// Gets all modifiers for a stat on a GameObject.
        /// </summary>
        public static IReadOnlyList<IStatModifier> GetStatModifiers(this GameObject gameObject, string statName)
        {
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                return collection.GetModifiers(statName);
            }
            
            return new List<IStatModifier>();
        }
        
        /// <summary>
        /// Gets the StatCollection for this GameObject.
        /// </summary>
        public static StatCollection GetStatCollection(this MonoBehaviour behaviour)
        {
            return GetStatCollection(behaviour.gameObject);
        }
        
        /// <summary>
        /// Gets the StatCollection for a GameObject.
        /// </summary>
        public static StatCollection GetStatCollection(this GameObject gameObject)
        {
            InitializeIfNeeded(gameObject);
            return _statCollections.GetValueOrDefault(gameObject);
        }
        
        /// <summary>
        /// Applies a temporary modifier that auto-removes after duration.
        /// </summary>
        public static IStatModifier ApplyTemporaryModifier(this MonoBehaviour behaviour, string statName, ModifierType type, float value, float duration)
        {
            return ApplyTemporaryModifier(behaviour.gameObject, statName, type, value, duration);
        }
        
        /// <summary>
        /// Applies a temporary modifier that auto-removes after duration.
        /// </summary>
        public static IStatModifier ApplyTemporaryModifier(this GameObject gameObject, string statName, ModifierType type, float value, float duration)
        {
            var modifier = new StatModifier(gameObject, statName, type, value, duration);
            AddStatModifier(gameObject, statName, modifier);
            return modifier;
        }
        
        /// <summary>
        /// Updates all stat modifiers for this GameObject (called automatically by StatManager).
        /// </summary>
        public static void UpdateStats(this GameObject gameObject, float deltaTime)
        {
            if (_statCollections.TryGetValue(gameObject, out var collection))
            {
                collection.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Initializes stats from [Stat] attributes on this GameObject.
        /// </summary>
        public static void InitializeStats(this MonoBehaviour behaviour)
        {
            InitializeStats(behaviour.gameObject);
        }
        
        /// <summary>
        /// Initializes stats from [Stat] attributes on a GameObject.
        /// </summary>
        public static void InitializeStats(this GameObject gameObject)
        {
            InitializeIfNeeded(gameObject);
            ScanForStatFields(gameObject);
        }
        
        private static void InitializeIfNeeded(GameObject gameObject)
        {
            if (gameObject == null) return;
            
            if (!_statCollections.ContainsKey(gameObject))
            {
                var collection = new StatCollection();
                collection.Initialize(gameObject);
                _statCollections[gameObject] = collection;
                
                // Register with StatManager for automatic updates
                StatManager.Register(gameObject);
                
                // Cleanup when GameObject is destroyed
                var cleanup = gameObject.GetComponent<StatCleanup>();
                if (cleanup == null)
                {
                    cleanup = gameObject.AddComponent<StatCleanup>();
                    cleanup.hideFlags = HideFlags.HideInInspector;
                }
            }
        }
        
        private static void ScanForStatFields(GameObject gameObject)
        {
            if (!_fieldCache.ContainsKey(gameObject))
            {
                _fieldCache[gameObject] = new Dictionary<string, FieldInfo>();
                
                var components = gameObject.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var type = component.GetType();
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    foreach (var field in fields)
                    {
                        var statAttr = field.GetCustomAttribute<StatAttribute>();
                        if (statAttr != null && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                        {
                            var statName = !string.IsNullOrEmpty(statAttr.Name) ? statAttr.Name : field.Name;
                            _fieldCache[gameObject][statName] = field;
                            
                            // Initialize stat with field value
                            var value = Convert.ToSingle(field.GetValue(component));
                            if (_statCollections.TryGetValue(gameObject, out var collection))
                            {
                                collection.Set(statName, value);
                            }
                        }
                    }
                }
            }
        }
        
        private static float? GetStatFieldValue(GameObject gameObject, string statName)
        {
            if (_fieldCache.TryGetValue(gameObject, out var fields) && fields.TryGetValue(statName, out var field))
            {
                var component = gameObject.GetComponent(field.DeclaringType);
                if (component != null)
                {
                    return Convert.ToSingle(field.GetValue(component));
                }
            }
            
            return null;
        }
        
        private static bool TrySetStatField(GameObject gameObject, string statName, float value)
        {
            if (_fieldCache.TryGetValue(gameObject, out var fields) && fields.TryGetValue(statName, out var field))
            {
                var component = gameObject.GetComponent(field.DeclaringType);
                if (component != null)
                {
                    if (field.FieldType == typeof(float))
                    {
                        field.SetValue(component, value);
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        field.SetValue(component, Mathf.RoundToInt(value));
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Internal cleanup for when GameObjects are destroyed.
        /// </summary>
        internal static void CleanupGameObject(GameObject gameObject)
        {
            StatManager.Unregister(gameObject);
            _statCollections.Remove(gameObject);
            _fieldCache.Remove(gameObject);
        }
    }
    
    /// <summary>
    /// Internal component for cleanup when GameObject is destroyed.
    /// </summary>
    internal class StatCleanup : MonoBehaviour
    {
        private void OnDestroy()
        {
            StatExtensions.CleanupGameObject(gameObject);
        }
    }
}