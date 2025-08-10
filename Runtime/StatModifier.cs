using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public class StatModifier : IStatModifier
    {
        [SerializeField] private string id;
        [SerializeField] private float value;
        [SerializeField] private ModifierType type;
        [SerializeField] private ModifierDuration duration;
        [SerializeField] private ModifierPriority priority;
        [SerializeField] private float remainingTime;
        [SerializeField] private string source;
        
        private Stat targetStat;
        private Func<bool> removalCondition;
        private object tag;
        
        public string Id => id;
        public Stat TargetStat => targetStat;
        public float Value => value;
        public ModifierType Type => type;
        public ModifierDuration Duration => duration;
        public ModifierPriority Priority => priority;
        public float RemainingTime => remainingTime;
        public bool IsExpired => duration == ModifierDuration.Temporary && remainingTime <= 0f;
        public string Source => source;
        public object Tag => tag;
        
        public StatModifier(Stat targetStat, float value, ModifierType type = ModifierType.Additive, 
                           ModifierDuration duration = ModifierDuration.Permanent, float time = 0f,
                           ModifierPriority priority = ModifierPriority.Normal, string source = "", object tag = null)
        {
            this.id = StatIdPool.GetId();
            this.targetStat = targetStat;
            this.value = value;
            this.type = type;
            this.duration = duration;
            this.priority = priority;
            this.remainingTime = time;
            this.source = source ?? "";
            this.tag = tag;
        }
        
        public bool Update(float deltaTime)
        {
            if (duration == ModifierDuration.Temporary)
            {
                remainingTime -= deltaTime;
                return remainingTime <= 0f;
            }
            
            if (duration == ModifierDuration.Conditional && removalCondition != null)
            {
                try
                {
                    return removalCondition();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro na condição do modificador {id}: {e.Message}");
                    return true;
                }
            }
            
            return false;
        }
        
        public bool ShouldRemove()
        {
            return IsExpired || (duration == ModifierDuration.Conditional && removalCondition?.Invoke() == true);
        }
        
        public void SetCondition(Func<bool> condition)
        {
            removalCondition = condition;
        }
        
        public IStatModifier Clone()
        {
            var clone = new StatModifier(targetStat, value, type, duration, remainingTime, priority, source, tag);
            clone.removalCondition = removalCondition;
            return clone;
        }
        
        public override string ToString()
        {
            var sign = type == ModifierType.Subtractive ? "-" : "+";
            var suffix = type == ModifierType.Multiplicative ? "x" : 
                        type == ModifierType.Percentage ? "%" : "";
            return $"{sign}{value}{suffix} ({source})";
        }
    }
    
    public static class StatModifierManager
    {
        private static readonly Dictionary<Stat, List<IStatModifier>> temporaryModifiers = new();
        private static readonly HashSet<Stat> trackedStats = new();
        private static StatUpdateManager updateManager;
        private static bool initialized;
        internal static bool isShuttingDown;
        
        static StatModifierManager()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            isShuttingDown = false;
            
            CreateUpdateManager();
            
            Application.quitting += OnApplicationQuitting;
        }
        
        private static void OnApplicationQuitting()
        {
            isShuttingDown = true;
            ClearAllCaches();
        }
        
        private static void CreateUpdateManager()
        {
            if (isShuttingDown) return;
            
            var go = new GameObject("[StatForge] Update Manager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            
            updateManager = go.AddComponent<StatUpdateManager>();
        }
        
        public static void RegisterTemporaryModifier(Stat stat, IStatModifier modifier)
        {
            if (stat == null || modifier?.Duration != ModifierDuration.Temporary || isShuttingDown) return;
            
            if (!temporaryModifiers.TryGetValue(stat, out var modifiers))
            {
                modifiers = new List<IStatModifier>();
                temporaryModifiers[stat] = modifiers;
            }
            
            modifiers.Add(modifier);
            trackedStats.Add(stat);
        }
        
        public static void UnregisterStat(Stat stat)
        {
            if (stat == null) return;
            
            temporaryModifiers.Remove(stat);
            trackedStats.Remove(stat);
        }
        
        public static bool IsTrackedForTemporary(Stat stat)
        {
            return trackedStats.Contains(stat);
        }
        
        public static void UpdateAllTrackedStats()
        {
            if (!Application.isPlaying || isShuttingDown) return;
            
            var deltaTime = Time.deltaTime;
            var validStatsToRemove = new List<Stat>();
            bool foundNullStats = false;
            
            var trackedStatsCopy = new List<Stat>(trackedStats);
            
            foreach (var stat in trackedStatsCopy)
            {
                if (ReferenceEquals(stat, null))
                {
                    foundNullStats = true;
                    continue;
                }
                
                stat.ForceUpdateModifiers(deltaTime);
                
                if (!stat.HasModifiers || !temporaryModifiers.ContainsKey(stat))
                {
                    validStatsToRemove.Add(stat);
                }
            }
            
            if (foundNullStats)
            {
                CleanupNullReferences();
            }
            
            foreach (var stat in validStatsToRemove)
            {
                UnregisterStat(stat);
            }
        }
        
        private static void CleanupNullReferences()
        {
            trackedStats.RemoveWhere(stat => ReferenceEquals(stat, null));
            var keysToRemove = new List<Stat>();
            foreach (var kvp in temporaryModifiers)
            {
                if (ReferenceEquals(kvp.Key, null))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                temporaryModifiers.Remove(key);
            }
        }
        
        public static string GetGlobalDebugInfo()
        {
            return $"Tracked stats: {trackedStats.Count}";
        }
        
        internal static void ClearDataOnly()
        {
            temporaryModifiers.Clear();
            trackedStats.Clear();
        }
        
        public static void ClearAllCaches()
        {
            temporaryModifiers.Clear();
            trackedStats.Clear();
            
            if (updateManager != null && !ReferenceEquals(updateManager, null))
            {
                try
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(updateManager.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(updateManager.gameObject);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[StatForge] Could not destroy update manager: {ex.GetType().Name}");
                }
                finally
                {
                    updateManager = null;
                }
            }
            
            initialized = false;
        }
    }
    
    internal class StatUpdateManager : MonoBehaviour
    {
        private bool hasBeenDestroyed;
        
        private void Update()
        {
            if (!hasBeenDestroyed)
            {
                StatModifierManager.UpdateAllTrackedStats();
            }
        }
        
        private void OnDestroy()
        {
            if (hasBeenDestroyed) return;
            
            hasBeenDestroyed = true;
            if (!StatModifierManager.isShuttingDown)
            {
                StatModifierManager.ClearDataOnly();
            }
        }
    }
}