using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StatForge
{
    /// <summary>
    /// Enhanced extension methods for Stat objects and GameObject stat management.
    /// Provides convenience methods for common stat operations.
    /// </summary>
    public static class StatExtensionsV2
    {
        /// <summary>
        /// Creates a new stat from a StatDefinition.
        /// </summary>
        public static Stat CreateStat(this StatDefinition definition)
        {
            if (definition == null) return null;
            return definition.CreateStat();
        }
        
        /// <summary>
        /// Applies a definition to an existing stat.
        /// </summary>
        public static void ApplyDefinition(this Stat stat, StatDefinition definition)
        {
            if (stat != null && definition != null)
            {
                stat.Definition = definition;
            }
        }
        
        /// <summary>
        /// Gets or creates a stat with auto-initialization.
        /// This method enables zero-setup stat usage.
        /// </summary>
        public static Stat GetOrCreateStat(this GameObject gameObject, string statName, float defaultValue = 0f)
        {
            if (gameObject == null) return null;
            
            // First try to get an existing Stat object from components
            var existingStat = gameObject.GetStatObject(statName);
            if (existingStat != null)
            {
                return existingStat;
            }
            
            // If not found, create a new one and register it in the legacy system
            var stat = new Stat(statName, defaultValue);
            stat.Owner = gameObject;
            
            // Also register in the legacy system for compatibility
            gameObject.SetStat(statName, defaultValue);
            
            return stat;
        }
        
        /// <summary>
        /// Gets or creates a stat from a definition with auto-initialization.
        /// </summary>
        public static Stat GetOrCreateStat(this GameObject gameObject, StatDefinition definition)
        {
            if (gameObject == null || definition == null) return null;
            
            var stat = gameObject.GetOrCreateStat(definition.StatName);
            stat.Definition = definition;
            return stat;
        }
        
        /// <summary>
        /// Adds multiple stats from definitions at once.
        /// </summary>
        public static void AddStats(this GameObject gameObject, params StatDefinition[] definitions)
        {
            if (gameObject == null || definitions == null) return;
            
            gameObject.InitializeStats();
            foreach (var definition in definitions)
            {
                if (definition != null)
                {
                    gameObject.GetOrCreateStat(definition);
                }
            }
        }
        
        /// <summary>
        /// Gets all stats that match a certain criteria.
        /// </summary>
        public static IEnumerable<Stat> GetStatsWhere(this GameObject gameObject, System.Func<Stat, bool> predicate)
        {
            if (gameObject == null) return System.Linq.Enumerable.Empty<Stat>();
            
            var allStats = gameObject.GetAllStatObjects();
            return allStats.Where(predicate);
        }
        
        /// <summary>
        /// Gets all stats in a specific category.
        /// </summary>
        public static IEnumerable<Stat> GetStatsByCategory(this GameObject gameObject, string category)
        {
            return gameObject.GetStatsWhere(stat => 
                stat.Definition != null && stat.Definition.Category == category);
        }
        
        /// <summary>
        /// Applies a modifier to multiple stats at once.
        /// </summary>
        public static void ApplyModifierToStats(this GameObject gameObject, IStatModifier modifier, params string[] statNames)
        {
            if (gameObject == null || modifier == null || statNames == null) return;
            
            foreach (var statName in statNames)
            {
                var stat = gameObject.GetOrCreateStat(statName);
                stat?.AddModifier(modifier);
            }
        }
        
        /// <summary>
        /// Buffs multiple stats by the same amount.
        /// </summary>
        public static void BuffStats(this GameObject gameObject, float value, float duration = -1f, params string[] statNames)
        {
            if (gameObject == null || statNames == null) return;
            
            foreach (var statName in statNames)
            {
                var stat = gameObject.GetOrCreateStat(statName);
                stat?.Buff(value, duration);
            }
        }
        
        /// <summary>
        /// Debuffs multiple stats by the same amount.
        /// </summary>
        public static void DebuffStats(this GameObject gameObject, float value, float duration = -1f, params string[] statNames)
        {
            if (gameObject == null || statNames == null) return;
            
            foreach (var statName in statNames)
            {
                var stat = gameObject.GetOrCreateStat(statName);
                stat?.Debuff(value, duration);
            }
        }
        
        /// <summary>
        /// Clears all modifiers from all stats.
        /// </summary>
        public static void ClearAllStatModifiers(this GameObject gameObject)
        {
            if (gameObject == null) return;
            
            var allStats = gameObject.GetAllStatObjects();
            foreach (var stat in allStats)
            {
                stat.ClearModifiers();
            }
        }
        
        /// <summary>
        /// Gets the total value of all stats.
        /// </summary>
        public static float GetTotalStatValue(this GameObject gameObject)
        {
            if (gameObject == null) return 0f;
            
            var allStats = gameObject.GetAllStatObjects();
            return allStats.Sum(stat => stat.Value);
        }
        
        /// <summary>
        /// Gets the average value of all stats.
        /// </summary>
        public static float GetAverageStatValue(this GameObject gameObject)
        {
            if (gameObject == null) return 0f;
            
            var allStats = gameObject.GetAllStatObjects().ToList();
            if (allStats.Count == 0) return 0f;
            
            return allStats.Average(stat => stat.Value);
        }
        
        /// <summary>
        /// Finds the stat with the highest value.
        /// </summary>
        public static Stat GetHighestStat(this GameObject gameObject)
        {
            if (gameObject == null) return null;
            
            var allStats = gameObject.GetAllStatObjects();
            return allStats.OrderByDescending(stat => stat.Value).FirstOrDefault();
        }
        
        /// <summary>
        /// Finds the stat with the lowest value.
        /// </summary>
        public static Stat GetLowestStat(this GameObject gameObject)
        {
            if (gameObject == null) return null;
            
            var allStats = gameObject.GetAllStatObjects();
            return allStats.OrderBy(stat => stat.Value).FirstOrDefault();
        }
        
        /// <summary>
        /// Scales all stats by a percentage.
        /// </summary>
        public static void ScaleAllStats(this GameObject gameObject, float percentage, float duration = -1f)
        {
            if (gameObject == null) return;
            
            var allStats = gameObject.GetAllStatObjects();
            foreach (var stat in allStats)
            {
                stat.Multiply(percentage, duration);
            }
        }
        
        /// <summary>
        /// Resets all stats to their base values (removes modifiers).
        /// </summary>
        public static void ResetAllStats(this GameObject gameObject)
        {
            if (gameObject == null) return;
            
            var allStats = gameObject.GetAllStatObjects();
            foreach (var stat in allStats)
            {
                stat.ClearModifiers();
            }
        }
        
        /// <summary>
        /// Creates a snapshot of all current stat values.
        /// </summary>
        public static Dictionary<string, float> CreateStatSnapshot(this GameObject gameObject)
        {
            var snapshot = new Dictionary<string, float>();
            if (gameObject == null) return snapshot;
            
            var allStats = gameObject.GetAllStatObjects();
            foreach (var stat in allStats)
            {
                snapshot[stat.Name] = stat.Value;
            }
            
            return snapshot;
        }
        
        /// <summary>
        /// Restores stat values from a snapshot.
        /// </summary>
        public static void RestoreFromSnapshot(this GameObject gameObject, Dictionary<string, float> snapshot)
        {
            if (gameObject == null || snapshot == null) return;
            
            foreach (var kvp in snapshot)
            {
                var stat = gameObject.GetOrCreateStat(kvp.Key);
                if (stat != null)
                {
                    stat.Value = kvp.Value;
                }
            }
        }
    }
}