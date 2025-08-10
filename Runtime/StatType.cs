using UnityEngine;
using System.Globalization;

namespace StatForge
{
    public class StatType : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string displayName;
        [SerializeField] private string shortName = "";
        [SerializeField] private string category = "General";
        [SerializeField] private StatValueType valueType = StatValueType.Normal;
        
        [Header("Value Configuration")]
        [SerializeField] private float defaultValue;
        
        [Header("Limits (Percentage Only)")]
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 100f;
        
        [Header("Formula (Optional)")]
        [TextArea(2, 4)]
        [SerializeField] private string formula;
        
        [Header("Description")]
        [TextArea(3, 5)]
        [SerializeField] private string description;
        
        public string DisplayName 
        { 
            get => string.IsNullOrEmpty(displayName) ? name : displayName;
            set => displayName = value;
        }
        
        public string ShortName 
        { 
            get => string.IsNullOrEmpty(shortName) ? GenerateShortName() : shortName;
            set => shortName = value;
        }
        
        public string Category { get => category; set => category = value; }
        public StatValueType ValueType { get => valueType; set => valueType = value; }
        public float DefaultValue { get => defaultValue; set => defaultValue = value; }
        public string Formula { get => formula; set => formula = value; }
        public string Description { get => description; set => description = value; }
        
        public float MinValue 
        { 
            get => valueType == StatValueType.Percentage ? minValue : float.MinValue;
            set => minValue = value;
        }
        
        public float MaxValue 
        { 
            get => valueType == StatValueType.Percentage ? maxValue : float.MaxValue;
            set => maxValue = value;
        }
        
        public bool HasFormula => !string.IsNullOrEmpty(formula);
        
        public string FormatValue(float value)
        {
            return valueType switch
            {
                StatValueType.Normal => value.ToString("F2", CultureInfo.InvariantCulture),
                StatValueType.Percentage => $"{value.ToString("F2", CultureInfo.InvariantCulture)}%",
                StatValueType.Rate => $"{value.ToString("F1", CultureInfo.InvariantCulture)}/s",
                _ => value.ToString("F1", CultureInfo.InvariantCulture)
            };
        }
        
        private string GenerateShortName()
        {
            if (string.IsNullOrEmpty(DisplayName)) return "";
            
            var words = DisplayName.Split(' ');
            var result = "";
            
            foreach (var word in words)
            {
                if (word.Length > 0)
                    result += char.ToUpper(word[0]);
            }
            
            return result.Length > 4 ? result.Substring(0, 4) : result;
        }
        
        private void OnValidate()
        {
            if (valueType == StatValueType.Percentage)
            {
                if (minValue > maxValue)
                    minValue = maxValue;
                
                if (defaultValue < minValue)
                    defaultValue = minValue;
                else if (defaultValue > maxValue)
                    defaultValue = maxValue;
            }
        }
    }
}