using System;

namespace StatForge
{
    /// <summary>
    /// Simple attribute to mark fields as stats for automatic management.
    /// Usage: [Stat] public float health = 100f; or [Stat] public Stat health = new Stat("Health", 100f);
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class StatAttribute : Attribute
    {
        /// <summary>
        /// Optional custom name for the stat. If not provided, uses the field/property name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Minimum value for this stat. Default is 0.
        /// </summary>
        public float MinValue { get; set; } = 0f;
        
        /// <summary>
        /// Maximum value for this stat. Default is float.MaxValue.
        /// </summary>
        public float MaxValue { get; set; } = float.MaxValue;
        
        /// <summary>
        /// Whether this stat can be modified by external modifiers. Default is true.
        /// </summary>
        public bool AllowModifiers { get; set; } = true;
        
        public StatAttribute()
        {
        }
        
        public StatAttribute(string name)
        {
            Name = name;
        }
    }
    
    /// <summary>
    /// Attribute for derived stats that are calculated from formulas.
    /// Usage: [DerivedStat("strength * 2")] public int damage;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DerivedStatAttribute : StatAttribute
    {
        /// <summary>
        /// Formula used to calculate this derived stat.
        /// </summary>
        public string Formula { get; set; }
        
        public DerivedStatAttribute(string formula)
        {
            Formula = formula;
        }
        
        public DerivedStatAttribute(string name, string formula) : base(name)
        {
            Formula = formula;
        }
    }
}