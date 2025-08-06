using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    [CreateAssetMenu(fileName = "New Stat Container", menuName = "Scriptable Objects/StatForge/Stat Container")]
    public class StatContainer : ScriptableObject
    {
        [Header("Container Information")]
        [SerializeField] private string containerName = "";
        [SerializeField] private string description = "";
        
        [Header("Stats Configuration")]
        [SerializeField] private List<StatValue> stats;
        
        [Header("Auto-populate Settings")]
        [SerializeField] private bool autoPopulatePrimary = true;
        [SerializeField] private bool autoPopulateDerived = true;
        [SerializeField] private bool autoPopulateExternal;
        
        [Header("Container Category")]
        [SerializeField] private ContainerCategory category = ContainerCategory.Base;

       
        private Dictionary<StatType, StatValue> statLookup;
        private bool isInitialized;
        
        public string ContainerName 
        { 
            get => containerName; 
            set => containerName = value; 
        }
        
        public string Description 
        { 
            get => description; 
            set => description = value; 
        }
        
        public ContainerCategory Category 
        { 
            get => category; 
            set => category = value; 
        }
        
        public List<StatValue> Stats
        {
            get => stats;
            set => stats = value;
        }

        public bool IsInitialized => isInitialized;
        
        public void Initialize()
        {
            if (isInitialized) return;
            
            AutoPopulateStats();
            RefreshLookup();
            SubscribeToValueChanges();
            RecalculateAllDerived();
            
            isInitialized = true;
        }
        
        private void AutoPopulateStats()
        {
            var allStatTypes = GetAllStatTypes();
            
            foreach (var statType in allStatTypes)
            {
                bool shouldInclude = false;
                
                switch (statType.Category)
                {
                    case StatCategory.Primary:
                        shouldInclude = autoPopulatePrimary;
                        break;
                    case StatCategory.Derived:
                        shouldInclude = autoPopulateDerived;
                        break;
                    case StatCategory.External:
                        shouldInclude = autoPopulateExternal;
                        break;
                }
                
                if (shouldInclude && !HasStat(statType))
                {
                    AddStat(statType, statType.DefaultValue);
                }
            }
        }
        
        private StatType[] GetAllStatTypes()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:StatType");
            return guids.Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<StatType>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                       .Where(stat => stat != null)
                       .ToArray();
#else
            // static list ou Resources.LoadAll
            return Resources.LoadAll<StatType>("");
#endif
        }
        
        public void ForceRecalculate()
        {
            isInitialized = false;
            Initialize();
        }
        
        private void RefreshLookup()
        {
            statLookup = stats.Where(s => s.statType != null)
                .ToDictionary(s => s.statType, s => s);
        }
        
        private void SubscribeToValueChanges()
        {
            foreach (var stat in stats)
            {
                stat.OnValueChanged -= OnStatValueChanged;
                stat.OnValueChanged += OnStatValueChanged;
            }
        }
        
        public float GetStatValue(StatType statType)
        {
            if (statType == null) return 0f;
            
            if (statLookup == null) RefreshLookup();
            
            if (statLookup!.TryGetValue(statType, out var stat))
            {
                if (statType.IsDerived)
                {
                    return CalculateDerivedValue(statType, stat);
                }
                return stat.TotalValue;
            }
            
            return statType.DefaultValue;
        }
        
        public void SetAllocatedPoints(StatType statType, float points)
        {
            if (statType == null || !statType.IsPrimary) return;
            
            var stat = GetOrCreateStat(statType);
            if (stat != null)
            {
                stat.SetAllocatedPoints(Mathf.Max(0f, points));
            }
        }
        
        public void SetBonusValue(StatType statType, float bonus)
        {
            var stat = GetOrCreateStat(statType);
            if (stat != null)
            {
                stat.SetBonusValue(bonus);
            }
        }
        
        public void SetBaseValue(StatType statType, float baseValue)
        {
            var stat = GetOrCreateStat(statType);
            if (stat != null)
            {
                stat.SetBaseValue(baseValue);
            }
        }
        
        public StatValue GetStat(StatType statType)
        {
            if (statType == null) return null;
            
            if (statLookup == null) RefreshLookup();
            statLookup.TryGetValue(statType, out var stat);
            return stat;
        }
        
        public StatValue GetOrCreateStat(StatType statType)
        {
            if (statType == null) return null;
            
            var stat = GetStat(statType);
            if (stat == null)
            {
                AddStat(statType, statType.DefaultValue);
                stat = GetStat(statType);
            }
            
            return stat;
        }
        
        public void AddStat(StatType statType, float baseValue = 0f)
        {
            if (statType == null || stats.Any(s => s.statType == statType)) return;
            
            var newStat = new StatValue(statType, baseValue);
            stats.Add(newStat);
            
            if (isInitialized)
            {
                RefreshLookup();
                newStat.OnValueChanged += OnStatValueChanged;
            }
        }
        
        public bool RemoveStat(StatType statType)
        {
            if (statType == null) return false;
            
            var stat = stats.FirstOrDefault(s => s.statType == statType);
            if (stat != null)
            {
                stat.OnValueChanged -= OnStatValueChanged;
                stats.Remove(stat);
                
                if (isInitialized)
                {
                    RefreshLookup();
                }
                return true;
            }
            
            return false;
        }
        
        private void OnStatValueChanged(StatValue changedStat)
        {
            if (!isInitialized) return;
            
            RecalculateDerivedStatsThatDependOn(changedStat.statType);
        }
        
        private void RecalculateDerivedStatsThatDependOn(StatType changedStat)
        {
            var derivedStats = stats.Where(s => s.statType != null && s.statType.IsDerived);
            
            foreach (var stat in derivedStats)
            {
                if (StatDependsOn(stat.statType, changedStat))
                {
                    CalculateDerivedValue(stat.statType, stat);
                }
            }
        }
        
        private bool StatDependsOn(StatType derivedStat, StatType dependency)
        {
            if (derivedStat.Dependencies != null && derivedStat.Dependencies.Contains(dependency))
                return true;
            
            var referencedStats = FormulaEvaluator.ExtractStatReferences(derivedStat.Formula);
            return referencedStats.Contains(dependency.ShortName);
        }
        
        private void RecalculateAllDerived()
        {
            var derivedStats = stats.Where(s => s.statType != null && s.statType.IsDerived);
            
            foreach (var stat in derivedStats)
            {
                CalculateDerivedValue(stat.statType, stat);
            }
        }
        
        private float CalculateDerivedValue(StatType derivedStat, StatValue statValue)
        {
            if (!derivedStat.IsDerived || string.IsNullOrEmpty(derivedStat.Formula))
                return statValue.TotalValue; 
            
            float formulaResult = FormulaEvaluator.Evaluate(derivedStat.Formula, this);
            return formulaResult + statValue.baseValue + statValue.bonusValue;
        }
        
        public static StatContainer Merge(params StatContainer[] containers)
        {
            if (containers == null || containers.Length == 0) return null;
            
            var merged = CreateInstance<StatContainer>();
            merged.containerName = string.Join(" + ", containers.Select(c => c.ContainerName));
            merged.stats = new List<StatValue>();
            
            var firstContainer = containers.FirstOrDefault(c => c != null);
            if (firstContainer != null)
            {
                merged.autoPopulatePrimary = firstContainer.autoPopulatePrimary;
                merged.autoPopulateDerived = firstContainer.autoPopulateDerived;
                merged.autoPopulateExternal = firstContainer.autoPopulateExternal;
            }
            
            var allStatTypes = new HashSet<StatType>();
            
            foreach (var container in containers.Where(c => c != null))
            {
                foreach (var stat in container.stats)
                {
                    if (stat.statType != null)
                        allStatTypes.Add(stat.statType);
                }
            }
            
            foreach (var statType in allStatTypes)
            {
                float totalBase = 0f;
                float totalBonus = 0f;
                
                foreach (var container in containers.Where(c => c != null))
                {
                    var stat = container.GetStat(statType);
                    if (stat != null)
                    {
                        totalBase += stat.baseValue;
                        totalBonus += stat.bonusValue;
                    }
                }
                
                var mergedStat = new StatValue(statType, totalBase);
                mergedStat.SetBonusValue(totalBonus);
                merged.stats.Add(mergedStat);
            }
            
            merged.Initialize();
            return merged;
        }

        private List<StatValue> GetStatsByCategory(StatCategory c)
        {
            return stats.Where(s => s.statType != null && s.statType.Category == c).ToList();
        }
        
        public List<StatValue> GetPrimaryStats()
        {
            return GetStatsByCategory(StatCategory.Primary);
        }
        
        public List<StatValue> GetDerivedStats()
        {
            return GetStatsByCategory(StatCategory.Derived);
        }
        
        public List<StatValue> GetExternalStats()
        {
            return GetStatsByCategory(StatCategory.External);
        }
        
        public bool HasStat(StatType statType)
        {
            return statType != null && stats.Any(s => s.statType == statType);
        }
        
        public void ClearStats()
        {
            foreach (var stat in stats)
            {
                stat.OnValueChanged -= OnStatValueChanged;
            }
            
            stats.Clear();
            statLookup?.Clear();
        }
        
        public void SetAutoPopulateSettings(bool primary, bool derived, bool external)
        {
            autoPopulatePrimary = primary;
            autoPopulateDerived = derived;
            autoPopulateExternal = external;
            
            if (isInitialized)
            {
                AutoPopulateStats();
                RefreshLookup();
            }
        }
        
        private void OnDestroy()
        {
            foreach (var stat in stats)
            {
                stat.OnValueChanged -= OnStatValueChanged;
            }
        }
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(containerName))
                containerName = name;
                
            RemoveNullStats();
        }
        
        private void RemoveNullStats()
        {
            stats.RemoveAll(s => s == null || s.statType == null);
        }
    }
}