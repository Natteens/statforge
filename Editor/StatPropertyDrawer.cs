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
            
            if (Event.current.type == EventType.Used) return;
            
            EditorGUI.BeginProperty(position, label, property);
            
            try
            {
                DrawStatProperty(position, property, label);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception ex)
            {
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
            
            EditorGUI.LabelField(rect, label.text, EditorStyles.boldLabel);
            currentY += LINE_HEIGHT + SPACING;
            
            DrawStatTypeAndValueLine(position, statTypeProperty, baseValueProperty, currentY);
            currentY += LINE_HEIGHT + SPACING;
            
            DrawInfoLine(position, property, statTypeProperty, baseValueProperty, currentY);
        }
        
        private void DrawStatTypeAndValueLine(Rect position, SerializedProperty statTypeProperty, SerializedProperty baseValueProperty, float y)
        {
            var statTypeRect = new Rect(position.x, y, position.width * LABEL_WIDTH_RATIO, LINE_HEIGHT);
            var valueRect = new Rect(position.x + position.width * (LABEL_WIDTH_RATIO + 0.02f), y, position.width * VALUE_WIDTH_RATIO, LINE_HEIGHT);
            
            try
            {
                EditorGUI.BeginChangeCheck();
                
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    EditorGUI.PropertyField(statTypeRect, statTypeProperty, GUIContent.none);
                }
                
                if (EditorGUI.EndChangeCheck() && !EditorApplication.isPlaying)
                {
                    HandleStatTypeChange(statTypeProperty, baseValueProperty);
                }
                
                if (statTypeProperty.objectReferenceValue != null)
                {
                    var statType = statTypeProperty.objectReferenceValue as StatType;
                    if (statType != null)
                    {
                        if (ShouldSetDefaultValue(baseValueProperty, statType))
                        {
                            SetDefaultValue(baseValueProperty, statType);
                        }
                        
                        EditorGUI.PropertyField(valueRect, baseValueProperty, GUIContent.none);
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.FloatField(valueRect, 0f);
                    }
                }
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error in DrawStatTypeAndValueLine: {ex}");
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
                    DrawErrorInfo(rect, "Debug: " + GetPropertyPath(property));
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
        
        private string GetPropertyPath(SerializedProperty property)
        {
            return $"Path: {property.propertyPath} | Target: {property.serializedObject.targetObject?.GetType().Name}";
        }
        
        private Stat GetStatFromProperty(SerializedProperty property)
        {
            try
            {
                var targetObject = property.serializedObject.targetObject;
                if (targetObject == null) return null;
                
                // Parse do property path para navegar pela hierarquia
                var pathParts = property.propertyPath.Split('.');
                object currentObject = targetObject;
                
                foreach (var part in pathParts)
                {
                    if (currentObject == null) break;
                    
                    var field = currentObject.GetType().GetField(part,
                        BindingFlags.NonPublic |
                        BindingFlags.Public |
                        BindingFlags.Instance);
                    
                    if (field == null) break;
                    
                    currentObject = field.GetValue(currentObject);
                }
                
                return currentObject as Stat;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error getting stat from property: {ex}");
                return null;
            }
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
                
                EditorApplication.delayCall += () =>
                {
                    if (baseValueProperty.serializedObject != null)
                    {
                        baseValueProperty.serializedObject.ApplyModifiedProperties();
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error handling StatType change: {ex}");
            }
        }
        
        private bool ShouldSetDefaultValue(SerializedProperty baseValueProperty, StatType statType)
        {
            return Mathf.Approximately(baseValueProperty.floatValue, 0f) && 
                   !Mathf.Approximately(statType.DefaultValue, 0f);
        }
        
        private void SetDefaultValue(SerializedProperty baseValueProperty, StatType statType)
        {
            try
            {
                baseValueProperty.floatValue = statType.DefaultValue;
                
                EditorApplication.delayCall += () =>
                {
                    if (baseValueProperty.serializedObject != null)
                    {
                        baseValueProperty.serializedObject.ApplyModifiedProperties();
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatForge] Error setting default value: {ex}");
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (LINE_HEIGHT * 3) + (SPACING * 3);
        }
    }
}
#endif