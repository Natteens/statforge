using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatForge
{
    public static class StatExtensions
    {
        // Extension methods for Stat
        public static bool IsMaxValue(this Stat stat)
        {
            return stat.IsValid && Mathf.Approximately(stat.Value, stat.StatType.MaxValue);
        }
        
        public static bool IsMinValue(this Stat stat)
        {
            return stat.IsValid && Mathf.Approximately(stat.Value, stat.StatType.MinValue);
        }
        
        public static float GetRemainingToMax(this Stat stat)
        {
            return stat.IsValid ? stat.StatType.MaxValue - stat.Value : 0f;
        }
        
        public static float GetRemainingToMin(this Stat stat)
        {
            return stat.IsValid ? stat.Value - stat.StatType.MinValue : 0f;
        }
        
        public static void FillToMax(this Stat stat)
        {
            if (stat.IsValid)
                stat.Value = stat.StatType.MaxValue;
        }
        
        public static void EmptyToMin(this Stat stat)
        {
            if (stat.IsValid)
                stat.Value = stat.StatType.MinValue;
        }
        
        public static void ResetToDefault(this Stat stat)
        {
            if (stat.IsValid)
                stat.BaseValue = stat.StatType.DefaultValue;
        }
        
        // Extension methods for Stat collections
        public static Stat FindByName(this IEnumerable<Stat> stats, string name)
        {
            return stats.FirstOrDefault(s => s.IsValid && 
                (s.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                 s.ShortName.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                 s.Abbreviation.Equals(name, System.StringComparison.OrdinalIgnoreCase)));
        }
        
        public static Stat FindByStatType(this IEnumerable<Stat> stats, StatType statType)
        {
            return stats.FirstOrDefault(s => s.StatType == statType);
        }
        
        public static List<Stat> FilterByCategory(this IEnumerable<Stat> stats, StatCategory category)
        {
            return stats.Where(s => s.IsValid && s.StatType.Category == category).ToList();
        }
        
        public static float GetTotalValue(this IEnumerable<Stat> stats)
        {
            return stats.Where(s => s.IsValid).Sum(s => s.Value);
        }
        
        public static void ApplyModifierToAll(this IEnumerable<Stat> stats, StatModifier modifier)
        {
            foreach (var stat in stats.Where(s => s.IsValid))
            {
                stat.AddModifier(modifier);
            }
        }
        
        public static void ClearAllModifiers(this IEnumerable<Stat> stats)
        {
            foreach (var stat in stats.Where(s => s.IsValid))
            {
                stat.ClearAllModifiers();
            }
        }
        
        // Extension methods for StatType
        public static bool MatchesName(this StatType statType, string name)
        {
            if (statType == null || string.IsNullOrEmpty(name)) return false;
            
            return statType.DisplayName.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                   statType.ShortName.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                   statType.Abbreviation.Equals(name, System.StringComparison.OrdinalIgnoreCase);
        }
        
        public static string GetBestName(this StatType statType)
        {
            if (statType == null) return "Unknown";
            
            if (!string.IsNullOrEmpty(statType.Abbreviation))
                return statType.Abbreviation;
            if (!string.IsNullOrEmpty(statType.ShortName))
                return statType.ShortName;
            return statType.DisplayName;
        }
        
        // Utility methods for MonoBehaviours
        public static Stat GetStat(this MonoBehaviour component, string statName)
        {
            var stats = GetAllStats(component);
            return stats.FindByName(statName);
        }
        
        public static List<Stat> GetAllStats(this MonoBehaviour component)
        {
            var stats = new List<Stat>();
            var type = component.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Stat))
                {
                    var stat = field.GetValue(component) as Stat;
                    if (stat != null && stat.IsValid)
                        stats.Add(stat);
                }
                else if (field.FieldType == typeof(List<Stat>))
                {
                    var statList = field.GetValue(component) as List<Stat>;
                    if (statList != null)
                        stats.AddRange(statList.Where(s => s != null && s.IsValid));
                }
                else if (field.FieldType.IsArray && field.FieldType.GetElementType() == typeof(Stat))
                {
                    var statArray = field.GetValue(component) as Stat[];
                    if (statArray != null)
                        stats.AddRange(statArray.Where(s => s != null && s.IsValid));
                }
            }
            
            return stats;
        }
    }
}