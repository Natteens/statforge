#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomPropertyDrawer(typeof(Stat))]
    public class StatPropertyDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float SPACING = 2f;
        private const float LABEL_WIDTH_RATIO = 0.65f;
        private const float VALUE_WIDTH_RATIO = 0.33f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null) return;
            
            EditorGUI.BeginProperty(position, label, property);
            
            try
            {
                DrawStatProperty(position, property, label);
            }
            catch (Exception ex)
            {
                // Fallback seguro em caso de erro
                var errorRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
                EditorGUI.LabelField(errorRect, $"[StatForge Error]: {ex.Message}", EditorStyles.helpBox);
                Debug.LogError($"[StatForge] StatPropertyDrawer Error: {ex}");
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }
        
        private void DrawStatProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            var statTypeProperty = property.FindPropertyRelative("statType");
            var baseValueProperty = property.FindPropertyRelative("baseValue");
            
            if (statTypeProperty == null || baseValueProperty == null)
            {
                EditorGUI.LabelField(position, "StatForge: Invalid property structure");
                return;
            }
            
            var currentY = position.y;
            var rect = new Rect(position.x, currentY, position.width, LINE_HEIGHT);
            
            // Header principal
            EditorGUI.LabelField(rect, label.text, EditorStyles.boldLabel);
            currentY += LINE_HEIGHT + SPACING;
            
            // Linha StatType e BaseValue
            DrawStatTypeAndValueLine(position, statTypeProperty, baseValueProperty, currentY);
            currentY += LINE_HEIGHT + SPACING;
            
            // Linha de informações (runtime ou preview)
            DrawInfoLine(position, property, statTypeProperty, baseValueProperty, currentY);
        }
        
        private void DrawStatTypeAndValueLine(Rect position, SerializedProperty statTypeProperty, SerializedProperty baseValueProperty, float y)
        {
            var statTypeRect = new Rect(position.x, y, position.width * LABEL_WIDTH_RATIO, LINE_HEIGHT);
            var valueRect = new Rect(position.x + position.width * (LABEL_WIDTH_RATIO + 0.02f), y, position.width * VALUE_WIDTH_RATIO, LINE_HEIGHT);
            
            // StatType field
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(statTypeRect, statTypeProperty, GUIContent.none);
            
            if (EditorGUI.EndChangeCheck())
            {
                HandleStatTypeChange(statTypeProperty, baseValueProperty);
            }
            
            // Value field (só se tiver StatType)
            if (statTypeProperty.objectReferenceValue != null)
            {
                var statType = statTypeProperty.objectReferenceValue as StatType;
                if (statType != null)
                {
                    // Auto-set default value se necessário
                    if (ShouldSetDefaultValue(baseValueProperty, statType))
                    {
                        SetDefaultValue(baseValueProperty, statType);
                    }
                    
                    EditorGUI.PropertyField(valueRect, baseValueProperty, GUIContent.none);
                }
            }
            else
            {
                // Se não tem StatType, mostra field desabilitado
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.FloatField(valueRect, 0f);
                }
            }
        }
        
        private void DrawInfoLine(Rect position, SerializedProperty property, SerializedProperty statTypeProperty, SerializedProperty baseValueProperty, float y)
        {
            var infoRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
            var statType = statTypeProperty.objectReferenceValue as StatType;
            
            if (statType == null)
            {
                DrawNoStatTypeInfo(infoRect);
                return;
            }
            
            if (Application.isPlaying)
            {
                DrawRuntimeInfo(infoRect, property);
            }
            else
            {
                DrawEditorInfo(infoRect, statType, baseValueProperty.floatValue);
            }
        }
        
        private void DrawNoStatTypeInfo(Rect rect)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.red }
            };
            EditorGUI.LabelField(rect, "⚠ Assign a StatType", style);
        }
        
        private void DrawRuntimeInfo(Rect rect, SerializedProperty property)
        {
            try
            {
                var stat = GetStatFromProperty(property);
                if (stat != null)
                {
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
                    };
                    
                    var displayText = $"🔄 Runtime: {stat.FormattedValue}";
                    
                    if (stat.HasModifiers)
                    {
                        displayText += $" ({stat.Modifiers.Count} mods)";
                    }
                    
                    if (stat.HasFormula)
                    {
                        displayText += " [+formula]";
                    }
                    
                    EditorGUI.LabelField(rect, displayText, style);
                }
                else
                {
                    DrawErrorInfo(rect, "Runtime stat não encontrado");
                }
            }
            catch (Exception ex)
            {
                DrawErrorInfo(rect, $"Runtime error: {ex.Message}");
            }
        }
        
        private void DrawEditorInfo(Rect rect, StatType statType, float baseValue)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            
            try
            {
                var formattedValue = statType.FormatValue(baseValue);
                var infoText = $"📋 {statType.ShortName} | Preview: {formattedValue}";
                
                if (statType.HasFormula)
                {
                    infoText += " + formula";
                }
                
                EditorGUI.LabelField(rect, infoText, style);
            }
            catch (Exception ex)
            {
                DrawErrorInfo(rect, $"Preview error: {ex.Message}");
            }
        }
        
        private void DrawErrorInfo(Rect rect, string message)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.red }
            };
            EditorGUI.LabelField(rect, $"⚠ {message}", style);
        }
        
        private void HandleStatTypeChange(SerializedProperty statTypeProperty, SerializedProperty baseValueProperty)
        {
            try
            {
                if (statTypeProperty.objectReferenceValue != null)
                {
                    var statType = statTypeProperty.objectReferenceValue as StatType;
                    if (statType != null)
                    {
                        baseValueProperty.floatValue = statType.DefaultValue;
                    }
                }
                else
                {
                    baseValueProperty.floatValue = 0f;
                }
                
                baseValueProperty.serializedObject.ApplyModifiedProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error handling StatType change: {ex}");
            }
        }
        
        private bool ShouldSetDefaultValue(SerializedProperty baseValueProperty, StatType statType)
        {
            // Só define valor padrão se o valor atual for zero E o StatType tem valor padrão diferente de zero
            return Mathf.Approximately(baseValueProperty.floatValue, 0f) && 
                   !Mathf.Approximately(statType.DefaultValue, 0f);
        }
        
        private void SetDefaultValue(SerializedProperty baseValueProperty, StatType statType)
        {
            try
            {
                baseValueProperty.floatValue = statType.DefaultValue;
                baseValueProperty.serializedObject.ApplyModifiedProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error setting default value: {ex}");
            }
        }
        
        private Stat GetStatFromProperty(SerializedProperty property)
        {
            try
            {
                var targetObject = property.serializedObject.targetObject;
                if (targetObject == null) return null;
                
                var fieldPath = property.propertyPath;
                var field = targetObject.GetType().GetField(fieldPath,
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.Instance);
                
                return field?.GetValue(targetObject) as Stat;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error getting stat from property: {ex}");
                return null;
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (LINE_HEIGHT * 3) + (SPACING * 3);
        }
    }
}
#endif