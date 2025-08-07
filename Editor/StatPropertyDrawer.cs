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
            var baseValueProperty = property.FindPropertyRelative("baseValue");
            
            // Draw StatType field
            EditorGUI.PropertyField(statTypeRect, statTypeProperty, GUIContent.none);
            
            // Draw value field with proper handling
            var statType = statTypeProperty.objectReferenceValue as StatType;
            if (statType != null)
            {
                var currentValue = baseValueProperty.floatValue;
                var newValue = EditorGUI.FloatField(valueRect, currentValue);
                
                // Clamp value if StatType has constraints
                newValue = statType.ClampValue(newValue);
                baseValueProperty.floatValue = newValue;
                
                // Show tooltip with additional info
                var tooltip = $"{statType.DisplayName}\n" +
                             $"Range: {statType.MinValue} - {statType.MaxValue}\n" +
                             $"Default: {statType.DefaultValue}";
                if (!string.IsNullOrEmpty(statType.Abbreviation))
                    tooltip += $"\nAbbreviation: {statType.Abbreviation}";
                
                GUI.tooltip = tooltip;
            }
            else
            {
                // Show value field but make it clear no StatType is selected
                GUI.enabled = false;
                EditorGUI.FloatField(valueRect, baseValueProperty.floatValue);
                GUI.enabled = true;
                GUI.tooltip = "Select a StatType first";
            }
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif