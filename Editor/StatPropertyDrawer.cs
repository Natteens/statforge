#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomPropertyDrawer(typeof(Stat))]
    public class StatPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var statTypeProperty = property.FindPropertyRelative("statType");
            var baseValueProperty = property.FindPropertyRelative("baseValue");

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(rect, label.text, EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            var statTypeRect = new Rect(rect.x, rect.y, rect.width * 0.65f, rect.height);
            var valueRect = new Rect(rect.x + rect.width * 0.67f, rect.y, rect.width * 0.33f, rect.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(statTypeRect, statTypeProperty, GUIContent.none);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (statTypeProperty.objectReferenceValue != null)
                {
                    var statType = statTypeProperty.objectReferenceValue as StatType;
                    if (statType != null)
                    {
                        baseValueProperty.floatValue = statType.DefaultValue;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    baseValueProperty.floatValue = 0f;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            if (statTypeProperty.objectReferenceValue != null)
            {
                var statType = statTypeProperty.objectReferenceValue as StatType;
                if (Mathf.Approximately(baseValueProperty.floatValue, 0f) && statType != null && !Mathf.Approximately(statType.DefaultValue, 0f))
                {
                    baseValueProperty.floatValue = statType.DefaultValue;
                    property.serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.PropertyField(valueRect, baseValueProperty, GUIContent.none);

                rect.y += EditorGUIUtility.singleLineHeight + 2;

                if (Application.isPlaying)
                {
                    var targetObject = property.serializedObject.targetObject;
                    var stat = GetStatFromProperty(property, targetObject);
                    if (stat != null)
                    {
                        var infoStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.2f, 0.7f, 0.2f) }
                        };
                        
                        var displayText = $"Runtime: {stat.FormattedValue}";
                        if (stat.HasModifiers)
                        {
                            displayText += $" ({stat.Modifiers.Count} mods)";
                        }
                        
                        EditorGUI.LabelField(rect, displayText, infoStyle);
                    }
                }
                else
                {
                    var infoStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = Color.gray }
                    };
                    
                    var formattedValue = statType.FormatValue(baseValueProperty.floatValue);
                    var infoText = $"{statType.ShortName} | {formattedValue}";
                    if (statType.HasFormula) infoText += " + formula";
                    
                    EditorGUI.LabelField(rect, infoText, infoStyle);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + 6f;
        }

        private Stat GetStatFromProperty(SerializedProperty property, object target)
        {
            var fieldName = property.propertyPath;
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance);

            return field?.GetValue(target) as Stat;
        }
    }
}
#endif