using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Implicit conversion extensions for Stat class to provide seamless type conversions.
    /// </summary>
    public static class StatConversions
    {
        /// <summary>
        /// Implicit conversion from float to Stat.
        /// Creates a new anonymous stat with the given value.
        /// </summary>
        public static Stat FromFloat(float value)
        {
            return new Stat("TempStat", value);
        }
        
        /// <summary>
        /// Implicit conversion from int to Stat.
        /// Creates a new anonymous stat with the given value.
        /// </summary>
        public static Stat FromInt(int value)
        {
            return new Stat("TempStat", value);
        }
        
        /// <summary>
        /// Converts a stat to int (truncated).
        /// </summary>
        public static int ToInt(this Stat stat)
        {
            return stat != null ? (int)stat.Value : 0;
        }
        
        /// <summary>
        /// Converts a stat to string with optional formatting.
        /// </summary>
        public static string ToString(this Stat stat, string format = "F2")
        {
            if (stat == null) return "0";
            
            try
            {
                return stat.Value.ToString(format);
            }
            catch
            {
                return stat.Value.ToString("F2");
            }
        }
        
        /// <summary>
        /// Formats the stat value with its definition's display format if available.
        /// </summary>
        public static string ToFormattedString(this Stat stat)
        {
            if (stat?.Definition != null)
            {
                return stat.Definition.FormatValue(stat.Value);
            }
            return stat?.ToString() ?? "0";
        }
        
        /// <summary>
        /// Converts stat to boolean (true if value > 0).
        /// </summary>
        public static bool ToBool(this Stat stat)
        {
            return stat != null && stat.Value > 0f;
        }
        
        /// <summary>
        /// Safely gets the stat value or returns a default if null.
        /// </summary>
        public static float GetValueOrDefault(this Stat stat, float defaultValue = 0f)
        {
            return stat?.Value ?? defaultValue;
        }
    }
}