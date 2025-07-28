using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    public class AttributeSystem : MonoBehaviour
    {
        [Header("Stat Containers")]
        public List<StatContainer> baseContainers;
        
        [Header("Runtime Info (Read Only)")]
        [SerializeField] private int availablePoints;
        
        private StatContainer runtimeContainer;
        private Dictionary<StatType, float> temporaryBonuses;
        
        public StatContainer RuntimeContainer => runtimeContainer;
        public int AvailablePoints => availablePoints;
        
        private void Awake()
        {
            InitializeStats();
        }
        
        private void InitializeStats()
        {
            if (baseContainers.Count == 0) return;
            
            runtimeContainer = StatContainer.Merge(baseContainers.ToArray());
            runtimeContainer.Initialize();
        }
        
        public void SetAvailablePoints(int points)
        {
            availablePoints = Mathf.Max(0, points);
        }
        
        public void AddAvailablePoints(int points)
        {
            availablePoints += points;
        }
        
        public float GetStatValue(StatType statType)
        {
            if (runtimeContainer == null) return 0f;
            
            float baseValue = runtimeContainer.GetStatValue(statType);
            
            if (temporaryBonuses.TryGetValue(statType, out float bonus))
                baseValue += bonus;
            
            return Mathf.Max(baseValue, statType.MinValue);
        }
        
        public bool CanAllocatePoint(StatType statType)
        {
            if (!Application.isPlaying) return false;
            return availablePoints > 0 && statType.Category == StatCategory.Primary;
        }
        
        public bool AllocatePoint(StatType statType)
        {
            if (!CanAllocatePoint(statType)) return false;
            
            var stat = runtimeContainer.GetStat(statType);
            if (stat != null)
            {
                stat.SetAllocatedPoints(stat.allocatedPoints + 1f);
                availablePoints--;
                return true;
            }
            
            return false;
        }
        
        public bool CanDeallocatePoint(StatType statType)
        {
            if (!Application.isPlaying) return false;
            if (statType.Category != StatCategory.Primary) return false;
            
            var stat = runtimeContainer.GetStat(statType);
            return stat != null && stat.allocatedPoints > 0f;
        }
        
        public bool DeallocatePoint(StatType statType)
        {
            if (!CanDeallocatePoint(statType)) return false;
            
            var stat = runtimeContainer.GetStat(statType);
            if (stat != null && stat.allocatedPoints > 0f)
            {
                stat.SetAllocatedPoints(stat.allocatedPoints - 1f);
                availablePoints++;
                return true;
            }
            
            return false;
        }
        
        public void AddTemporaryBonus(StatType statType, float bonus)
        {
            if (temporaryBonuses.ContainsKey(statType))
                temporaryBonuses[statType] += bonus;
            else
                temporaryBonuses[statType] = bonus;
        }
        
        public void RemoveTemporaryBonus(StatType statType, float bonus)
        {
            if (temporaryBonuses.ContainsKey(statType))
            {
                temporaryBonuses[statType] -= bonus;
                if (Mathf.Approximately(temporaryBonuses[statType], 0f))
                    temporaryBonuses.Remove(statType);
            }
        }
        
        public void ClearTemporaryBonuses()
        {
            temporaryBonuses.Clear();
        }
        
        public void SetTemporaryBonus(StatType statType, float bonus)
        {
            if (Mathf.Approximately(bonus, 0f))
                temporaryBonuses.Remove(statType);
            else
                temporaryBonuses[statType] = bonus;
        }
        
        public float GetTemporaryBonus(StatType statType)
        {
            return temporaryBonuses.TryGetValue(statType, out float bonus) ? bonus : 0f;
        }
        
        public List<StatValue> GetPrimaryStats()
        {
            return runtimeContainer?.Stats.Where(s => s.statType.Category == StatCategory.Primary).ToList() 
                   ?? new List<StatValue>();
        }
        
        public List<StatValue> GetDerivedStats()
        {
            return runtimeContainer?.Stats.Where(s => s.statType.Category == StatCategory.Derived).ToList() 
                   ?? new List<StatValue>();
        }
        
        public List<StatValue> GetExternalStats()
        {
            return runtimeContainer?.Stats.Where(s => s.statType.Category == StatCategory.External).ToList() 
                   ?? new List<StatValue>();
        }
        
        public List<StatValue> GetAllStats()
        {
            return runtimeContainer?.Stats ?? new List<StatValue>();
        }
        
        public void ResetAllocatedPoints()
        {
            if (!Application.isPlaying) return;
            
            int totalAllocated = 0;
            
            foreach (var stat in GetPrimaryStats())
            {
                totalAllocated += Mathf.RoundToInt(stat.allocatedPoints);
                stat.SetAllocatedPoints(0f);
            }
            
            availablePoints += totalAllocated;
        }
    }
}