#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomEditor(typeof(StatType))]
    public class StatTypeEditor : UnityEditor.Editor
    {
        private const float SECTION_SPACING = 10f;
        private const float MINI_SPACING = 5f;
        private const float BUTTON_WIDTH = 50f;
        private const float FIELD_WIDTH = 80f;
        private const float LABEL_WIDTH = 60f;
        
        private bool hasErrors;
        private string errorMessage;
        
        public override void OnInspectorGUI()
        {
            var statType = target as StatType;
            if (statType == null)
            {
                EditorGUILayout.HelpBox("StatType target is null!", MessageType.Error);
                return;
            }
            
            serializedObject.Update();
            hasErrors = false;
            errorMessage = "";
            
            try
            {
                DrawInspectorContent(statType);
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Editor Error: {ex.Message}", MessageType.Error);
                Debug.LogError($"[StatForge] StatTypeEditor Error: {ex}");
            }
            
            if (hasErrors)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(statType);
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void DrawInspectorContent(StatType statType)
        {
            EditorGUILayout.Space(MINI_SPACING);
            
            DrawBasicInformation(statType);
            DrawValueTypeSection(statType);
            DrawValueConfiguration(statType);
            DrawFormulaSection(statType);
            DrawDescriptionSection(statType);
            
            ValidateStatType(statType);
        }
        
        private void DrawBasicInformation(StatType statType)
        {
            EditorGUILayout.LabelField("📋 Basic Information", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            statType.DisplayName = EditorGUILayout.TextField("Display Name", statType.DisplayName ?? "");
            
            if (EditorGUI.EndChangeCheck() && string.IsNullOrEmpty(statType.ShortName))
            {
                // Auto-gera short name se estiver vazio
                statType.ShortName = GenerateShortName(statType.DisplayName);
            }
            
            EditorGUILayout.BeginHorizontal();
            statType.ShortName = EditorGUILayout.TextField("Short Name", statType.ShortName ?? "");
            
            if (GUILayout.Button("Auto", GUILayout.Width(BUTTON_WIDTH)))
            {
                statType.ShortName = GenerateShortName(statType.DisplayName);
                EditorUtility.SetDirty(statType);
            }
            EditorGUILayout.EndHorizontal();
            
            statType.Category = EditorGUILayout.TextField("Category", statType.Category ?? "General");
            
            EditorGUILayout.Space(SECTION_SPACING);
        }
        
        private void DrawValueTypeSection(StatType statType)
        {
            EditorGUILayout.LabelField("🔢 Value Type", EditorStyles.boldLabel);
            
            var oldValueType = statType.ValueType;
            statType.ValueType = (StatValueType)EditorGUILayout.EnumPopup("Type", statType.ValueType);
            
            if (oldValueType != statType.ValueType)
            {
                // CORREÇÃO: Chama método de ajuste manual em vez do removido
                AdjustRangeForValueType(statType);
                EditorUtility.SetDirty(statType);
            }
            
            DrawValueTypePreview(statType);
            
            EditorGUILayout.Space(SECTION_SPACING);
        }
        
        // CORREÇÃO: Método próprio do Editor para ajustar ranges
        private void AdjustRangeForValueType(StatType statType)
        {
            if (statType.ValueType == StatValueType.Percentage)
            {
                // Ajusta para porcentagem se necessário
                if (statType.MinValue < 0f) statType.MinValue = 0f;
                if (statType.MaxValue > 100f) statType.MaxValue = 100f;
                
                // Ajusta default se estiver fora do range
                if (statType.DefaultValue < statType.MinValue)
                    statType.DefaultValue = statType.MinValue;
                else if (statType.DefaultValue > statType.MaxValue)
                    statType.DefaultValue = statType.MaxValue;
            }
        }
        
        private void DrawValueTypePreview(StatType statType)
        {
            var previewStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
            
            EditorGUILayout.BeginVertical(previewStyle);
            EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
            
            try
            {
                var exampleValue = statType.ValueType == StatValueType.Percentage ? 25.5f : 150.75f;
                var formattedExample = statType.FormatValue(exampleValue);
                EditorGUILayout.LabelField($"Example: {formattedExample}", EditorStyles.boldLabel);
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"Preview Error: {ex.Message}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawValueConfiguration(StatType statType)
        {
            EditorGUILayout.LabelField("⚙️ Value Configuration", EditorStyles.boldLabel);
            
            statType.DefaultValue = EditorGUILayout.FloatField("Default Value", statType.DefaultValue);
            
            if (statType.ValueType == StatValueType.Percentage)
            {
                DrawPercentageLimits(statType);
            }
            
            EditorGUILayout.Space(SECTION_SPACING);
        }
        
        private void DrawPercentageLimits(StatType statType)
        {
            EditorGUILayout.Space(MINI_SPACING);
            EditorGUILayout.LabelField("📊 Percentage Limits", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Min", GUILayout.Width(LABEL_WIDTH));
            var newMin = EditorGUILayout.FloatField(statType.MinValue, GUILayout.Width(FIELD_WIDTH));
            
            GUILayout.Space(20);
            
            EditorGUILayout.LabelField("Max", GUILayout.Width(LABEL_WIDTH));
            var newMax = EditorGUILayout.FloatField(statType.MaxValue, GUILayout.Width(FIELD_WIDTH));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // Validação dos limites
            if (newMin > newMax)
            {
                hasErrors = true;
                errorMessage = "Minimum value cannot be greater than maximum value!";
            }
            else
            {
                statType.MinValue = newMin;
                statType.MaxValue = newMax;
            }
            
            if (GUILayout.Button("Reset to 0-100%"))
            {
                statType.MinValue = 0f;
                statType.MaxValue = 100f;
                EditorUtility.SetDirty(statType);
            }
            
            // Validação do valor padrão
            if (statType.DefaultValue < statType.MinValue || statType.DefaultValue > statType.MaxValue)
            {
                EditorGUILayout.HelpBox($"Default value ({statType.DefaultValue:F1}) is outside limits [{statType.MinValue:F1}, {statType.MaxValue:F1}]", MessageType.Warning);
            }
        }
        
        private void DrawFormulaSection(StatType statType)
        {
            EditorGUILayout.LabelField("🧮 Formula (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use stat abbreviations (e.g. CON * 15 + STR * 2)", EditorStyles.miniLabel);
            
            var oldFormula = statType.Formula;
            statType.Formula = EditorGUILayout.TextArea(statType.Formula ?? "", GUILayout.Height(60));
            
            if (!string.IsNullOrEmpty(statType.Formula))
            {
                DrawFormulaValidation(statType);
            }
            
            EditorGUILayout.Space(SECTION_SPACING);
        }
        
        private void DrawFormulaValidation(StatType statType)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 4)
            };
            
            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.LabelField("Formula Info:", EditorStyles.miniLabel);
            
            try
            {
                if (statType.HasFormula)
                {
                    EditorGUILayout.LabelField("✅ Formula detected", EditorStyles.miniLabel);
                    
                    // Tentativa básica de validação
                    if (HasValidFormulaStructure(statType.Formula))
                    {
                        EditorGUILayout.LabelField("✅ Basic structure looks valid", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("⚠ Formula structure may have issues", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("❌ No valid formula", EditorStyles.miniLabel);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"❌ Formula validation error: {ex.Message}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDescriptionSection(StatType statType)
        {
            EditorGUILayout.LabelField("📝 Description", EditorStyles.boldLabel);
            statType.Description = EditorGUILayout.TextArea(statType.Description ?? "", GUILayout.Height(80));
        }
        
        private void ValidateStatType(StatType statType)
        {
            if (string.IsNullOrWhiteSpace(statType.DisplayName))
            {
                hasErrors = true;
                errorMessage += "Display Name is required. ";
            }
            
            if (string.IsNullOrWhiteSpace(statType.ShortName))
            {
                hasErrors = true;
                errorMessage += "Short Name is required. ";
            }
        }
        
        private bool HasValidFormulaStructure(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula)) return false;
            
            try
            {
                // Verificações básicas de estrutura
                var hasLetters = System.Text.RegularExpressions.Regex.IsMatch(formula, @"[A-Za-z]");
                var hasNumbers = System.Text.RegularExpressions.Regex.IsMatch(formula, @"\d");
                var hasOperators = System.Text.RegularExpressions.Regex.IsMatch(formula, @"[+\-*/]");
                
                return hasLetters && (hasNumbers || hasOperators);
            }
            catch
            {
                return false;
            }
        }
        
        private string GenerateShortName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "";
            
            try
            {
                var words = displayName.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var result = "";
                
                foreach (var word in words)
                {
                    if (word.Length > 0)
                    {
                        result += char.ToUpper(word[0]);
                    }
                }
                
                return result.Length > 4 ? result.Substring(0, 4) : result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error generating short name: {ex}");
                return "STAT";
            }
        }
    }
}
#endif