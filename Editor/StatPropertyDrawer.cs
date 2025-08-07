using UnityEngine;
using UnityEditor;
using StatForge;
using System.Linq;

namespace StatForge.Editor
{
    /// <summary>
    /// Enhanced property drawer for the Stat class with StatDefinition support and intelligent features.
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
            var definitionGuidProperty = property.FindPropertyRelative("_definitionGuid");
            
            var currentRect = position;
            currentRect.height = LINE_HEIGHT;
            
            // Main label with foldout
            var isExpanded = property.isExpanded;
            property.isExpanded = EditorGUI.Foldout(currentRect, isExpanded, label, true);
            
            if (!property.isExpanded)
            {
                // Show just the name and value on one line when collapsed
                var valueRect = new Rect(currentRect.x + EditorGUIUtility.labelWidth, currentRect.y, 
                    currentRect.width - EditorGUIUtility.labelWidth - 100f, currentRect.height);
                var nameRect = new Rect(valueRect.xMax + 5f, currentRect.y, 95f, currentRect.height);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.LabelField(valueRect, GetStatDisplayValue(property), EditorStyles.miniLabel);
                EditorGUI.LabelField(nameRect, nameProperty.stringValue, EditorStyles.miniLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.EndProperty();
                return;
            }
            
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Indent for sub-properties
            EditorGUI.indentLevel++;
            
            // StatDefinition dropdown
            DrawStatDefinitionDropdown(currentRect, definitionGuidProperty, nameProperty, property);
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Name field (editable if no definition selected)
            var hasDefinition = !string.IsNullOrEmpty(definitionGuidProperty.stringValue);
            EditorGUI.BeginDisabledGroup(hasDefinition);
            EditorGUI.PropertyField(currentRect, nameProperty, new GUIContent("Name"));
            EditorGUI.EndDisabledGroup();
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Base Value field
            EditorGUI.PropertyField(currentRect, baseValueProperty, new GUIContent("Base Value"));
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Formula field (if present or if editing)
            var hasFormula = formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue);
            if (hasFormula || !hasDefinition)
            {
                EditorGUI.BeginDisabledGroup(hasDefinition);
                var formulaRect = currentRect;
                formulaRect.height = LINE_HEIGHT;
                EditorGUI.PropertyField(formulaRect, formulaProperty, new GUIContent("Formula"));
                currentRect.y += LINE_HEIGHT + SPACING;
                EditorGUI.EndDisabledGroup();
                
                if (hasFormula)
                {
                    // Show formula validation
                    DrawFormulaValidation(currentRect, formulaProperty.stringValue);
                    currentRect.y += LINE_HEIGHT + SPACING;
                }
            }
            
            // Min/Max values in a single line
            EditorGUI.BeginDisabledGroup(hasDefinition);
            var minMaxRect = currentRect;
            var halfWidth = minMaxRect.width / 2f - 5f;
            
            var minRect = new Rect(minMaxRect.x, minMaxRect.y, halfWidth, LINE_HEIGHT);
            var maxRect = new Rect(minMaxRect.x + halfWidth + 10f, minMaxRect.y, halfWidth, LINE_HEIGHT);
            
            EditorGUI.PropertyField(minRect, minValueProperty, new GUIContent("Min"));
            EditorGUI.PropertyField(maxRect, maxValueProperty, new GUIContent("Max"));
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Allow modifiers toggle
            EditorGUI.PropertyField(currentRect, allowModifiersProperty, new GUIContent("Allow Modifiers"));
            EditorGUI.EndDisabledGroup();
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Runtime info (if playing)
            if (Application.isPlaying)
            {
                DrawRuntimeInfo(currentRect, property);
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
        
        private void DrawStatDefinitionDropdown(Rect position, SerializedProperty guidProperty, 
            SerializedProperty nameProperty, SerializedProperty mainProperty)
        {
            var definitions = StatDefinition.GetAllDefinitions();
            var options = new string[definitions.Length + 1];
            var guids = new string[definitions.Length + 1];
            
            options[0] = "None (Custom)";
            guids[0] = "";
            
            for (int i = 0; i < definitions.Length; i++)
            {
                options[i + 1] = $"{definitions[i].StatName} ({definitions[i].Category})";
                guids[i + 1] = definitions[i].Guid;
            }
            
            var currentIndex = 0;
            var currentGuid = guidProperty.stringValue;
            for (int i = 0; i < guids.Length; i++)
            {
                if (guids[i] == currentGuid)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUI.Popup(position, "Definition", currentIndex, options);
            
            if (EditorGUI.EndChangeCheck())
            {
                guidProperty.stringValue = guids[newIndex];
                
                // Apply definition if selected
                if (newIndex > 0)
                {
                    var definition = definitions[newIndex - 1];
                    nameProperty.stringValue = definition.StatName;
                    
                    var baseValueProp = mainProperty.FindPropertyRelative("_baseValue");
                    var formulaProp = mainProperty.FindPropertyRelative("_formula");
                    var minValueProp = mainProperty.FindPropertyRelative("_minValue");
                    var maxValueProp = mainProperty.FindPropertyRelative("_maxValue");
                    var allowModsProp = mainProperty.FindPropertyRelative("_allowModifiers");
                    
                    if (baseValueProp.floatValue == 0f)
                        baseValueProp.floatValue = definition.DefaultBaseValue;
                    formulaProp.stringValue = definition.DefaultFormula;
                    minValueProp.floatValue = definition.MinValue;
                    maxValueProp.floatValue = definition.MaxValue;
                    allowModsProp.boolValue = definition.AllowModifiers;
                }
            }
        }
        
        private void DrawFormulaValidation(Rect position, string formula)
        {
            if (string.IsNullOrEmpty(formula)) return;
            
            var isValid = IndividualStatFormulaEvaluator.ValidateFormula(formula);
            var content = isValid ? 
                new GUIContent("✓ Formula Valid", EditorGUIUtility.IconContent("TestPassed").image) :
                new GUIContent("✗ Formula Invalid", EditorGUIUtility.IconContent("TestFailed").image);
            
            var style = isValid ? EditorStyles.miniLabel : 
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } };
            
            EditorGUI.LabelField(position, content, style);
        }
        
        private void DrawRuntimeInfo(Rect position, SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var fieldInfo = fieldInfo = this.fieldInfo;
            
            if (fieldInfo != null && target != null)
            {
                var stat = fieldInfo.GetValue(target) as Stat;
                if (stat != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    
                    // Current value
                    EditorGUI.FloatField(position, "Runtime Value", stat.Value);
                    position.y += LINE_HEIGHT + SPACING;
                    
                    // Modifier count
                    if (stat.Modifiers.Count > 0)
                    {
                        EditorGUI.LabelField(position, "Active Modifiers", stat.Modifiers.Count.ToString());
                        position.y += LINE_HEIGHT + SPACING;
                        
                        // Show some modifier details
                        foreach (var modifier in stat.Modifiers.Take(3))
                        {
                            var modText = $"{modifier.Type}: {modifier.Value:F2}";
                            if (modifier.HasDuration)
                                modText += $" ({modifier.RemainingDuration:F1}s)";
                            
                            EditorGUI.LabelField(position, "  " + modText, EditorStyles.miniLabel);
                            position.y += LINE_HEIGHT + SPACING;
                        }
                        
                        if (stat.Modifiers.Count > 3)
                        {
                            EditorGUI.LabelField(position, $"  ... and {stat.Modifiers.Count - 3} more", EditorStyles.miniLabel);
                        }
                    }
                    
                    EditorGUI.EndDisabledGroup();
                }
            }
        }
        
        private string GetStatDisplayValue(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var fieldInfo = this.fieldInfo;
            
            if (fieldInfo != null && target != null && Application.isPlaying)
            {
                var stat = fieldInfo.GetValue(target) as Stat;
                if (stat != null)
                {
                    return stat.Value.ToString("F1");
                }
            }
            
            var baseValueProp = property.FindPropertyRelative("_baseValue");
            return baseValueProp != null ? baseValueProp.floatValue.ToString("F1") : "0";
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return LINE_HEIGHT;
            
            var formulaProperty = property.FindPropertyRelative("_formula");
            var hasFormula = formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue);
            var hasDefinition = !string.IsNullOrEmpty(property.FindPropertyRelative("_definitionGuid").stringValue);
            
            var lines = 6f; // Foldout, Definition, Name, BaseValue, MinMax, AllowModifiers
            
            if (hasFormula || !hasDefinition)
            {
                lines += 1f; // Formula field
                if (hasFormula)
                    lines += 1f; // Validation
            }
            
            if (Application.isPlaying)
            {
                var target = property.serializedObject.targetObject;
                var fieldInfo = this.fieldInfo;
                
                if (fieldInfo != null && target != null)
                {
                    var stat = fieldInfo.GetValue(target) as Stat;
                    if (stat != null)
                    {
                        lines += 1f; // Runtime value
                        if (stat.Modifiers.Count > 0)
                        {
                            lines += 1f + Mathf.Min(stat.Modifiers.Count, 3); // Modifier count + details
                            if (stat.Modifiers.Count > 3)
                                lines += 1f; // "... and more"
                        }
                    }
                }
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