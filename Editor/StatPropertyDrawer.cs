using UnityEngine;
using UnityEditor;
using StatForge;
using System.Linq;

namespace StatForge.Editor
{
    /// <summary>
    /// Enhanced property drawer for the Stat class with dropdown for global definitions.
    /// </summary>
    [CustomPropertyDrawer(typeof(Stat))]
    public class StatPropertyDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float SPACING = 2f;
        private const float BUTTON_WIDTH = 60f;
        
        private static StatDefinition[] _cachedDefinitions;
        private static string[] _definitionNames;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Cache definitions if needed
            RefreshDefinitionCache();
            
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
            
            // Header with main label and definition dropdown
            var headerRect = currentRect;
            var labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width * 0.4f, LINE_HEIGHT);
            var dropdownRect = new Rect(headerRect.x + headerRect.width * 0.4f + 5f, headerRect.y, headerRect.width * 0.45f, LINE_HEIGHT);
            var buttonRect = new Rect(headerRect.x + headerRect.width - BUTTON_WIDTH, headerRect.y, BUTTON_WIDTH, LINE_HEIGHT);
            
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
            
            // Definition dropdown
            var currentDefinitionIndex = GetCurrentDefinitionIndex(definitionGuidProperty?.stringValue, nameProperty?.stringValue);
            var newDefinitionIndex = EditorGUI.Popup(dropdownRect, currentDefinitionIndex, _definitionNames);
            
            if (newDefinitionIndex != currentDefinitionIndex && newDefinitionIndex > 0)
            {
                ApplyDefinitionToProperty(property, _cachedDefinitions[newDefinitionIndex - 1]);
            }
            
            // Quick setup button
            if (GUI.Button(buttonRect, "Setup", EditorStyles.miniButton))
            {
                ShowStatSetupMenu(property);
            }
            
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Indent for sub-properties
            EditorGUI.indentLevel++;
            
            // Name field with validation
            var nameRect = currentRect;
            EditorGUI.PropertyField(nameRect, nameProperty, new GUIContent("Name"));
            if (string.IsNullOrEmpty(nameProperty?.stringValue))
            {
                var warningRect = new Rect(nameRect.x + nameRect.width - 100f, nameRect.y, 100f, LINE_HEIGHT);
                EditorGUI.LabelField(warningRect, "⚠ Name required", EditorStyles.miniLabel);
            }
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
                
                // Validate formula
                var isValid = IndividualStatFormulaEvaluator.ValidateFormula(formulaProperty.stringValue);
                var validationRect = currentRect;
                var statusText = isValid ? "✓ Valid formula" : "✗ Invalid formula";
                var style = isValid ? EditorStyles.miniLabel : EditorStyles.miniLabel;
                EditorGUI.LabelField(validationRect, "Validation", statusText, style);
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
            currentRect.y += LINE_HEIGHT + SPACING;
            
            // Runtime info (if playing)
            if (Application.isPlaying)
            {
                ShowRuntimeInfo(property, currentRect);
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var formulaProperty = property.FindPropertyRelative("_formula");
            var hasFormula = formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue);
            
            var lines = 6f; // Header, Name, BaseValue, MinMax, AllowModifiers, spacing
            if (hasFormula)
            {
                lines += 3f; // Formula (2 lines) + Validation
            }
            
            if (Application.isPlaying)
            {
                lines += 2f; // Runtime info
            }
            
            return lines * LINE_HEIGHT + (lines - 1) * SPACING;
        }
        
        private void RefreshDefinitionCache()
        {
            if (_cachedDefinitions == null)
            {
                _cachedDefinitions = StatDefinition.GetAllDefinitions();
                _definitionNames = new string[_cachedDefinitions.Length + 1];
                _definitionNames[0] = "None (Custom)";
                
                for (int i = 0; i < _cachedDefinitions.Length; i++)
                {
                    _definitionNames[i + 1] = _cachedDefinitions[i].StatName;
                }
            }
        }
        
        private int GetCurrentDefinitionIndex(string guid, string statName)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var def = AssetDatabase.LoadAssetAtPath<StatDefinition>(path);
                    if (def != null)
                    {
                        for (int i = 0; i < _cachedDefinitions.Length; i++)
                        {
                            if (_cachedDefinitions[i] == def)
                                return i + 1;
                        }
                    }
                }
            }
            
            // Try to match by name
            if (!string.IsNullOrEmpty(statName))
            {
                for (int i = 0; i < _cachedDefinitions.Length; i++)
                {
                    if (_cachedDefinitions[i].StatName == statName)
                        return i + 1;
                }
            }
            
            return 0; // None
        }
        
        private void ApplyDefinitionToProperty(SerializedProperty property, StatDefinition definition)
        {
            if (definition == null) return;
            
            var nameProperty = property.FindPropertyRelative("_name");
            var baseValueProperty = property.FindPropertyRelative("_baseValue");
            var formulaProperty = property.FindPropertyRelative("_formula");
            var minValueProperty = property.FindPropertyRelative("_minValue");
            var maxValueProperty = property.FindPropertyRelative("_maxValue");
            var allowModifiersProperty = property.FindPropertyRelative("_allowModifiers");
            var definitionGuidProperty = property.FindPropertyRelative("_definitionGuid");
            
            nameProperty.stringValue = definition.StatName;
            baseValueProperty.floatValue = definition.DefaultBaseValue;
            formulaProperty.stringValue = definition.DefaultFormula;
            minValueProperty.floatValue = definition.MinValue;
            maxValueProperty.floatValue = definition.MaxValue;
            allowModifiersProperty.boolValue = definition.AllowModifiers;
            
            var path = AssetDatabase.GetAssetPath(definition);
            definitionGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(path);
            
            property.serializedObject.ApplyModifiedProperties();
        }
        
        private void ShowStatSetupMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Create from Template/Health"), false, () => CreateFromTemplate(property, "Health", 100f, 0f, 1000f));
            menu.AddItem(new GUIContent("Create from Template/Mana"), false, () => CreateFromTemplate(property, "Mana", 50f, 0f, 500f));
            menu.AddItem(new GUIContent("Create from Template/Damage"), false, () => CreateFromTemplate(property, "Damage", 10f, 0f, 999f));
            menu.AddItem(new GUIContent("Create from Template/Level"), false, () => CreateFromTemplate(property, "Level", 1f, 1f, 100f));
            menu.AddItem(new GUIContent("Create from Template/Percentage"), false, () => CreateFromTemplate(property, "Percentage", 0f, 0f, 100f));
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear All Values"), false, () => ClearAllValues(property));
            menu.AddItem(new GUIContent("Validate Formula"), false, () => ValidateFormula(property));
            
            if (_cachedDefinitions.Length > 0)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Open StatDefinition Editor"), false, () => StatForgeWindow.ShowWindow());
            }
            
            menu.ShowAsContext();
        }
        
        private void CreateFromTemplate(SerializedProperty property, string name, float baseValue, float min, float max)
        {
            var nameProperty = property.FindPropertyRelative("_name");
            var baseValueProperty = property.FindPropertyRelative("_baseValue");
            var minValueProperty = property.FindPropertyRelative("_minValue");
            var maxValueProperty = property.FindPropertyRelative("_maxValue");
            var allowModifiersProperty = property.FindPropertyRelative("_allowModifiers");
            
            nameProperty.stringValue = name;
            baseValueProperty.floatValue = baseValue;
            minValueProperty.floatValue = min;
            maxValueProperty.floatValue = max;
            allowModifiersProperty.boolValue = true;
            
            property.serializedObject.ApplyModifiedProperties();
        }
        
        private void ClearAllValues(SerializedProperty property)
        {
            var nameProperty = property.FindPropertyRelative("_name");
            var baseValueProperty = property.FindPropertyRelative("_baseValue");
            var formulaProperty = property.FindPropertyRelative("_formula");
            var minValueProperty = property.FindPropertyRelative("_minValue");
            var maxValueProperty = property.FindPropertyRelative("_maxValue");
            var definitionGuidProperty = property.FindPropertyRelative("_definitionGuid");
            
            nameProperty.stringValue = "";
            baseValueProperty.floatValue = 0f;
            formulaProperty.stringValue = "";
            minValueProperty.floatValue = 0f;
            maxValueProperty.floatValue = float.MaxValue;
            definitionGuidProperty.stringValue = "";
            
            property.serializedObject.ApplyModifiedProperties();
        }
        
        private void ValidateFormula(SerializedProperty property)
        {
            var formulaProperty = property.FindPropertyRelative("_formula");
            if (formulaProperty != null && !string.IsNullOrEmpty(formulaProperty.stringValue))
            {
                var isValid = IndividualStatFormulaEvaluator.ValidateFormula(formulaProperty.stringValue);
                EditorUtility.DisplayDialog("Formula Validation", 
                    isValid ? "✓ Formula is valid!" : "✗ Formula has syntax errors!", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Formula Validation", "No formula to validate.", "OK");
            }
        }
        
        private void ShowRuntimeInfo(SerializedProperty property, Rect rect)
        {
            var target = property.serializedObject.targetObject;
            if (target == null) return;
            
            var nameProperty = property.FindPropertyRelative("_name");
            var statName = nameProperty?.stringValue;
            if (string.IsNullOrEmpty(statName)) return;
            
            // Try to get the actual Stat object at runtime
            var fieldInfo = property.GetFieldInfo();
            if (fieldInfo != null)
            {
                var stat = fieldInfo.GetValue(target) as Stat;
                if (stat != null)
                {
                    EditorGUI.LabelField(rect, "Runtime Value", stat.Value.ToString("F2"), EditorStyles.miniLabel);
                    rect.y += LINE_HEIGHT;
                    
                    if (stat.Modifiers.Count > 0)
                    {
                        EditorGUI.LabelField(rect, "Active Modifiers", stat.Modifiers.Count.ToString(), EditorStyles.miniLabel);
                    }
                }
            }
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
    
    /// <summary>
    /// Extension methods for SerializedProperty to help with reflection.
    /// </summary>
    public static class SerializedPropertyExtensions
    {
        public static System.Reflection.FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            var fieldName = property.propertyPath;
            
            // Handle array elements
            if (fieldName.Contains("["))
            {
                fieldName = fieldName.Substring(0, fieldName.IndexOf('['));
            }
            
            return targetType.GetField(fieldName, 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
        }
    }
}