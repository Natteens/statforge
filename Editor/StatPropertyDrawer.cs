#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomPropertyDrawer(typeof(Stat))]
    public class StatPropertyDrawer : PropertyDrawer
    {
        private const float STAT_TYPE_RATIO = 0.6f;
        private const float VALUE_RATIO = 0.4f;
        private const float SPACING = 2f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            // Calculate rects
            var statTypeRect = new Rect(position.x, position.y, position.width * STAT_TYPE_RATIO - SPACING, position.height);
            var valueRect = new Rect(position.x + position.width * STAT_TYPE_RATIO, position.y, position.width * VALUE_RATIO, position.height);
            
            // Get properties
            var statTypeProperty = property.FindPropertyRelative("statType");
            var statDefinitionProperty = property.FindPropertyRelative("statDefinition");
            var baseValueProperty = property.FindPropertyRelative("baseValue");
            
            // Check which one is being used (prioritize StatDefinition)
            var statDefinition = statDefinitionProperty.objectReferenceValue as StatDefinition;
            var statType = statTypeProperty.objectReferenceValue as StatType;
            var currentStat = statDefinition != null ? (object)statDefinition : statType;
            
            // Draw appropriate field based on what's currently set
            if (statDefinition != null)
            {
                // Clear statType if statDefinition is set
                if (statType != null)
                    statTypeProperty.objectReferenceValue = null;
                    
                EditorGUI.PropertyField(statTypeRect, statDefinitionProperty, GUIContent.none);
            }
            else
            {
                // Show a combined field that can accept both types
                var newValue = EditorGUI.ObjectField(statTypeRect, currentStat, typeof(Object), false);
                
                if (newValue is StatDefinition newDefinition)
                {
                    statDefinitionProperty.objectReferenceValue = newDefinition;
                    statTypeProperty.objectReferenceValue = null;
                }
                else if (newValue is StatType newStatType)
                {
                    statTypeProperty.objectReferenceValue = newStatType;
                    statDefinitionProperty.objectReferenceValue = null;
                }
                else if (newValue == null)
                {
                    statTypeProperty.objectReferenceValue = null;
                    statDefinitionProperty.objectReferenceValue = null;
                }
            }
            
            // Draw value field
            DrawValueField(valueRect, baseValueProperty, currentStat);
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        
        private void DrawValueField(Rect valueRect, SerializedProperty baseValueProperty, object currentStat)
        {
            if (currentStat != null)
            {
                var currentValue = baseValueProperty.floatValue;
                var newValue = EditorGUI.FloatField(valueRect, currentValue);
                
                // Clamp value based on stat constraints
                if (currentStat is StatDefinition definition)
                {
                    newValue = definition.ClampValue(newValue);
                    SetTooltip(definition);
                }
                else if (currentStat is StatType statType)
                {
                    newValue = statType.ClampValue(newValue);
                    SetTooltip(statType);
                }
                
                baseValueProperty.floatValue = newValue;
            }
            else
            {
                // Show value field but make it clear no stat is selected
                GUI.enabled = false;
                EditorGUI.FloatField(valueRect, baseValueProperty.floatValue);
                GUI.enabled = true;
                GUI.tooltip = "Select a StatType or StatDefinition first";
            }
        }
        
        private void SetTooltip(StatDefinition definition)
        {
            var tooltip = $"{definition.DisplayName}\n" +
                         $"Range: {definition.MinValue} - {definition.MaxValue}\n" +
                         $"Default: {definition.DefaultValue}";
            if (!string.IsNullOrEmpty(definition.Abbreviation))
                tooltip += $"\nAbbreviation: {definition.Abbreviation}";
            
            GUI.tooltip = tooltip;
        }
        
        private void SetTooltip(StatType statType)
        {
            var tooltip = $"{statType.DisplayName}\n" +
                         $"Range: {statType.MinValue} - {statType.MaxValue}\n" +
                         $"Default: {statType.DefaultValue}";
            if (!string.IsNullOrEmpty(statType.Abbreviation))
                tooltip += $"\nAbbreviation: {statType.Abbreviation}";
            
            GUI.tooltip = tooltip;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif