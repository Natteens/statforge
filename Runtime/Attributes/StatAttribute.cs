using System;

namespace StatForge
{
    /// <summary>
    /// Mark fields to be automatically managed by StatForge
    /// Usage: [Stat] public int Strength = 10;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class StatAttribute : Attribute
    {
        /// <summary>
        /// Display name for the stat (optional, defaults to field name)
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Short name for formulas (optional, defaults to field name)
        /// </summary>
        public string ShortName { get; set; }
        
        /// <summary>
        /// Minimum value (optional)
        /// </summary>
        public object MinValue { get; set; }
        
        /// <summary>
        /// Maximum value (optional)
        /// </summary>
        public object MaxValue { get; set; }
        
        /// <summary>
        /// Formula for derived stats (optional)
        /// </summary>
        public string Formula { get; set; }
        
        /// <summary>
        /// Category of the stat
        /// </summary>
        public StatCategory Category { get; set; } = StatCategory.Primary;
        
        public StatAttribute()
        {
        }
        
        public StatAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        
        public StatAttribute(string displayName, string shortName)
        {
            DisplayName = displayName;
            ShortName = shortName;
        }
    }
}