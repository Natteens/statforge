using UnityEngine;
using System.Collections.Generic;

namespace StatForge
{
    /// <summary>
    /// Optional ScriptableObject for defining global stat templates and presets.
    /// Can be used to standardize stat configurations across projects.
    /// </summary>
    [CreateAssetMenu(fileName = "StatDefinition", menuName = "StatForge/Stat Definition")]
    public class StatDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string guid = System.Guid.NewGuid().ToString();
        [SerializeField] private string statName = "NewStat";
        [SerializeField] private string description = "";
        [SerializeField] private string category = "General";
        
        [Header("Default Values")]
        [SerializeField] private float defaultBaseValue = 0f;
        [SerializeField] private string defaultFormula = "";
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private bool allowModifiers = true;
        
        [Header("Display")]
        [SerializeField] private string displayFormat = "F0";
        [SerializeField] private string unit = "";
        [SerializeField] private Color displayColor = Color.white;
        
        [Header("Advanced")]
        [SerializeField] private bool cacheValue = true;
        [SerializeField] private float cacheTimeout = 0.1f;
        [SerializeField] private List<StatDefinition> dependencies = new List<StatDefinition>();
        
        #region Properties
        
        public string Guid => guid;
        public string StatName => statName;
        public string Description => description;
        public string Category => category;
        public float DefaultBaseValue => defaultBaseValue;
        public string DefaultFormula => defaultFormula;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public bool AllowModifiers => allowModifiers;
        public string DisplayFormat => displayFormat;
        public string Unit => unit;
        public Color DisplayColor => displayColor;
        public bool CacheValue => cacheValue;
        public float CacheTimeout => cacheTimeout;
        public IReadOnlyList<StatDefinition> Dependencies => dependencies;
        
        #endregion
        
        /// <summary>
        /// Creates a new Stat instance based on this definition.
        /// </summary>
        public Stat CreateStat()
        {
            Stat stat;
            
            if (!string.IsNullOrEmpty(defaultFormula))
            {
                stat = new Stat(statName, defaultFormula);
                stat.BaseValue = defaultBaseValue;
            }
            else
            {
                stat = new Stat(statName, defaultBaseValue, minValue, maxValue, allowModifiers);
            }
            
            // Apply additional configuration
            stat.MinValue = minValue;
            stat.MaxValue = maxValue;
            stat.AllowModifiers = allowModifiers;
            
            return stat;
        }
        
        /// <summary>
        /// Applies this definition's configuration to an existing stat.
        /// </summary>
        public void ApplyToStat(Stat stat)
        {
            if (stat == null) return;
            
            stat.Name = statName;
            stat.MinValue = minValue;
            stat.MaxValue = maxValue;
            stat.AllowModifiers = allowModifiers;
            
            if (!string.IsNullOrEmpty(defaultFormula))
            {
                stat.Formula = defaultFormula;
            }
            
            if (stat.BaseValue == 0f) // Only set if not already configured
            {
                stat.BaseValue = defaultBaseValue;
            }
        }
        
        /// <summary>
        /// Formats a stat value for display using this definition's format.
        /// </summary>
        public string FormatValue(float value)
        {
            var formatted = value.ToString(displayFormat);
            if (!string.IsNullOrEmpty(unit))
            {
                formatted += " " + unit;
            }
            return formatted;
        }
        
        /// <summary>
        /// Gets all stat definitions in the project.
        /// </summary>
        public static StatDefinition[] GetAllDefinitions()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:StatDefinition");
            var definitions = new List<StatDefinition>();
            
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<StatDefinition>(path);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }
            
            return definitions.ToArray();
#else
            return Resources.LoadAll<StatDefinition>("");
#endif
        }
        
        /// <summary>
        /// Finds a stat definition by GUID.
        /// </summary>
        public static StatDefinition FindByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            
            var definitions = GetAllDefinitions();
            foreach (var definition in definitions)
            {
                if (definition.Guid == guid)
                {
                    return definition;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Finds a stat definition by name.
        /// </summary>
        public static StatDefinition FindByName(string name)
        {
            var definitions = GetAllDefinitions();
            foreach (var definition in definitions)
            {
                if (definition.StatName == name)
                {
                    return definition;
                }
            }
            return null;
        }
        
        private void OnValidate()
        {
            // Ensure valid GUID
            if (string.IsNullOrEmpty(guid))
                guid = System.Guid.NewGuid().ToString();
            
            // Ensure valid values
            if (string.IsNullOrEmpty(statName))
                statName = "NewStat";
            
            if (minValue > maxValue)
                minValue = maxValue;
            
            if (defaultBaseValue < minValue)
                defaultBaseValue = minValue;
            else if (defaultBaseValue > maxValue)
                defaultBaseValue = maxValue;
            
            // Validate formula if present
            if (!string.IsNullOrEmpty(defaultFormula))
            {
                if (!IndividualStatFormulaEvaluator.ValidateFormula(defaultFormula))
                {
                    Debug.LogWarning($"Invalid formula in StatDefinition '{statName}': {defaultFormula}");
                }
            }
            
            // Check for circular dependencies
            if (dependencies.Contains(this))
            {
                dependencies.Remove(this);
                Debug.LogWarning($"StatDefinition '{statName}' cannot depend on itself!");
            }
        }
    }
}