using UnityEngine;
using UnityEditor;
using StatForge;

namespace StatForge.Editor
{
    /// <summary>
    /// Property drawer for the new Stat class to provide a clean editor experience.
    /// </summary>
    [CustomPropertyDrawer(typeof(Stat))]
    public class StatPropertyDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float SPACING = 2f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Get serialized properties
            var nameProperty = property.FindPropertyRelative("_name");
            var baseValueProperty = property.FindPropertyRelative("_baseValue");
            var formulaProperty = property.FindPropertyRelative("_formula");
            var minValueProperty = property.FindPropertyRelative("_minValue");
            var maxValueProperty = property.FindPropertyRelative("_maxValue");
            var allowModifiersProperty = property.FindPropertyRelative("_allowModifiers");
            
            var currentRect = position;
            currentRect.height = LINE_HEIGHT;
            
            // Main label
            EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Indent for sub-properties
            EditorGUI.indentLevel++;
            
            // Name field
            EditorGUI.PropertyField(currentRect, nameProperty, new GUIContent("Name"));
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Base Value field
            EditorGUI.PropertyField(currentRect, baseValueProperty, new GUIContent("Base Value"));
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Formula field (if present)
            if (formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue))
            {
                var formulaRect = currentRect;
                formulaRect.height = LINE_HEIGHT * 2; // Multi-line for formulas
                EditorGUI.PropertyField(formulaRect, formulaProperty, new GUIContent("Formula"));
                currentRect.y += (LINE_HEIGHT * 2) + SPACING;
                
                // Show if it's a derived stat
                var isDerivedRect = currentRect;
                EditorGUI.LabelField(isDerivedRect, "Type", "Derived Stat", EditorStyles.miniLabel);
                currentRect.y += LINE_HEIGHT + SPACING;
            }
            
            // Min/Max values in a single line
            var minMaxRect = currentRect;
            var halfWidth = minMaxRect.width / 2f - 5f;
            
            var minRect = new Rect(minMaxRect.x, minMaxRect.y, halfWidth, LINE_HEIGHT);
            var maxRect = new Rect(minMaxRect.x + halfWidth + 10f, minMaxRect.y, halfWidth, LINE_HEIGHT);
            
            EditorGUI.PropertyField(minRect, minValueProperty, new GUIContent("Min"));
            EditorGUI.PropertyField(maxRect, maxValueProperty, new GUIContent("Max"));
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Allow modifiers toggle
            EditorGUI.PropertyField(currentRect, allowModifiersProperty, new GUIContent("Allow Modifiers"));
            
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var formulaProperty = property.FindPropertyRelative("_formula");
            var hasFormula = formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue);
            
            var lines = 5f; // Label, Name, BaseValue, MinMax, AllowModifiers
            if (hasFormula)
            {
                lines += 3f; // Formula (2 lines) + Type label
            }
            
            return lines * LINE_HEIGHT + (lines - 1) * SPACING;
        }
    }
    
    /// <summary>
    /// Custom editor for MonoBehaviours with Stat fields to provide runtime debugging.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class StatBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (!Application.isPlaying) return;
            
            var monoBehaviour = target as MonoBehaviour;
            if (monoBehaviour == null) return;
            
            // Show runtime stat values
            var statObjects = monoBehaviour.GetAllStatObjects();
            var statList = System.Linq.Enumerable.ToList(statObjects);
            
            if (statList.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Stat Values", EditorStyles.boldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                foreach (var stat in statList)
                {
                    var label = $"{stat.Name}";
                    if (stat.IsDerived)
                        label += " (Derived)";
                    
                    EditorGUILayout.FloatField(label, stat.Value);
                    
                    if (stat.Modifiers.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"Active Modifiers: {stat.Modifiers.Count}", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.EndDisabledGroup();
                
                // Repaint to show live updates
                if (Application.isPlaying)
                {
                    Repaint();
                }
            }
        }
    }
}