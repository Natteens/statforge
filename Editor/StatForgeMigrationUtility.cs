#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeMigrationUtility : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<StatType> foundStatTypes;
        private List<StatContainer> foundContainers;
        private List<MonoBehaviour> foundComponents;
        private bool scanCompleted = false;
        
        [MenuItem("Tools/StatForge/Migration Utility")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeMigrationUtility>("StatForge Migration");
            window.minSize = new Vector2(600f, 400f);
            window.Show();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            
            GUILayout.Space(10);
            
            DrawScanSection();
            
            if (scanCompleted)
            {
                GUILayout.Space(10);
                DrawMigrationOptions();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("StatForge Migration Utility", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "This utility helps you migrate from the old StatForge system to the new simplified API. " +
                "It can scan your project for existing StatTypes and suggest migration paths.",
                MessageType.Info);
        }
        
        private void DrawScanSection()
        {
            EditorGUILayout.LabelField("1. Scan Project", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan for StatTypes", GUILayout.Height(30)))
            {
                ScanProject();
            }
            
            if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
            {
                ClearResults();
            }
            EditorGUILayout.EndHorizontal();
            
            if (scanCompleted)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Scan Results:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"StatTypes found: {foundStatTypes?.Count ?? 0}");
                EditorGUILayout.LabelField($"StatContainers found: {foundContainers?.Count ?? 0}");
                EditorGUILayout.LabelField($"Components using stats: {foundComponents?.Count ?? 0}");
            }
        }
        
        private void DrawMigrationOptions()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("2. Migration Options", EditorStyles.boldLabel);
            
            if (foundStatTypes != null && foundStatTypes.Count > 0)
            {
                DrawStatTypeMigration();
            }
            
            GUILayout.Space(10);
            
            if (foundContainers != null && foundContainers.Count > 0)
            {
                DrawContainerMigration();
            }
            
            GUILayout.Space(10);
            
            DrawAdvancedOptions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawStatTypeMigration()
        {
            EditorGUILayout.LabelField("StatType Migration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Convert StatTypes to StatDefinitions for enhanced features (colors, icons, better organization).",
                MessageType.Info);
            
            foreach (var statType in foundStatTypes)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(statType.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Category: {statType.Category}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Convert to StatDefinition", GUILayout.Width(150)))
                {
                    ConvertStatTypeToDefinition(statType);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert All StatTypes"))
            {
                if (EditorUtility.DisplayDialog("Convert All StatTypes", 
                    $"This will create StatDefinitions for all {foundStatTypes.Count} StatTypes. Continue?", 
                    "Convert", "Cancel"))
                {
                    ConvertAllStatTypes();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawContainerMigration()
        {
            EditorGUILayout.LabelField("StatContainer Information", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "StatContainers are still supported and work with both StatTypes and StatDefinitions. " +
                "No migration needed, but you can review them here.",
                MessageType.Info);
            
            foreach (var container in foundContainers)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(container.ContainerName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Stats: {container.Stats?.Count ?? 0}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Open in Editor", GUILayout.Width(100)))
                {
                    Selection.activeObject = container;
                    StatForgeWindow.ShowWindow();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawAdvancedOptions()
        {
            EditorGUILayout.LabelField("3. Advanced Migration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Component Migration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The new Stat class can be used directly in MonoBehaviours without containers. " +
                "Components using AttributeSystem can continue to work as before.",
                MessageType.Info);
            
            if (GUILayout.Button("Show Migration Guide"))
            {
                ShowMigrationGuide();
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Cleanup Options", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Backup of Current Setup"))
            {
                CreateBackup();
            }
            
            if (GUILayout.Button("Validate Project Integrity"))
            {
                ValidateProjectIntegrity();
            }
            EditorGUILayout.EndVertical();
        }
        
        private void ScanProject()
        {
            foundStatTypes = FindAssets<StatType>();
            foundContainers = FindAssets<StatContainer>();
            foundComponents = FindComponentsUsingStats();
            scanCompleted = true;
            
            Debug.Log($"StatForge Migration Scan Complete:");
            Debug.Log($"  - StatTypes: {foundStatTypes.Count}");
            Debug.Log($"  - StatContainers: {foundContainers.Count}");
            Debug.Log($"  - Components: {foundComponents.Count}");
        }
        
        private List<T> FindAssets<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Select(guid => 
                AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();
        }
        
        private List<MonoBehaviour> FindComponentsUsingStats()
        {
            var components = new List<MonoBehaviour>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var attributeSystems = prefab.GetComponentsInChildren<AttributeSystem>();
                    components.AddRange(attributeSystems);
                }
            }
            
            return components;
        }
        
        private void ConvertStatTypeToDefinition(StatType statType)
        {
            var definition = CreateInstance<StatDefinition>();
            definition.DisplayName = statType.DisplayName;
            definition.ShortName = statType.ShortName;
            definition.Abbreviation = statType.Abbreviation;
            definition.Category = statType.Category;
            definition.DefaultValue = statType.DefaultValue;
            definition.MinValue = statType.MinValue;
            definition.MaxValue = statType.MaxValue;
            definition.Formula = statType.Formula;
            
            // Set default color based on category
            definition.StatColor = GetDefaultColorForCategory(statType.Category);
            
            var path = AssetDatabase.GetAssetPath(statType);
            var newPath = path.Replace(".asset", "_Definition.asset");
            
            AssetDatabase.CreateAsset(definition, newPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Converted {statType.DisplayName} to StatDefinition: {newPath}");
        }
        
        private void ConvertAllStatTypes()
        {
            foreach (var statType in foundStatTypes)
            {
                ConvertStatTypeToDefinition(statType);
            }
            
            AssetDatabase.Refresh();
            ScanProject(); // Refresh the scan
        }
        
        private Color GetDefaultColorForCategory(StatCategory category)
        {
            return category switch
            {
                StatCategory.Primary => new Color(0.6f, 0.8f, 1f),
                StatCategory.Derived => new Color(1f, 0.8f, 0.6f),
                StatCategory.External => new Color(0.8f, 1f, 0.6f),
                _ => Color.white
            };
        }
        
        private void ShowMigrationGuide()
        {
            var guide = @"StatForge Migration Guide

OLD SYSTEM:
public class PlayerController : MonoBehaviour 
{
    public AttributeSystem attributeSystem;
    
    void Start() 
    {
        attributeSystem.SetAvailablePoints(10);
        var health = attributeSystem.GetStatValue(healthStatType);
    }
}

NEW SIMPLIFIED SYSTEM:
public class PlayerController : MonoBehaviour 
{
    [SerializeField] private Stat health;
    [SerializeField] private Stat mana;
    
    void Start() 
    {
        health.Value = 100f; // Direct access!
        health.OnValueChanged += OnHealthChanged;
    }
    
    void OnHealthChanged(Stat stat) 
    {
        Debug.Log($'Health: {stat.Value}');
    }
}

BENEFITS:
• No initialization required
• Direct property access
• Better inspector integration
• Enhanced event system
• Modifier system with durations
• Extension methods for common operations

The old AttributeSystem still works for complex scenarios!";

            EditorUtility.DisplayDialog("Migration Guide", guide, "OK");
        }
        
        private void CreateBackup()
        {
            var backupPath = $"Assets/StatForge_Backup_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(backupPath));
            
            // Copy all StatTypes
            foreach (var statType in foundStatTypes)
            {
                var originalPath = AssetDatabase.GetAssetPath(statType);
                var backupFile = $"{backupPath}/{statType.name}.asset";
                AssetDatabase.CopyAsset(originalPath, backupFile);
            }
            
            // Copy all StatContainers
            foreach (var container in foundContainers)
            {
                var originalPath = AssetDatabase.GetAssetPath(container);
                var backupFile = $"{backupPath}/{container.name}.asset";
                AssetDatabase.CopyAsset(originalPath, backupFile);
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Backup created at: {backupPath}");
            EditorUtility.DisplayDialog("Backup Complete", $"Backup created at:\n{backupPath}", "OK");
        }
        
        private void ValidateProjectIntegrity()
        {
            var issues = new List<string>();
            
            // Check for missing references
            foreach (var container in foundContainers)
            {
                foreach (var stat in container.Stats)
                {
                    if (stat.statType == null)
                    {
                        issues.Add($"StatContainer '{container.name}' has null StatType reference");
                    }
                }
            }
            
            // Check for duplicate names
            var statNames = foundStatTypes.Select(s => s.DisplayName).ToList();
            var duplicates = statNames.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicates)
            {
                issues.Add($"Duplicate stat name found: '{duplicate}'");
            }
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "No issues found! Project integrity is good.", "OK");
            }
            else
            {
                var issueText = string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Issues", $"Found {issues.Count} issues:\n\n{issueText}", "OK");
            }
        }
        
        private void ClearResults()
        {
            foundStatTypes = null;
            foundContainers = null;
            foundComponents = null;
            scanCompleted = false;
        }
    }
}
#endif