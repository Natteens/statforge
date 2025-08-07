using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Additional extension methods for enhanced Stat functionality.
    /// This class provides additional convenience methods beyond the core Stat class.
    /// </summary>
    public static class StatOperators
    {
        /// <summary>
        /// Applies a percentage modifier to a stat.
        /// Example: health.Percent(50) adds 50% of current value as bonus.
        /// </summary>
        public static IStatModifier Percent(this Stat stat, float percentage, float duration = -1f)
        {
            if (stat == null) return null;
            var bonusValue = stat.Value * (percentage / 100f);
            return stat.AddTemporaryBonus(bonusValue, duration);
        }
        
        /// <summary>
        /// Applies a percentage multiplier to a stat.
        /// Example: damage.Multiply(150) multiplies damage by 1.5x.
        /// </summary>
        public static IStatModifier Multiply(this Stat stat, float percentage, float duration = -1f)
        {
            if (stat == null) return null;
            var multiplier = percentage / 100f;
            return stat.AddTemporaryMultiplier(multiplier, duration);
        }
        
        /// <summary>
        /// Clamps the stat value between min and max using temporary modifiers.
        /// </summary>
        public static void ClampValue(this Stat stat, float min, float max)
        {
            if (stat == null) return;
            
            if (stat.Value < min)
            {
                stat.AddTemporaryBonus(min - stat.Value);
            }
            else if (stat.Value > max)
            {
                stat.AddTemporaryBonus(max - stat.Value);
            }
        }
        
        /// <summary>
        /// Returns true if the stat value is within the specified range.
        /// </summary>
        public static bool InRange(this Stat stat, float min, float max)
        {
            if (stat == null) return false;
            return stat.Value >= min && stat.Value <= max;
        }
        
        /// <summary>
        /// Returns the stat value as a percentage of the maximum.
        /// </summary>
        public static float AsPercentage(this Stat stat)
        {
            if (stat == null || stat.MaxValue == 0f) return 0f;
            return (stat.Value / stat.MaxValue) * 100f;
        }
        
        /// <summary>
        /// Fills the stat to its maximum value.
        /// </summary>
        public static void FillToMax(this Stat stat)
        {
            if (stat != null)
            {
                stat.Value = stat.MaxValue;
            }
        }
        
        /// <summary>
        /// Empties the stat to its minimum value.
        /// </summary>
        public static void EmptyToMin(this Stat stat)
        {
            if (stat != null)
            {
                stat.Value = stat.MinValue;
            }
        }
        
        /// <summary>
        /// Returns true if the stat is at its maximum value.
        /// </summary>
        public static bool IsFull(this Stat stat)
        {
            return stat != null && Mathf.Approximately(stat.Value, stat.MaxValue);
        }
        
        /// <summary>
        /// Returns true if the stat is at its minimum value.
        /// </summary>
        public static bool IsEmpty(this Stat stat)
        {
            return stat != null && Mathf.Approximately(stat.Value, stat.MinValue);
        }
        
        /// <summary>
        /// Combines two stat values (addition).
        /// </summary>
        public static float CombineAdd(this Stat statA, Stat statB)
        {
            return (statA?.Value ?? 0f) + (statB?.Value ?? 0f);
        }
        
        /// <summary>
        /// Combines two stat values (multiplication).
        /// </summary>
        public static float CombineMultiply(this Stat statA, Stat statB)
        {
            return (statA?.Value ?? 0f) * (statB?.Value ?? 0f);
        }
        
        /// <summary>
        /// Gets the ratio between two stats.
        /// </summary>
        public static float GetRatio(this Stat statA, Stat statB)
        {
            var bValue = statB?.Value ?? 0f;
            if (Mathf.Approximately(bValue, 0f))
            {
                return 0f;
            }
            return (statA?.Value ?? 0f) / bValue;
        }
    }
}
}