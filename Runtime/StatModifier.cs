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
        [SerializeField] private double startTime; // ⚡ Tempo absoluto para precisão
        
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
        
        // ⚡ Nova propriedade para tempo absoluto
        public double AbsoluteEndTime { get; private set; }
        
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
            
            // ⚡ Calcula tempo absoluto de expiração
            if (duration == ModifierDuration.Temporary)
            {
                this.startTime = StatModifierManager.GetCurrentTime();
                this.AbsoluteEndTime = this.startTime + time;
            }
        }
        
        public bool Update(float deltaTime)
        {
            if (duration == ModifierDuration.Temporary)
            {
                // ⚡ Usa tempo absoluto para precisão perfeita
                var currentTime = StatModifierManager.GetCurrentTime();
                remainingTime = (float)(AbsoluteEndTime - currentTime);
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
    
    // 🚀 SISTEMA GLOBAL ULTRA-OTIMIZADO
    public static class StatModifierManager
    {
        // ⚡ Pool de objetos para evitar GC
        private static readonly Queue<StatModifierData> modifierPool = new Queue<StatModifierData>(1024);
        
        // ⚡ Estruturas otimizadas
        private static readonly SortedDictionary<double, List<StatModifierData>> modifiersByExpiration = new();
        private static readonly Dictionary<Stat, List<StatModifierData>> modifiersByStat = new();
        private static readonly HashSet<Stat> dirtyStats = new HashSet<Stat>();
        
        private static bool globalUpdateInitialized = false;
        private static StatUpdateManager updateManager;
        private static double lastCleanupTime = 0;
        
        // ⚡ Timing preciso
        public static double GetCurrentTime()
        {
            #if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
            #else
            return Time.realtimeSinceStartupAsDouble;
            #endif
        }
        
        static StatModifierManager()
        {
            InitializeGlobalUpdate();
        }
        
        private static void InitializeGlobalUpdate()
        {
            if (globalUpdateInitialized) return;
            
            globalUpdateInitialized = true;
            
            // 🚀 Cria manager universal (Editor + Build)
            CreateUpdateManager();
            
            Debug.Log("[StatForge] 🚀 Sistema Ultra-Otimizado inicializado!");
        }
        
        private static void CreateUpdateManager()
        {
            #if UNITY_EDITOR
            // No Editor: usa EditorApplication.update
            UnityEditor.EditorApplication.update += GlobalUpdateAllStats;
            #endif
            
            // ⚡ Sempre cria MonoBehaviour para compatibilidade total
            var go = new GameObject("[StatForge] Ultra Manager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            UnityEngine.Object.DontDestroyOnLoad(go);
            updateManager = go.AddComponent<StatUpdateManager>();
            updateManager.enabled = !Application.isEditor; // Só ativo no build
        }
        
        // 🚀 UPDATE GLOBAL ULTRA-OTIMIZADO
        public static void GlobalUpdateAllStats()
        {
            var currentTime = GetCurrentTime();
            
            // ⚡ Cleanup automático a cada 5 segundos
            if (currentTime - lastCleanupTime > 5.0)
            {
                CleanupExpiredEntries();
                lastCleanupTime = currentTime;
            }
            
            // ⚡ Processa apenas modificadores que expiraram
            ProcessExpiredModifiers(currentTime);
            
            // ⚡ Força recálculo apenas de stats que mudaram
            ProcessDirtyStats();
        }
        
        private static void ProcessExpiredModifiers(double currentTime)
        {
            if (modifiersByExpiration.Count == 0) return;
            
            var expiredTimes = new List<double>();
            
            // ⚡ Coleta todos os tempos expirados
            foreach (var kvp in modifiersByExpiration)
            {
                if (kvp.Key <= currentTime)
                    expiredTimes.Add(kvp.Key);
                else
                    break; // SortedDictionary = já está ordenado
            }
            
            // ⚡ Processa modificadores expirados
            foreach (var expiredTime in expiredTimes)
            {
                if (modifiersByExpiration.TryGetValue(expiredTime, out var expiredModifiers))
                {
                    foreach (var modData in expiredModifiers)
                    {
                        RemoveModifierFromStat(modData);
                    }
                    
                    modifiersByExpiration.Remove(expiredTime);
                    
                    // ⚡ Retorna objetos para o pool
                    foreach (var modData in expiredModifiers)
                    {
                        ReturnToPool(modData);
                    }
                }
            }
        }
        
        private static void ProcessDirtyStats()
        {
            if (dirtyStats.Count == 0) return;
    
            var statsToProcess = new List<Stat>(dirtyStats);
            dirtyStats.Clear();
    
            foreach (var stat in statsToProcess)
            {
                stat?.ForceUpdateModifiers(0f);
            }
        }
        
        private static void CleanupExpiredEntries()
        {
            var currentTime = GetCurrentTime();
            var toRemove = new List<double>();
            
            foreach (var kvp in modifiersByExpiration)
            {
                if (kvp.Key < currentTime - 10.0) // Remove entradas muito antigas
                    toRemove.Add(kvp.Key);
            }
            
            foreach (var time in toRemove)
            {
                modifiersByExpiration.Remove(time);
            }
        }
        
        public static void RegisterTemporaryModifier(Stat stat, StatModifier modifier)
        {
            if (modifier.Duration != ModifierDuration.Temporary) return;
            
            var modData = GetFromPool();
            modData.Initialize(stat, modifier);
            
            var endTime = modifier.AbsoluteEndTime;
            if (!modifiersByExpiration.TryGetValue(endTime, out var modList))
            {
                modList = new List<StatModifierData>();
                modifiersByExpiration[endTime] = modList;
            }
            modList.Add(modData);
            
            if (!modifiersByStat.TryGetValue(stat, out var statMods))
            {
                statMods = new List<StatModifierData>();
                modifiersByStat[stat] = statMods;
            }
            statMods.Add(modData);
            
            Debug.Log($"[StatForge] ⚡ Modificador {modifier.Id} registrado para expirar em {modifier.RemainingTime:F2}s");
        }
        
        public static void UnregisterStat(Stat stat)
        {
            if (modifiersByStat.TryGetValue(stat, out var modifiers))
            {
                foreach (var modData in modifiers)
                {
                    RemoveModifierFromExpiration(modData);
                    ReturnToPool(modData);
                }
                
                modifiersByStat.Remove(stat);
            }
            
            dirtyStats.Remove(stat);
        }
        
        private static void RemoveModifierFromStat(StatModifierData modData)
        {
            var stat = modData.Stat;
            var modifier = modData.Modifier;
            
            Debug.Log($"[StatForge] ⚡ Modificador {modifier.Id} expirou em {stat.Name} (INSTANTÂNEO)");
            
            if (modifiersByStat.TryGetValue(stat, out var statMods))
            {
                statMods.Remove(modData);
                if (statMods.Count == 0)
                    modifiersByStat.Remove(stat);
            }
            
            dirtyStats.Add(stat);
        }
        
        private static void RemoveModifierFromExpiration(StatModifierData modData)
        {
            var endTime = modData.Modifier.AbsoluteEndTime;
            if (modifiersByExpiration.TryGetValue(endTime, out var modList))
            {
                modList.Remove(modData);
                if (modList.Count == 0)
                    modifiersByExpiration.Remove(endTime);
            }
        }
        private static StatModifierData GetFromPool()
        {
            return modifierPool.Count > 0 ? modifierPool.Dequeue() : new StatModifierData();
        }
        
        private static void ReturnToPool(StatModifierData modData)
        {
            modData.Reset();
            if (modifierPool.Count < 1024) 
                modifierPool.Enqueue(modData);
        }
       
        public static bool IsTrackedForTemporary(Stat stat)
        {
            return modifiersByStat.ContainsKey(stat);
        }
        
        public static string GetGlobalDebugInfo()
        {
            var trackedStats = modifiersByStat.Count;
            var totalModifiers = modifiersByExpiration.Values.Sum(list => list.Count);
            
            return $"[StatForge Ultra] Stats: {trackedStats} | Modificadores ativos: {totalModifiers} | " +
                   $"Pool disponível: {modifierPool.Count}";
        }
        
        public static void ClearAllCaches()
        {
            modifiersByExpiration.Clear();
            modifiersByStat.Clear();
            dirtyStats.Clear();
            modifierPool.Clear();
        }
    }
    
    internal class StatModifierData
    {
        public Stat Stat { get; private set; }
        public StatModifier Modifier { get; private set; }
        
        public void Initialize(Stat stat, StatModifier modifier)
        {
            Stat = stat;
            Modifier = modifier;
        }
        
        public void Reset()
        {
            Stat = null;
            Modifier = null;
        }
    }
    
    internal class StatUpdateManager : MonoBehaviour
    {
        private void Update()
        {
            StatModifierManager.GlobalUpdateAllStats();
        }
        
        private void OnDestroy()
        {
            StatModifierManager.ClearAllCaches();
        }
    }
}