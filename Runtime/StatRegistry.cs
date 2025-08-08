using System.Collections.Generic;
using System.Linq;

namespace StatForge
{
    public class StatRegistry
    {
        private object owner;
        private Dictionary<string, Stat> statsByName;
        private Dictionary<string, Stat> statsByShort;
        private List<Stat> allStats;
        private Dictionary<Stat, List<Stat>> dependencies;
        
        public StatRegistry(object owner)
        {
            this.owner = owner;
            BuildRegistry();
        }
        
        private void BuildRegistry()
        {
            statsByName = new Dictionary<string, Stat>();
            statsByShort = new Dictionary<string, Stat>();
            allStats = new List<Stat>();
            dependencies = new Dictionary<Stat, List<Stat>>();
            
            var fields = owner.GetType().GetFields(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Stat))
                {
                    var stat = field.GetValue(owner) as Stat;
                    if (stat?.StatType != null)
                    {
                        allStats.Add(stat);
                        
                        var name = stat.StatType.DisplayName;
                        var shortName = stat.StatType.ShortName;
                        
                        if (!string.IsNullOrEmpty(name))
                            statsByName[name] = stat;
                            
                        if (!string.IsNullOrEmpty(shortName))
                            statsByShort[shortName] = stat;
                        
                        Stat.RegisterStatRegistry(stat.Id, this);
                    }
                }
            }
            
            BuildDependencies();
        }
        
        private void BuildDependencies()
        {
            dependencies.Clear();
            
            foreach (var stat in allStats)
            {
                if (stat.StatType.HasFormula)
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
            var pattern = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
            var matches = System.Text.RegularExpressions.Regex.Matches(formula, pattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var statName = match.Groups[1].Value;
                var foundStat = GetStat(statName);
                if (foundStat != null && !deps.Contains(foundStat))
                {
                    deps.Add(foundStat);
                }
            }
            
            return deps;
        }
        
        public Stat GetStat(string nameOrShort)
        {
            if (statsByName.TryGetValue(nameOrShort, out var stat1))
                return stat1;
                
            if (statsByShort.TryGetValue(nameOrShort, out var stat2))
                return stat2;
                
            return null;
        }
        
        public float GetStatValue(string nameOrShort)
        {
            var stat = GetStat(nameOrShort);
            if (stat != null)
            {
                return stat.BaseValue + stat.Modifiers
                    .Where(m => m.Type == ModifierType.Additive)
                    .Sum(m => m.Value);
            }
            return 0f;
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
        
        public void RefreshAll()
        {
            foreach (var stat in allStats.Where(s => !s.StatType.HasFormula))
            {
                stat.ForceRecalculate();
            }
            
            foreach (var stat in allStats.Where(s => s.StatType.HasFormula))
            {
                stat.ForceRecalculate();
            }
        }
    }
}