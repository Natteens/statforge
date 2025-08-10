using System;
using System.Collections.Generic;
using System.Linq;
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
            if (duration == ModifierDuration.Temporary && remainingTime > 0f)
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
        private static readonly List<Stat> allStatsWithTemporaryModifiers = new List<Stat>();
        private static bool globalUpdateInitialized = false;
        private static float lastUpdateTime = 0f;
        
        static StatModifierManager()
        {
            InitializeGlobalUpdate();
        }
        
        private static void InitializeGlobalUpdate()
        {
            if (globalUpdateInitialized) return;
            
            globalUpdateInitialized = true;
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update += GlobalUpdateAllStats;
            #endif
            
           // Debug.Log("[StatForge] Sistema de auto-update global inicializado!");
        }
        
        private static void GlobalUpdateAllStats()
        {
            if (allStatsWithTemporaryModifiers.Count == 0) return;
            
            var currentTime = Time.realtimeSinceStartup;
            var deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;
            
            if (deltaTime <= 0f || deltaTime > 1f) 
                deltaTime = 0.016f;
            
            for (int i = allStatsWithTemporaryModifiers.Count - 1; i >= 0; i--)
            {
                if (i < allStatsWithTemporaryModifiers.Count)
                {
                    var stat = allStatsWithTemporaryModifiers[i];
                    if (stat != null)
                    {
                        ForceUpdateStat(stat, deltaTime);
                    }
                    else
                    {
                        allStatsWithTemporaryModifiers.RemoveAt(i);
                    }
                }
            }
        }
        
        private static void ForceUpdateStat(Stat stat, float deltaTime)
        {
            try
            {
                var modifiers = stat.Modifiers as List<IStatModifier>;
                if (modifiers == null) return;
                
                bool removedAny = false;
                bool hasAnyTemporary = false;
                
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    var modifier = modifiers[i];
                    if (modifier == null) continue;
                    
                    if (modifier.Duration == ModifierDuration.Temporary)
                    {
                        hasAnyTemporary = true;
                        
                        if (modifier.Update(deltaTime) || modifier.ShouldRemove())
                        {
                            Debug.Log($"[StatForge] Modificador {modifier.Id} expirou em {stat.Name} (update forçado)");
                            
                            stat.RemoveModifier(modifier);
                            removedAny = true;
                        }
                    }
                    else if (modifier.Duration == ModifierDuration.Conditional)
                    {
                        if (modifier.ShouldRemove())
                        {
                            Debug.Log($"[StatForge] Modificador condicional {modifier.Id} removido de {stat.Name} (update forçado)");
                            
                            stat.RemoveModifier(modifier);
                            removedAny = true;
                        }
                    }
                }
                
                UpdateTemporaryTracking(stat, hasAnyTemporary);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Erro no update forçado de {stat.Name}: {ex}");
            }
        }
        
        public static void RegisterStat(Stat stat)
        {
        }
        
        public static void UpdateTemporaryTracking(Stat stat, bool hasTemporary)
        {
            if (hasTemporary)
            {
                if (!allStatsWithTemporaryModifiers.Contains(stat))
                {
                    allStatsWithTemporaryModifiers.Add(stat);
                    Debug.Log($"[StatForge] Stat {stat.Name} adicionado ao tracking global");
                }
            }
            else
            {
                if (allStatsWithTemporaryModifiers.Remove(stat))
                {
                    Debug.Log($"[StatForge] Stat {stat.Name} removido do tracking global");
                }
            }
        }
        
        public static bool IsTrackedForTemporary(Stat stat)
        {
            return allStatsWithTemporaryModifiers.Contains(stat);
        }
        
        public static string GetGlobalDebugInfo()
        {
            return $"[StatForge Global] Stats com modificadores temporários: {allStatsWithTemporaryModifiers.Count} | " +
                   $"Nomes: [{string.Join(", ", allStatsWithTemporaryModifiers.Select(s => s.Name))}]";
        }
        
        public static void ClearAllCaches()
        {
            allStatsWithTemporaryModifiers.Clear();
        }
    }
}