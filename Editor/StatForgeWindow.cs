#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeWindow : EditorWindow
    {
        private enum ViewMode { StatDefinitions, StatTypes, Containers, Overview }
        
        private ViewMode currentView = ViewMode.StatDefinitions;
        private List<StatDefinition> allStatDefinitions;
        private List<StatType> allStatTypes;
        private List<StatContainer> allContainers;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private string searchFilter = "";
        
        // Selection
        private StatDefinition selectedStatDefinition;
        private StatType selectedStatType;
        private StatContainer selectedContainer;
        
        // UI Colors
        private static readonly Color HeaderColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color PrimaryColor = new Color(0.6f, 0.8f, 1f);
        private static readonly Color DerivedColor = new Color(1f, 0.8f, 0.6f);
        private static readonly Color ExternalColor = new Color(0.8f, 1f, 0.6f);
        
        [MenuItem("Tools/StatForge/StatForge Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeWindow>("StatForge");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshData();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            var rect = new Rect(0, 30, position.width, position.height - 30);
            var leftRect = new Rect(rect.x, rect.y, 300, rect.height);
            var rightRect = new Rect(leftRect.xMax + 5, rect.y, rect.width - leftRect.width - 5, rect.height);
            
            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var originalColor = GUI.backgroundColor;
            
            // View mode buttons with colors
            GUI.backgroundColor = currentView == ViewMode.StatDefinitions ? PrimaryColor : originalColor;
            if (GUILayout.Toggle(currentView == ViewMode.StatDefinitions, "Definitions", EditorStyles.toolbarButton))
                currentView = ViewMode.StatDefinitions;
            
            GUI.backgroundColor = currentView == ViewMode.StatTypes ? DerivedColor : originalColor;
            if (GUILayout.Toggle(currentView == ViewMode.StatTypes, "Legacy Types", EditorStyles.toolbarButton))
                currentView = ViewMode.StatTypes;
            
            GUI.backgroundColor = currentView == ViewMode.Containers ? ExternalColor : originalColor;
            if (GUILayout.Toggle(currentView == ViewMode.Containers, "Containers", EditorStyles.toolbarButton))
                currentView = ViewMode.Containers;
            
            GUI.backgroundColor = currentView == ViewMode.Overview ? HeaderColor : originalColor;
            if (GUILayout.Toggle(currentView == ViewMode.Overview, "Overview", EditorStyles.toolbarButton))
                currentView = ViewMode.Overview;
            
            GUI.backgroundColor = originalColor;
            
            GUILayout.Space(20);
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                RefreshData();
                
            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
                ShowHelp();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            var title = GetCurrentViewTitle();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (currentView != ViewMode.Overview && GUILayout.Button("+", GUILayout.Width(25)))
                CreateNew();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // List
            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);
            
            switch (currentView)
            {
                case ViewMode.StatDefinitions:
                    DrawStatDefinitionsList();
                    break;
                case ViewMode.StatTypes:
                    DrawStatTypesList();
                    break;
                case ViewMode.Containers:
                    DrawContainersList();
                    break;
                case ViewMode.Overview:
                    DrawOverviewList();
                    break;
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private string GetCurrentViewTitle()
        {
            return currentView switch
            {
                ViewMode.StatDefinitions => "Stat Definitions",
                ViewMode.StatTypes => "Legacy Stat Types",
                ViewMode.Containers => "Stat Containers",
                ViewMode.Overview => "Project Overview",
                _ => "Unknown"
            };
        }
        
        private void DrawStatDefinitionsList()
        {
            if (allStatDefinitions == null) return;
            
            var filtered = allStatDefinitions.Where(s => string.IsNullOrEmpty(searchFilter) || 
                s.DisplayName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var statDef in filtered)
            {
                var isSelected = selectedStatDefinition == statDef;
                DrawStatItem(statDef.DisplayName, statDef.ShortName, statDef.Abbreviation, statDef.Category, statDef.StatColor, isSelected, () =>
                {
                    selectedStatDefinition = statDef;
                    selectedStatType = null;
                    selectedContainer = null;
                    Repaint();
                });
            }
        }
        
        private void DrawStatTypesList()
        {
            if (allStatTypes == null) return;
            
            var filtered = allStatTypes.Where(s => string.IsNullOrEmpty(searchFilter) || 
                s.DisplayName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var statType in filtered)
            {
                var isSelected = selectedStatType == statType;
                DrawStatItem(statType.DisplayName, statType.ShortName, statType.Abbreviation, statType.Category, Color.white, isSelected, () =>
                {
                    selectedStatType = statType;
                    selectedStatDefinition = null;
                    selectedContainer = null;
                    Repaint();
                });
            }
        }
        
        private void DrawStatItem(string displayName, string shortName, string abbreviation, StatCategory category, Color color, bool isSelected, Action onSelect)
        {
            var originalColor = GUI.backgroundColor;
            
            if (isSelected)
                GUI.backgroundColor = GetCategoryColor(category);
            else
                GUI.backgroundColor = color * 0.8f + Color.white * 0.2f;
                
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            // Color indicator
            var colorRect = GUILayoutUtility.GetRect(10, 10, GUILayout.Width(10));
            EditorGUI.DrawRect(colorRect, color);
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label(displayName, EditorStyles.boldLabel);
            var infoText = $"({shortName})";
            if (!string.IsNullOrEmpty(abbreviation))
                infoText += $" [{abbreviation}]";
            infoText += $" - {category}";
            GUILayout.Label(infoText, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            if (Event.current.type == EventType.MouseDown && 
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                onSelect?.Invoke();
                Event.current.Use();
            }
            
            GUI.backgroundColor = originalColor;
        }
        
        private Color GetCategoryColor(StatCategory category)
        {
            return category switch
            {
                StatCategory.Primary => PrimaryColor,
                StatCategory.Derived => DerivedColor,
                StatCategory.External => ExternalColor,
                _ => Color.white
            };
        }
        
        private void DrawContainersList()
        {
            if (allContainers == null) return;
            
            var filtered = allContainers.Where(c => string.IsNullOrEmpty(searchFilter) || 
                c.ContainerName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var container in filtered)
            {
                var isSelected = selectedContainer == container;
                var style = isSelected ? "selectionRect" : "box";
                
                EditorGUILayout.BeginVertical(style);
                
                GUILayout.Label(container.ContainerName, EditorStyles.boldLabel);
                GUILayout.Label($"{container.Stats?.Count ?? 0} stats - {container.Category}", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedContainer = container;
                    selectedStatDefinition = null;
                    selectedStatType = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawOverviewList()
        {
            EditorGUILayout.LabelField("Project Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"Stat Definitions: {allStatDefinitions?.Count ?? 0}");
            EditorGUILayout.LabelField($"Legacy Stat Types: {allStatTypes?.Count ?? 0}");
            EditorGUILayout.LabelField($"Stat Containers: {allContainers?.Count ?? 0}");
            
            GUILayout.Space(20);
            
            if (allStatDefinitions != null && allStatDefinitions.Count > 0)
            {
                EditorGUILayout.LabelField("Categories Breakdown:", EditorStyles.boldLabel);
                var primary = allStatDefinitions.Count(s => s.Category == StatCategory.Primary);
                var derived = allStatDefinitions.Count(s => s.Category == StatCategory.Derived);
                var external = allStatDefinitions.Count(s => s.Category == StatCategory.External);
                
                EditorGUILayout.LabelField($"  Primary: {primary}");
                EditorGUILayout.LabelField($"  Derived: {derived}");
                EditorGUILayout.LabelField($"  External: {external}");
            }
        }
        
        private void DrawRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            if (selectedStatDefinition != null)
                DrawStatDefinitionEditor();
            else if (selectedStatType != null)
                DrawStatTypeEditor();
            else if (selectedContainer != null)
                DrawContainerEditor();
            else
                DrawWelcome();
            
            GUILayout.EndArea();
        }
        
        private void DrawStatDefinitionEditor()
        {
            DrawStatDefinitionHeader();
            
            GUILayout.Space(10);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            // Basic fields
            selectedStatDefinition.DisplayName = EditorGUILayout.TextField("Display Name", selectedStatDefinition.DisplayName);
            selectedStatDefinition.ShortName = EditorGUILayout.TextField("Short Name", selectedStatDefinition.ShortName);
            selectedStatDefinition.Abbreviation = EditorGUILayout.TextField("Abbreviation", selectedStatDefinition.Abbreviation);
            selectedStatDefinition.Category = (StatCategory)EditorGUILayout.EnumPopup("Category", selectedStatDefinition.Category);
            
            GUILayout.Space(10);
            
            // Visual fields
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            selectedStatDefinition.StatColor = EditorGUILayout.ColorField("Color", selectedStatDefinition.StatColor);
            selectedStatDefinition.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedStatDefinition.Icon, typeof(Sprite), false);
            
            GUILayout.Space(10);
            
            // Value fields
            EditorGUILayout.LabelField("Value Configuration", EditorStyles.boldLabel);
            selectedStatDefinition.DefaultValue = EditorGUILayout.FloatField("Default Value", selectedStatDefinition.DefaultValue);
            selectedStatDefinition.MinValue = EditorGUILayout.FloatField("Min Value", selectedStatDefinition.MinValue);
            selectedStatDefinition.MaxValue = EditorGUILayout.FloatField("Max Value", selectedStatDefinition.MaxValue);
            
            if (selectedStatDefinition.Category == StatCategory.Derived)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
                selectedStatDefinition.Formula = EditorGUILayout.TextArea(selectedStatDefinition.Formula, GUILayout.Height(80));
                
                if (GUILayout.Button("Validate Formula"))
                {
                    // TODO: Add formula validation
                    EditorUtility.DisplayDialog("Formula Validation", "Formula validation not yet implemented.", "OK");
                }
            }
            
            GUILayout.EndScrollView();
            
            if (GUI.changed)
                EditorUtility.SetDirty(selectedStatDefinition);
        }
        
        private void DrawStatDefinitionHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedStatDefinition.DisplayName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Convert to StatType button
            if (GUILayout.Button("Convert to StatType", GUILayout.Width(120)))
            {
                ConvertToStatType(selectedStatDefinition);
            }
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Stat Definition", $"Delete '{selectedStatDefinition.DisplayName}'?", "Delete", "Cancel"))
                {
                    DeleteStatDefinition(selectedStatDefinition);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatTypeEditor()
        {
            // Similar to existing StatForgeEditor implementation but enhanced
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedStatType.DisplayName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Convert to StatDefinition button
            if (GUILayout.Button("Convert to Definition", GUILayout.Width(120)))
            {
                ConvertToStatDefinition(selectedStatType);
            }
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Stat Type", $"Delete '{selectedStatType.DisplayName}'?", "Delete", "Cancel"))
                {
                    DeleteStatType(selectedStatType);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            // Fields
            selectedStatType.DisplayName = EditorGUILayout.TextField("Display Name", selectedStatType.DisplayName);
            selectedStatType.ShortName = EditorGUILayout.TextField("Short Name", selectedStatType.ShortName);
            selectedStatType.Abbreviation = EditorGUILayout.TextField("Abbreviation", selectedStatType.Abbreviation);
            selectedStatType.Category = (StatCategory)EditorGUILayout.EnumPopup("Category", selectedStatType.Category);
            selectedStatType.DefaultValue = EditorGUILayout.FloatField("Default Value", selectedStatType.DefaultValue);
            selectedStatType.MinValue = EditorGUILayout.FloatField("Min Value", selectedStatType.MinValue);
            selectedStatType.MaxValue = EditorGUILayout.FloatField("Max Value", selectedStatType.MaxValue);
            
            if (selectedStatType.Category == StatCategory.Derived)
            {
                GUILayout.Label("Formula", EditorStyles.boldLabel);
                selectedStatType.Formula = EditorGUILayout.TextArea(selectedStatType.Formula, GUILayout.Height(60));
            }
            
            GUILayout.EndScrollView();
            
            if (GUI.changed)
                EditorUtility.SetDirty(selectedStatType);
        }
        
        private void DrawContainerEditor()
        {
            // Use existing implementation from StatForgeEditor
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedContainer.ContainerName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Container", $"Delete '{selectedContainer.ContainerName}'?", "Delete", "Cancel"))
                {
                    DeleteContainer(selectedContainer);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            selectedContainer.ContainerName = EditorGUILayout.TextField("Name", selectedContainer.ContainerName);
            selectedContainer.Category = (ContainerCategory)EditorGUILayout.EnumPopup("Category", selectedContainer.Category);
            
            GUILayout.Label("Description", EditorStyles.boldLabel);
            selectedContainer.Description = EditorGUILayout.TextArea(selectedContainer.Description, GUILayout.Height(40));
            
            GUILayout.EndScrollView();
            
            if (GUI.changed)
                EditorUtility.SetDirty(selectedContainer);
        }
        
        private void DrawWelcome()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("StatForge Enhanced Editor", EditorStyles.largeLabel);
            GUILayout.Label("Select an item from the left panel to edit", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox("StatDefinitions are the new recommended way to define stats. They include additional features like colors and icons.", MessageType.Info);
            
            GUILayout.FlexibleSpace();
        }
        
        private void CreateNew()
        {
            switch (currentView)
            {
                case ViewMode.StatDefinitions:
                    CreateStatDefinition();
                    break;
                case ViewMode.StatTypes:
                    CreateStatType();
                    break;
                case ViewMode.Containers:
                    CreateContainer();
                    break;
            }
        }
        
        private void CreateStatDefinition()
        {
            var asset = CreateInstance<StatDefinition>();
            asset.DisplayName = "New Stat Definition";
            asset.ShortName = "NSD";
            asset.Abbreviation = "NSD";
            
            var path = EditorUtility.SaveFilePanel("Create Stat Definition", "Assets", "NewStatDefinition", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                RefreshData();
                selectedStatDefinition = asset;
            }
            else
            {
                DestroyImmediate(asset);
            }
        }
        
        private void CreateStatType()
        {
            var asset = CreateInstance<StatType>();
            asset.DisplayName = "New Stat Type";
            asset.ShortName = "NST";
            
            var path = EditorUtility.SaveFilePanel("Create Stat Type", "Assets", "NewStatType", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                RefreshData();
                selectedStatType = asset;
            }
            else
            {
                DestroyImmediate(asset);
            }
        }
        
        private void CreateContainer()
        {
            var asset = CreateInstance<StatContainer>();
            asset.ContainerName = "New Container";
            asset.Stats = new List<StatValue>();
            
            var path = EditorUtility.SaveFilePanel("Create Container", "Assets", "NewContainer", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                RefreshData();
                selectedContainer = asset;
            }
            else
            {
                DestroyImmediate(asset);
            }
        }
        
        private void ConvertToStatDefinition(StatType statType)
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
            
            var path = AssetDatabase.GetAssetPath(statType);
            var newPath = path.Replace(".asset", "_Definition.asset");
            
            AssetDatabase.CreateAsset(definition, newPath);
            AssetDatabase.SaveAssets();
            RefreshData();
            selectedStatDefinition = definition;
            selectedStatType = null;
        }
        
        private void ConvertToStatType(StatDefinition statDefinition)
        {
            var statType = CreateInstance<StatType>();
            statType.DisplayName = statDefinition.DisplayName;
            statType.ShortName = statDefinition.ShortName;
            statType.Abbreviation = statDefinition.Abbreviation;
            statType.Category = statDefinition.Category;
            statType.DefaultValue = statDefinition.DefaultValue;
            statType.MinValue = statDefinition.MinValue;
            statType.MaxValue = statDefinition.MaxValue;
            statType.Formula = statDefinition.Formula;
            
            var path = AssetDatabase.GetAssetPath(statDefinition);
            var newPath = path.Replace(".asset", "_Type.asset");
            
            AssetDatabase.CreateAsset(statType, newPath);
            AssetDatabase.SaveAssets();
            RefreshData();
            selectedStatType = statType;
            selectedStatDefinition = null;
        }
        
        private void DeleteStatDefinition(StatDefinition statDefinition)
        {
            var path = AssetDatabase.GetAssetPath(statDefinition);
            AssetDatabase.DeleteAsset(path);
            RefreshData();
            selectedStatDefinition = null;
        }
        
        private void DeleteStatType(StatType statType)
        {
            var path = AssetDatabase.GetAssetPath(statType);
            AssetDatabase.DeleteAsset(path);
            RefreshData();
            selectedStatType = null;
        }
        
        private void DeleteContainer(StatContainer container)
        {
            var path = AssetDatabase.GetAssetPath(container);
            AssetDatabase.DeleteAsset(path);
            RefreshData();
            selectedContainer = null;
        }
        
        private void RefreshData()
        {
            // Load stat definitions
            var statDefGuids = AssetDatabase.FindAssets("t:StatDefinition");
            allStatDefinitions = statDefGuids.Select(guid => 
                AssetDatabase.LoadAssetAtPath<StatDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null)
                .OrderBy(s => s.DisplayName)
                .ToList();
            
            // Load stat types
            var statTypeGuids = AssetDatabase.FindAssets("t:StatType");
            allStatTypes = statTypeGuids.Select(guid => 
                AssetDatabase.LoadAssetAtPath<StatType>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null)
                .OrderBy(s => s.DisplayName)
                .ToList();
            
            // Load containers
            var containerGuids = AssetDatabase.FindAssets("t:StatContainer");
            allContainers = containerGuids.Select(guid => 
                AssetDatabase.LoadAssetAtPath<StatContainer>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null)
                .OrderBy(c => c.ContainerName)
                .ToList();
            
            Repaint();
        }
        
        private void ShowHelp()
        {
            EditorUtility.DisplayDialog("StatForge Help", 
                "StatForge Enhanced Editor\n\n" +
                "• StatDefinitions: New enhanced stat definitions with colors and icons\n" +
                "• Legacy StatTypes: Original stat types for backward compatibility\n" +
                "• Containers: Collections of stats for entities\n" +
                "• Overview: Project statistics and breakdown\n\n" +
                "Use the + button to create new items.\n" +
                "Convert between StatTypes and StatDefinitions using the convert buttons.", 
                "OK");
        }
    }
}
#endif