using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Implicit conversion extensions to make stats work seamlessly with common Unity types.
    /// </summary>
    public static class StatConversions
    {
        /// <summary>
        /// Converts a Stat to int (rounded down).
        /// Usage: int level = playerLevel;
        /// </summary>
        public static int ToInt(this Stat stat)
        {
            return stat != null ? Mathf.FloorToInt(stat.Value) : 0;
        }
        
        /// <summary>
        /// Converts a Stat to int (rounded to nearest).
        /// Usage: int level = playerLevel.ToIntRounded();
        /// </summary>
        public static int ToIntRounded(this Stat stat)
        {
            return stat != null ? Mathf.RoundToInt(stat.Value) : 0;
        }
        
        /// <summary>
        /// Converts a Stat to bool (true if > 0).
        /// Usage: bool isAlive = health.ToBool();
        /// </summary>
        public static bool ToBool(this Stat stat)
        {
            return stat != null && stat.Value > 0f;
        }
        
        /// <summary>
        /// Converts a Stat to Vector2 (for 2D ranges).
        /// Usage: Vector2 range = healthRange.ToVector2(); // x = min, y = max
        /// </summary>
        public static Vector2 ToVector2(this Stat stat)
        {
            return stat != null ? new Vector2(stat.MinValue, stat.MaxValue) : Vector2.zero;
        }
        
        /// <summary>
        /// Converts a Stat to Vector3 (for 3D data).
        /// Usage: Vector3 data = stat.ToVector3(); // x = min, y = value, z = max
        /// </summary>
        public static Vector3 ToVector3(this Stat stat)
        {
            return stat != null ? new Vector3(stat.MinValue, stat.Value, stat.MaxValue) : Vector3.zero;
        }
        
        /// <summary>
        /// Converts a Stat to Color (for UI visualization).
        /// Usage: Color healthColor = health.ToColor(); // Green at max, red at min
        /// </summary>
        public static Color ToColor(this Stat stat)
        {
            if (stat == null) return Color.black;
            
            float percentage = stat.Percentage;
            return Color.Lerp(Color.red, Color.green, percentage);
        }
        
        /// <summary>
        /// Converts a Stat to Color with custom colors.
        /// Usage: Color healthColor = health.ToColor(Color.red, Color.green);
        /// </summary>
        public static Color ToColor(this Stat stat, Color minColor, Color maxColor)
        {
            if (stat == null) return Color.black;
            
            float percentage = stat.Percentage;
            return Color.Lerp(minColor, maxColor, percentage);
        }
        
        /// <summary>
        /// Creates a Stat from an int value.
        /// Usage: Stat level = StatConversions.FromInt(5);
        /// </summary>
        public static Stat FromInt(int value, string name = "IntStat")
        {
            return new Stat(name, value);
        }
        
        /// <summary>
        /// Creates a Stat from a float value.
        /// Usage: Stat health = StatConversions.FromFloat(100f);
        /// </summary>
        public static Stat FromFloat(float value, string name = "FloatStat")
        {
            return new Stat(name, value);
        }
        
        /// <summary>
        /// Creates a Stat from a bool value.
        /// Usage: Stat isActive = StatConversions.FromBool(true);
        /// </summary>
        public static Stat FromBool(bool value, string name = "BoolStat")
        {
            return new Stat(name, value ? 1f : 0f, 0f, 1f);
        }
        
        /// <summary>
        /// Creates a Stat from a Vector2 (using magnitude).
        /// Usage: Stat speed = velocity.ToStat("Speed");
        /// </summary>
        public static Stat ToStat(this Vector2 vector, string name = "Vector2Stat")
        {
            return new Stat(name, vector.magnitude);
        }
        
        /// <summary>
        /// Creates a Stat from a Vector3 (using magnitude).
        /// Usage: Stat speed = velocity.ToStat("Speed");
        /// </summary>
        public static Stat ToStat(this Vector3 vector, string name = "Vector3Stat")
        {
            return new Stat(name, vector.magnitude);
        }
        
        /// <summary>
        /// Creates a percentage Stat (0-100 range).
        /// Usage: Stat percentage = StatConversions.Percentage(0.75f); // 75%
        /// </summary>
        public static Stat Percentage(float value, string name = "Percentage")
        {
            return new Stat(name, value * 100f, 0f, 100f);
        }
        
        /// <summary>
        /// Creates a normalized Stat (0-1 range).
        /// Usage: Stat normalized = StatConversions.Normalized(0.75f);
        /// </summary>
        public static Stat Normalized(float value, string name = "Normalized")
        {
            return new Stat(name, Mathf.Clamp01(value), 0f, 1f);
        }
        
        /// <summary>
        /// Converts Stat to a 0-1 slider value for UI.
        /// Usage: slider.value = health.ToSliderValue();
        /// </summary>
        public static float ToSliderValue(this Stat stat)
        {
            return stat?.Normalize() ?? 0f;
        }
        
        /// <summary>
        /// Converts Stat to a 0-100 percentage value for UI.
        /// Usage: text.text = health.ToPercentageText(); // "75%"
        /// </summary>
        public static string ToPercentageText(this Stat stat, string format = "F0")
        {
            if (stat == null) return "0%";
            return $"{(stat.Percentage * 100).ToString(format)}%";
        }
        
        /// <summary>
        /// Converts Stat to a fraction string for UI.
        /// Usage: text.text = health.ToFractionText(); // "75/100"
        /// </summary>
        public static string ToFractionText(this Stat stat, string format = "F0")
        {
            if (stat == null) return "0/0";
            return $"{stat.Value.ToString(format)}/{stat.MaxValue.ToString(format)}";
        }
    }
}