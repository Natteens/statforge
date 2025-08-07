using UnityEngine;
using System.Linq; 

namespace StatForge
{
    [CreateAssetMenu(fileName = "New Stat Definition", menuName = "Scriptable Objects/StatForge/Stat Definition")]
    public class StatDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string displayName = "";
        [SerializeField] private string shortName = "";
        [SerializeField] private string abbreviation = "";
        [SerializeField] private StatCategory category = StatCategory.Primary;
        
        [Header("Visual")]
        [SerializeField] private Color statColor = Color.white;
        [SerializeField] private Sprite icon;
        
        [Header("Value Configuration")]
        [SerializeField] private float defaultValue = 0f;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;
        
        [Header("Derived Formula (Derived Stats Only)")]
        [TextArea(2, 4)]
        [SerializeField] private string formula = "";
        [SerializeField] private StatDefinition[] dependencies;
        
        public string DisplayName 
        { 
            get => displayName; 
            set => displayName = value; 
        }
        
        public string ShortName 
        { 
            get => shortName; 
            set => shortName = value; 
        }
        
        public string Abbreviation 
        { 
            get => abbreviation; 
            set => abbreviation = value; 
        }
        
        public StatCategory Category 
        { 
            get => category; 
            set => category = value; 
        }
        
        public Color StatColor
        {
            get => statColor;
            set => statColor = value;
        }
        
        public Sprite Icon
        {
            get => icon;
            set => icon = value;
        }
        
        public float DefaultValue 
        { 
            get => defaultValue; 
            set => defaultValue = value; 
        }
        
        public float MinValue 
        { 
            get => minValue; 
            set => minValue = value; 
        }
        
        public float MaxValue 
        { 
            get => maxValue; 
            set => maxValue = value; 
        }
        
        public string Formula 
        { 
            get => formula; 
            set 
            {
                formula = FormulaEvaluator.PreProcessFormula(value);
            } 
        }
        
        public StatDefinition[] Dependencies => dependencies;
        public string Id => name;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = name;
            
            if (!string.IsNullOrEmpty(shortName))
                shortName = shortName.Replace(" ", "");
            
            if (!string.IsNullOrEmpty(abbreviation))
                abbreviation = abbreviation.Replace(" ", "").ToUpper();
            
            if (category == StatCategory.Derived && string.IsNullOrEmpty(formula))
            {
                Debug.LogWarning($"Derived stat '{displayName}' should have a formula defined.");
            }
            else if (category == StatCategory.Derived && !string.IsNullOrEmpty(formula))
            {
                formula = FormulaEvaluator.PreProcessFormula(formula);
            }
            
            if (category != StatCategory.Derived && !string.IsNullOrEmpty(formula))
            {
                Debug.LogWarning($"Non-derived stat '{displayName}' has a formula but it won't be used.");
            }
            
            if (dependencies != null && dependencies.Any(d => d == this))
            {
                Debug.LogError($"Stat '{displayName}' cannot depend on itself!");
                dependencies = dependencies.Where(d => d != this).ToArray();
            }
            
            ClampValues();
        }
        
        private void ClampValues()
        {
            if (minValue > maxValue)
                minValue = maxValue;
            
            if (defaultValue < minValue)
                defaultValue = minValue;
            else if (defaultValue > maxValue)
                defaultValue = maxValue;
        }
        
        public bool IsValidValue(float value)
        {
            return value >= minValue && value <= maxValue;
        }
        
        public float ClampValue(float value)
        {
            return Mathf.Clamp(value, minValue, maxValue);
        }
        
        public bool IsDerived => category == StatCategory.Derived;
        public bool IsPrimary => category == StatCategory.Primary;
        public bool IsExternal => category == StatCategory.External;
        
        // Conversion methods for backward compatibility
        public static implicit operator StatType(StatDefinition statDefinition)
        {
            if (statDefinition == null) return null;
            
            var statType = CreateInstance<StatType>();
            statType.DisplayName = statDefinition.DisplayName;
            statType.ShortName = statDefinition.ShortName;
            statType.Abbreviation = statDefinition.Abbreviation;
            statType.Category = statDefinition.Category;
            statType.DefaultValue = statDefinition.DefaultValue;
            statType.MinValue = statDefinition.MinValue;
            statType.MaxValue = statDefinition.MaxValue;
            statType.Formula = statDefinition.Formula;
            
            return statType;
        }
        
        public StatType ToStatType()
        {
            return this;
        }
    }
}