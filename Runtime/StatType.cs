using UnityEngine;

namespace StatForge
{
    [CreateAssetMenu(fileName = "New Stat", menuName = "StatForge/Stat Type")]
    public class StatType : ScriptableObject
    {
        [Header("Informações Básicas")]
        [SerializeField] private string displayName = "";
        [SerializeField] private string shortName = "";
        [SerializeField] private string category = "General";
        
        [Header("Configuração de Valores")]
        [SerializeField] private float defaultValue = 0f;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100000f;
        
        [Header("Fórmula (Opcional)")]
        [TextArea(2, 4)]
        [SerializeField] private string formula = "";
        
        [Header("Descrição")]
        [TextArea(3, 5)]
        [SerializeField] private string description = "";
        
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
        public float DefaultValue { get => defaultValue; set => defaultValue = value; }
        public float MinValue { get => minValue; set => minValue = value; }
        public float MaxValue { get => maxValue; set => maxValue = value; }
        public string Formula { get => formula; set => formula = value; }
        public string Description { get => description; set => description = value; }
        
        public bool HasFormula => !string.IsNullOrEmpty(formula);
        
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
            if (minValue > maxValue)
                minValue = maxValue;
            
            if (defaultValue < minValue)
                defaultValue = minValue;
            else if (defaultValue > maxValue)
                defaultValue = maxValue;
                
            if (maxValue > 1000000f)
            {
                Debug.LogWarning($"[StatForge] Valor máximo muito alto para {displayName}. Considere usar valores menores para evitar overflow.");
            }
        }
    }
}