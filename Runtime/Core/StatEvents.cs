using System;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Global event system for stat changes and modifications.
    /// Provides centralized event handling for all stat operations.
    /// </summary>
    public static class StatEvents
    {
        /// <summary>
        /// Fired when any stat value changes.
        /// Parameters: (GameObject owner, string statName, float oldValue, float newValue)
        /// </summary>
        public static event Action<GameObject, string, float, float> OnStatChanged;
        
        /// <summary>
        /// Fired when a modifier is added to a stat.
        /// Parameters: (GameObject owner, string statName, IStatModifier modifier)
        /// </summary>
        public static event Action<GameObject, string, IStatModifier> OnModifierAdded;
        
        /// <summary>
        /// Fired when a modifier is removed from a stat.
        /// Parameters: (GameObject owner, string statName, IStatModifier modifier)
        /// </summary>
        public static event Action<GameObject, string, IStatModifier> OnModifierRemoved;
        
        /// <summary>
        /// Fired when a stat is initialized on an object.
        /// Parameters: (GameObject owner, string statName, float initialValue)
        /// </summary>
        public static event Action<GameObject, string, float> OnStatInitialized;
        
        /// <summary>
        /// Fired when stats are cleared from an object.
        /// Parameters: (GameObject owner)
        /// </summary>
        public static event Action<GameObject> OnStatsCleared;
        
        /// <summary>
        /// Internal method to trigger stat changed events.
        /// </summary>
        internal static void TriggerStatChanged(GameObject owner, string statName, float oldValue, float newValue)
        {
            try
            {
                OnStatChanged?.Invoke(owner, statName, oldValue, newValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in StatChanged event handler: {e.Message}");
            }
        }
        
        /// <summary>
        /// Internal method to trigger modifier added events.
        /// </summary>
        internal static void TriggerModifierAdded(GameObject owner, string statName, IStatModifier modifier)
        {
            try
            {
                OnModifierAdded?.Invoke(owner, statName, modifier);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ModifierAdded event handler: {e.Message}");
            }
        }
        
        /// <summary>
        /// Internal method to trigger modifier removed events.
        /// </summary>
        internal static void TriggerModifierRemoved(GameObject owner, string statName, IStatModifier modifier)
        {
            try
            {
                OnModifierRemoved?.Invoke(owner, statName, modifier);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ModifierRemoved event handler: {e.Message}");
            }
        }
        
        /// <summary>
        /// Internal method to trigger stat initialized events.
        /// </summary>
        internal static void TriggerStatInitialized(GameObject owner, string statName, float initialValue)
        {
            try
            {
                OnStatInitialized?.Invoke(owner, statName, initialValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in StatInitialized event handler: {e.Message}");
            }
        }
        
        /// <summary>
        /// Internal method to trigger stats cleared events.
        /// </summary>
        internal static void TriggerStatsCleared(GameObject owner)
        {
            try
            {
                OnStatsCleared?.Invoke(owner);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in StatsCleared event handler: {e.Message}");
            }
        }
    }
}