using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public class StatContainer
    {
        [SerializeField] private List<Stat> stats = new();
        [SerializeField] private string containerName;
        
        private Dictionary<string, Stat> statsByName = new();
        private Dictionary<string, Stat> statsByShort = new();
        private Dictionary<Stat, List<Stat>> dependencies = new();
        private bool isInitialized;
        private HashSet<string> initializingStats = new();
        
        public string Name => containerName;
        public IReadOnlyList<Stat> Stats => stats;
        public int Count => stats.Count;
        public bool IsEmpty => stats.Count == 0;
        
        public event Action<Stat, float, float> OnStatChanged;
        public event Action<Stat> OnStatAdded;
        public event Action<Stat> OnStatRemoved;
        
        public StatContainer(string name = "Default")
        {
            containerName = name;
        }
        
        public void Initialize()
        {
            if (isInitialized) return;
            
            statsByName.Clear();
            statsByShort.Clear();
            dependencies.Clear();
            initializingStats.Clear();
            
            foreach (var stat in stats)
            {
                if (stat?.StatType == null) continue;
                
                stat.SetContainer(this);
                stat.OnValueChanged += HandleStatChanged;
                
                RegisterStat(stat);
            }
            
            BuildDependencies();
            isInitialized = true;
            
            RecalculateAllStats();
        }
        
        private void RegisterStat(Stat stat)
        {
            var name = stat.StatType.DisplayName;
            var shortName = stat.StatType.ShortName;
            
            if (!string.IsNullOrEmpty(name))
                statsByName[name] = stat;
                
            if (!string.IsNullOrEmpty(shortName))
                statsByShort[shortName] = stat;
        }
        
        private void BuildDependencies()
        {
            dependencies.Clear();
            
            foreach (var stat in stats)
            {
                if (stat.StatType?.HasFormula == true)
                {
                    var deps = FindStatDependencies(stat.StatType.Formula);
                    dependencies[stat] = deps;
                    
                    foreach (var dep in deps)
                    {
                        Stat.RegisterDependency(dep.Id, stat);
                    }
                }
            }
        }
        
        private List<Stat> FindStatDependencies(string formula)
        {
            var deps = new List<Stat>();
            if (string.IsNullOrEmpty(formula)) return deps;
            
            try
            {
                var pattern = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
                var matches = System.Text.RegularExpressions.Regex.Matches(formula, pattern);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var statName = match.Groups[1].Value;
                    var foundStat = GetStatInternal(statName);
                    if (foundStat != null && !deps.Contains(foundStat))
                    {
                        deps.Add(foundStat);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatForge] Error finding dependencies for formula '{formula}': {e.Message}");
            }
            
            return deps;
        }
        
        private Stat GetStatInternal(string nameOrShort)
        {
            if (statsByName.TryGetValue(nameOrShort, out var stat1))
                return stat1;
                
            if (statsByShort.TryGetValue(nameOrShort, out var stat2))
                return stat2;
                
            return null;
        }
        
        public Stat GetStat(string nameOrShort)
        {
            if (!isInitialized) 
            {
                if (initializingStats.Contains(nameOrShort))
                {
                    Debug.LogWarning($"[StatForge] Circular dependency detected for stat '{nameOrShort}'");
                    return null;
                }
                
                initializingStats.Add(nameOrShort);
                Initialize();
                initializingStats.Remove(nameOrShort);
            }
            
            return GetStatInternal(nameOrShort);
        }
        
        public float GetStatValue(string nameOrShort)
        {
            var stat = GetStatInternal(nameOrShort);
            if (stat != null)
            {
                return stat.Value; 
            }
            return 0f;
        }
        
        public void AddStat(Stat stat)
        {
            if (stat == null || stats.Contains(stat)) return;
            
            stats.Add(stat);
            
            if (isInitialized)
            {
                stat.SetContainer(this);
                stat.OnValueChanged += HandleStatChanged;
                RegisterStat(stat);
                BuildDependencies();
                RecalculateAllStats();
            }
            
            OnStatAdded?.Invoke(stat);
        }
        
        public Stat CreateStat(StatType statType, float baseValue = 0f)
        {
            var stat = new Stat(statType, baseValue);
            AddStat(stat);
            return stat;
        }
        
        public bool RemoveStat(Stat stat)
        {
            if (stat == null || !stats.Remove(stat)) return false;
            
            stat.OnValueChanged -= HandleStatChanged;
            
            if (stat.StatType != null)
            {
                statsByName.Remove(stat.StatType.DisplayName);
                statsByShort.Remove(stat.StatType.ShortName);
            }
            
            dependencies.Remove(stat);
            BuildDependencies();
            OnStatRemoved?.Invoke(stat);
            
            return true;
        }
        
        public bool RemoveStat(string nameOrShort)
        {
            var stat = GetStatInternal(nameOrShort);
            return stat != null && RemoveStat(stat);
        }
        
        public void ClearStats()
        {
            var statsToRemove = stats.ToList();
            foreach (var stat in statsToRemove)
            {
                RemoveStat(stat);
            }
        }
        
        public void NotifyStatChanged(Stat changedStat)
        {
            var dependentStats = new List<Stat>();
            
            foreach (var kvp in dependencies)
            {
                if (kvp.Value.Contains(changedStat))
                {
                    dependentStats.Add(kvp.Key);
                }
            }
            
            foreach (var dependent in dependentStats)
            {
                dependent.ForceRecalculate();
            }
        }
        
        private void RecalculateAllStats()
        {
            foreach (var stat in stats.Where(s => s.StatType?.HasFormula != true))
            {
                stat.ForceRecalculate();
            }
            
            foreach (var stat in stats.Where(s => s.StatType?.HasFormula == true))
            {
                stat.ForceRecalculate();
            }
        }
        
        private void HandleStatChanged(Stat stat, float oldValue, float newValue)
        {
            OnStatChanged?.Invoke(stat, oldValue, newValue);
            NotifyStatChanged(stat);
        }
        
        public IEnumerable<Stat> GetStatsByCategory(string category)
        {
            return stats.Where(s => s.StatType?.Category == category);
        }
        
        public IEnumerable<Stat> GetStatsWithFormula()
        {
            return stats.Where(s => s.StatType?.HasFormula == true);
        }
        
        public override string ToString()
        {
            return $"{containerName} ({stats.Count} stats)";
        }
    }
}