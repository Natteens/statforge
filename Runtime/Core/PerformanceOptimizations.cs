using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace StatForge.Core
{
    /// <summary>
    /// High-performance caching system for stat calculations and lookups
    /// </summary>
    public class StatCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> cache = 
            new ConcurrentDictionary<string, CacheEntry>();
        private readonly ConcurrentDictionary<string, DateTime> lastAccess = 
            new ConcurrentDictionary<string, DateTime>();
        
        public float DefaultTtl { get; set; } = 1f; // 1 second default TTL
        public int MaxEntries { get; set; } = 1000;
        
        /// <summary>
        /// Get cached value or calculate and cache it
        /// </summary>
        public T GetOrCalculate<T>(string key, Func<T> calculator, float? ttl = null)
        {
            var now = DateTime.UtcNow;
            var entryTtl = ttl ?? DefaultTtl;
            
            if (cache.TryGetValue(key, out var entry))
            {
                if ((now - entry.CreatedAt).TotalSeconds < entryTtl)
                {
                    lastAccess[key] = now;
                    return (T)entry.Value;
                }
                
                // Expired, remove from cache
                cache.TryRemove(key, out _);
                lastAccess.TryRemove(key, out _);
            }
            
            // Calculate new value
            var value = calculator();
            
            // Add to cache if under limit
            if (cache.Count < MaxEntries)
            {
                cache[key] = new CacheEntry { Value = value, CreatedAt = now };
                lastAccess[key] = now;
            }
            else
            {
                // Cache is full, remove oldest entry
                CleanupOldestEntry();
                cache[key] = new CacheEntry { Value = value, CreatedAt = now };
                lastAccess[key] = now;
            }
            
            return value;
        }
        
        /// <summary>
        /// Invalidate cache entry
        /// </summary>
        public void Invalidate(string key)
        {
            cache.TryRemove(key, out _);
            lastAccess.TryRemove(key, out _);
        }
        
        /// <summary>
        /// Clear all cache entries
        /// </summary>
        public void Clear()
        {
            cache.Clear();
            lastAccess.Clear();
        }
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            return new CacheStats
            {
                EntryCount = cache.Count,
                MaxEntries = MaxEntries,
                DefaultTtl = DefaultTtl
            };
        }
        
        private void CleanupOldestEntry()
        {
            var oldestKey = "";
            var oldestTime = DateTime.MaxValue;
            
            foreach (var kvp in lastAccess)
            {
                if (kvp.Value < oldestTime)
                {
                    oldestTime = kvp.Value;
                    oldestKey = kvp.Key;
                }
            }
            
            if (!string.IsNullOrEmpty(oldestKey))
            {
                cache.TryRemove(oldestKey, out _);
                lastAccess.TryRemove(oldestKey, out _);
            }
        }
        
        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        
        public struct CacheStats
        {
            public int EntryCount;
            public int MaxEntries;
            public float DefaultTtl;
        }
    }
    
    /// <summary>
    /// Lazy loading container for expensive attribute operations
    /// </summary>
    public class LazyAttribute<T> where T : struct, IComparable<T>
    {
        private readonly Func<T> valueCalculator;
        private readonly Func<bool> shouldRecalculate;
        private T cachedValue;
        private bool isCalculated;
        private DateTime lastCalculation;
        
        public LazyAttribute(Func<T> calculator, Func<bool> invalidationCheck = null)
        {
            valueCalculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            shouldRecalculate = invalidationCheck ?? (() => false);
        }
        
        public T Value
        {
            get
            {
                if (!isCalculated || shouldRecalculate())
                {
                    RecalculateValue();
                }
                return cachedValue;
            }
        }
        
        public bool IsCalculated => isCalculated;
        public DateTime LastCalculation => lastCalculation;
        
        public void ForceRecalculation()
        {
            RecalculateValue();
        }
        
        public void Invalidate()
        {
            isCalculated = false;
        }
        
        private void RecalculateValue()
        {
            cachedValue = valueCalculator();
            isCalculated = true;
            lastCalculation = DateTime.UtcNow;
        }
        
        // Implicit conversion for natural syntax
        public static implicit operator T(LazyAttribute<T> lazy)
        {
            return lazy.Value;
        }
    }
    
    /// <summary>
    /// Batch operations manager for multiple attribute changes
    /// </summary>
    public class AttributeBatch : IDisposable
    {
        private readonly List<Action> deferredActions = new List<Action>();
        private readonly List<IEvent> deferredEvents = new List<IEvent>();
        private bool isCommitted;
        private bool isDisposed;
        
        /// <summary>
        /// Add an action to be executed when batch is committed
        /// </summary>
        public void AddAction(Action action)
        {
            if (isCommitted) throw new InvalidOperationException("Batch already committed");
            deferredActions.Add(action);
        }
        
        /// <summary>
        /// Add an event to be published when batch is committed
        /// </summary>
        public void AddEvent(IEvent eventData)
        {
            if (isCommitted) throw new InvalidOperationException("Batch already committed");
            deferredEvents.Add(eventData);
        }
        
        /// <summary>
        /// Commit all deferred operations
        /// </summary>
        public void Commit()
        {
            if (isCommitted) return;
            
            try
            {
                // Execute all actions
                foreach (var action in deferredActions)
                {
                    action();
                }
                
                // Publish all events
                foreach (var eventData in deferredEvents)
                {
                    EventBus.Publish(eventData);
                }
                
                isCommitted = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
        
        /// <summary>
        /// Rollback all changes (not implemented - for future extension)
        /// </summary>
        public void Rollback()
        {
            deferredActions.Clear();
            deferredEvents.Clear();
        }
        
        public void Dispose()
        {
            if (!isDisposed)
            {
                if (!isCommitted)
                {
                    Commit(); // Auto-commit on dispose
                }
                
                deferredActions.Clear();
                deferredEvents.Clear();
                isDisposed = true;
            }
        }
    }
    
    /// <summary>
    /// Validation system for attribute values
    /// </summary>
    public static class AttributeValidation
    {
        private static readonly ConcurrentDictionary<string, List<Func<object, bool>>> validators = 
            new ConcurrentDictionary<string, List<Func<object, bool>>>();
        
        /// <summary>
        /// Add a validation rule for an attribute
        /// </summary>
        public static void AddRule(string attributeName, Func<object, bool> validator)
        {
            var rules = validators.GetOrAdd(attributeName, _ => new List<Func<object, bool>>());
            lock (rules)
            {
                rules.Add(validator);
            }
        }
        
        /// <summary>
        /// Validate a value for an attribute
        /// </summary>
        public static bool Validate(string attributeName, object value)
        {
            if (!validators.TryGetValue(attributeName, out var rules))
                return true;
            
            lock (rules)
            {
                foreach (var rule in rules)
                {
                    if (!rule(value))
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Remove all validation rules for an attribute
        /// </summary>
        public static void ClearRules(string attributeName)
        {
            validators.TryRemove(attributeName, out _);
        }
        
        /// <summary>
        /// Add common validation rules
        /// </summary>
        public static class CommonRules
        {
            public static Func<object, bool> PositiveNumbers() => 
                value => value is IComparable comparable && comparable.CompareTo(0) > 0;
            
            public static Func<object, bool> Range<T>(T min, T max) where T : IComparable<T> =>
                value => value is T typedValue && typedValue.CompareTo(min) >= 0 && typedValue.CompareTo(max) <= 0;
            
            public static Func<object, bool> NotNull() =>
                value => value != null;
        }
    }
}