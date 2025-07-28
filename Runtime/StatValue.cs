using System;

namespace StatForge
{
    [Serializable]
    public class StatValue
    {
        public StatType statType;
        public float baseValue;
        public float allocatedPoints;
        public float bonusValue;
        
        public float TotalValue => baseValue + allocatedPoints + bonusValue;
        
        public event Action<StatValue> OnValueChanged;
        
        public StatValue(StatType type, float baseVal = 0f)
        {
            statType = type;
            baseValue = baseVal;
            allocatedPoints = 0f;
            bonusValue = 0f;
        }
        
        public void SetAllocatedPoints(float points)
        {
            allocatedPoints = points;
            OnValueChanged?.Invoke(this);
        }
        
        public void SetBonusValue(float bonus)
        {
            bonusValue = bonus;
            OnValueChanged?.Invoke(this);
        }
        
        public void SetBaseValue(float baseVal)
        {
            baseValue = baseVal;
            OnValueChanged?.Invoke(this);
        }
    }
}