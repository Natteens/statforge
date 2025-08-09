using System;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public class PooledStatModifier : IStatModifier
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
        
        public void Initialize(Stat targetStat, float value, ModifierType type, string source)
        {
            id = StatIdPool.GetId();
            this.targetStat = targetStat;
            this.value = value;
            this.type = type;
            this.source = source ?? "";
            duration = ModifierDuration.Permanent;
            priority = ModifierPriority.Normal;
            remainingTime = 0f;
            tag = null;
            removalCondition = null;
        }
        
        public void Reset()
        {
            if (!string.IsNullOrEmpty(id))
            {
                StatIdPool.ReturnId(id);
                id = null;
            }
            targetStat = null;
            value = 0f;
            type = ModifierType.Additive;
            duration = ModifierDuration.Permanent;
            priority = ModifierPriority.Normal;
            remainingTime = 0f;
            source = "";
            tag = null;
            removalCondition = null;
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
                    Debug.LogError($"Error in modifier condition {id}: {e.Message}");
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
            var clone = new PooledStatModifier();
            clone.Initialize(targetStat, value, type, source);
            clone.duration = duration;
            clone.priority = priority;
            clone.remainingTime = remainingTime;
            clone.tag = tag;
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
}