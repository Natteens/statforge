#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace StatForge.Editor
{
    /// <summary>
    /// Custom inspector for StatForgeComponent with clean, modern UI
    /// </summary>
    [CustomEditor(typeof(StatForgeComponent))]
    public class StatForgeComponentEditor : UnityEditor.Editor
    {
        private StatForgeComponent component;
        private bool showAttributes = true;
        private bool showAdvanced = false;
        
        private void OnEnable()
        {
            component = target as StatForgeComponent;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawMainSettings();
            DrawAttributesList();
            DrawAdvancedSettings();
            DrawActionButtons();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUILayout.LabelField("StatForge Component", headerStyle);
            EditorGUILayout.LabelField("Ultra-simplified attribute system", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
        }
        
        private void DrawMainSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoDiscoverOnAwake"), 
                new GUIContent("Auto-Discover Stats", "Automatically find [Stat] attributes on Awake"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncWithLegacySystem"), 
                new GUIContent("Legacy System Sync", "Sync with traditional AttributeSystem if present"));
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
        }
        
        private void DrawAttributesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showAttributes = EditorGUILayout.Foldout(showAttributes, "Discovered Attributes", true);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                component.InitializeAttributes();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            if (showAttributes)
            {
                GUILayout.Space(5);
                
                if (Application.isPlaying && component.Attributes != null)
                {
                    var attributeNames = component.Attributes.GetAttributeNames().ToList();
                    
                    if (attributeNames.Any())
                    {
                        foreach (var name in attributeNames)
                        {
                            DrawAttributeField(name);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No attributes found. Add [Stat] to fields in your script.", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Attributes will be shown during play mode after initialization.", MessageType.Info);
                    
                    if (GUILayout.Button("Preview Attributes"))
                    {
                        component.InitializeAttributes();
                        Repaint();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
        }
        
        private void DrawAttributeField(string attributeName)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(attributeName, GUILayout.Width(120));
            
            // Try to display and edit the value
            GUI.enabled = Application.isPlaying;
            
            try
            {
                // Try int first
                var intVal = component.Get<int>(attributeName);
                var newIntVal = EditorGUILayout.IntField(intVal, GUILayout.Width(80));
                if (newIntVal != intVal && Application.isPlaying)
                {
                    component.Set(attributeName, newIntVal);
                }
                
                if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(20)))
                {
                    component.AddModifier(attributeName, 1, 5f);
                }
                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    component.AddModifier(attributeName, -1, 5f);
                }
            }
            catch
            {
                try
                {
                    // Try float
                    var floatVal = component.Get<float>(attributeName);
                    var newFloatVal = EditorGUILayout.FloatField(floatVal, GUILayout.Width(80));
                    if (!Mathf.Approximately(newFloatVal, floatVal) && Application.isPlaying)
                    {
                        component.Set(attributeName, newFloatVal);
                    }
                    
                    if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(20)))
                    {
                        component.AddModifier(attributeName, 1f, 5f);
                    }
                    if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                    {
                        component.AddModifier(attributeName, -1f, 5f);
                    }
                }
                catch
                {
                    EditorGUILayout.LabelField("N/A", GUILayout.Width(80));
                }
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAdvancedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            
            if (showAdvanced)
            {
                EditorGUILayout.HelpBox("Advanced features coming soon:\n• Query system visualization\n• Performance metrics\n• Custom validation rules", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Modern Editor"))
            {
                ModernStatForgeEditor.ShowWindow();
            }
            
            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("https://github.com/Natteens/statforge");
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif