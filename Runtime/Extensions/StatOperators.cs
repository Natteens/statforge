using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Extension methods to provide ultra-natural syntax for stats.
    /// Enables usage like: health += 25f; mana -= 10f; etc.
    /// </summary>
    public static class StatOperators
    {
        /// <summary>
        /// Adds a value to the stat's base value (equivalent to stat.BaseValue += value).
        /// Usage: health += 25f;
        /// </summary>
        public static Stat AddToBase(this Stat stat, float value)
        {
            if (stat != null)
                stat.BaseValue += value;
            return stat;
        }
        
        /// <summary>
        /// Subtracts a value from the stat's base value.
        /// Usage: health -= 10f;
        /// </summary>
        public static Stat SubtractFromBase(this Stat stat, float value)
        {
            if (stat != null)
                stat.BaseValue -= value;
            return stat;
        }
        
        /// <summary>
        /// Multiplies the stat's base value by a factor.
        /// Usage: damage *= 1.5f;
        /// </summary>
        public static Stat MultiplyBase(this Stat stat, float factor)
        {
            if (stat != null)
                stat.BaseValue *= factor;
            return stat;
        }
        
        /// <summary>
        /// Divides the stat's base value by a factor.
        /// Usage: speed /= 2f;
        /// </summary>
        public static Stat DivideBase(this Stat stat, float factor)
        {
            if (stat != null && !Mathf.Approximately(factor, 0f))
                stat.BaseValue /= factor;
            return stat;
        }
        
        /// <summary>
        /// Adds a temporary modifier (equivalent to AddTemporaryBonus).
        /// Usage: health.AddTemp(25f, 5f);
        /// </summary>
        public static IStatModifier AddTemp(this Stat stat, float value, float duration = -1f)
        {
            return stat?.AddTemporaryBonus(value, duration);
        }
        
        /// <summary>
        /// Adds a temporary multiplier.
        /// Usage: damage.MultiplyTemp(2f, 10f);
        /// </summary>
        public static IStatModifier MultiplyTemp(this Stat stat, float multiplier, float duration = -1f)
        {
            return stat?.AddTemporaryMultiplier(multiplier, duration);
        }
        
        /// <summary>
        /// Sets the value temporarily.
        /// Usage: health.SetTemp(1f, 3f); // Invincibility
        /// </summary>
        public static IStatModifier SetTemp(this Stat stat, float value, float duration = -1f)
        {
            return stat?.SetTemporaryValue(value, duration);
        }
        
        /// <summary>
        /// Fills the stat to its maximum value.
        /// Usage: health.Fill();
        /// </summary>
        public static void Fill(this Stat stat)
        {
            if (stat != null)
                stat.Value = stat.MaxValue;
        }
        
        /// <summary>
        /// Empties the stat to its minimum value.
        /// Usage: mana.Empty();
        /// </summary>
        public static void Empty(this Stat stat)
        {
            if (stat != null)
                stat.Value = stat.MinValue;
        }
        
        /// <summary>
        /// Resets the stat to its base value (removes all modifiers).
        /// Usage: health.Reset();
        /// </summary>
        public static void Reset(this Stat stat)
        {
            if (stat != null)
            {
                stat.ClearModifiers();
                stat.Value = stat.BaseValue;
            }
        }
        
        /// <summary>
        /// Clamps the stat's base value between its min and max.
        /// Usage: health.Clamp();
        /// </summary>
        public static void Clamp(this Stat stat)
        {
            if (stat != null)
                stat.BaseValue = Mathf.Clamp(stat.BaseValue, stat.MinValue, stat.MaxValue);
        }
        
        /// <summary>
        /// Normalizes the stat value to 0-1 range based on min/max.
        /// Usage: float normalized = health.Normalize();
        /// </summary>
        public static float Normalize(this Stat stat)
        {
            if (stat == null) return 0f;
            
            var range = stat.MaxValue - stat.MinValue;
            if (range <= 0f) return 0f;
            
            return Mathf.Clamp01((stat.Value - stat.MinValue) / range);
        }
        
        /// <summary>
        /// Lerps between two values and sets the base value.
        /// Usage: health.Lerp(0f, 100f, 0.5f); // Sets to 50
        /// </summary>
        public static void Lerp(this Stat stat, float from, float to, float t)
        {
            if (stat != null)
                stat.BaseValue = Mathf.Lerp(from, to, t);
        }
        
        /// <summary>
        /// Moves the stat towards a target value at a given speed.
        /// Usage: health.MoveTowards(100f, 10f * Time.deltaTime);
        /// </summary>
        public static void MoveTowards(this Stat stat, float target, float maxDelta)
        {
            if (stat != null)
                stat.BaseValue = Mathf.MoveTowards(stat.BaseValue, target, maxDelta);
        }
        
        /// <summary>
        /// Applies damage to the stat (subtracts from base value).
        /// Usage: health.TakeDamage(25f);
        /// </summary>
        public static void TakeDamage(this Stat stat, float damage)
        {
            if (stat != null)
                stat.BaseValue = Mathf.Max(stat.MinValue, stat.BaseValue - damage);
        }
        
        /// <summary>
        /// Heals the stat (adds to base value, clamped to max).
        /// Usage: health.Heal(50f);
        /// </summary>
        public static void Heal(this Stat stat, float amount)
        {
            if (stat != null)
                stat.BaseValue = Mathf.Min(stat.MaxValue, stat.BaseValue + amount);
        }
        
        /// <summary>
        /// Checks if the stat can afford a cost.
        /// Usage: if (mana.CanAfford(20f)) { ... }
        /// </summary>
        public static bool CanAfford(this Stat stat, float cost)
        {
            return stat != null && stat.Value >= cost;
        }
        
        /// <summary>
        /// Consumes a cost from the stat if possible.
        /// Usage: if (mana.Consume(20f)) { CastSpell(); }
        /// </summary>
        public static bool Consume(this Stat stat, float cost)
        {
            if (stat != null && stat.CanAfford(cost))
            {
                stat.BaseValue -= cost;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets a formatted string representation of the stat.
        /// Usage: string text = health.Format("F0"); // "100"
        /// </summary>
        public static string Format(this Stat stat, string format = "F1")
        {
            return stat?.Value.ToString(format) ?? "0";
        }
        
        /// <summary>
        /// Gets a formatted string with name and value.
        /// Usage: string text = health.FormatFull(); // "Health: 100.0"
        /// </summary>
        public static string FormatFull(this Stat stat, string format = "F1")
        {
            if (stat == null) return "Unknown: 0";
            return $"{stat.Name}: {stat.Value.ToString(format)}";
        }
    }
}