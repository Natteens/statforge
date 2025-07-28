#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeEditor : EditorWindow
    {
        private enum ViewMode { StatTypes, Containers }
        
        private ViewMode currentView = ViewMode.StatTypes;
        private List<StatType> allStats;
        private List<StatContainer> allContainers;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private string searchFilter = "";
        
        // Selection
        private StatType selectedStat;
        private StatContainer selectedContainer;
        
        [MenuItem("Tools/StatForge/Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeEditor>("StatForge");
            window.minSize = new Vector2(800f, 500f);
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
            var leftRect = new Rect(rect.x, rect.y, 250, rect.height);
            var rightRect = new Rect(leftRect.xMax + 5, rect.y, rect.width - leftRect.width - 5, rect.height);
            
            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Toggle(currentView == ViewMode.StatTypes, "Stats", EditorStyles.toolbarButton))
                currentView = ViewMode.StatTypes;
            if (GUILayout.Toggle(currentView == ViewMode.Containers, "Containers", EditorStyles.toolbarButton))
                currentView = ViewMode.Containers;
            
            GUILayout.Space(20);
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                RefreshData();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            var title = currentView == ViewMode.StatTypes ? "Stat Types" : "Containers";
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(25)))
                CreateNew();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // List
            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);
            
            if (currentView == ViewMode.StatTypes)
                DrawStatTypesList();
            else
                DrawContainersList();
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawStatTypesList()
        {
            if (allStats == null) return;
            
            var filtered = allStats.Where(s => string.IsNullOrEmpty(searchFilter) || 
                s.DisplayName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var stat in filtered)
            {
                var isSelected = selectedStat == stat;
                var style = isSelected ? "selectionRect" : "box";
                
                EditorGUILayout.BeginVertical(style);
                
                GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
                GUILayout.Label($"({stat.ShortName}) - {stat.Category}", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedStat = stat;
                    selectedContainer = null;
                    Repaint();
                    Event.current.Use();
                }
            }
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
                    selectedStat = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            if (selectedStat != null)
                DrawStatEditor();
            else if (selectedContainer != null)
                DrawContainerEditor();
            else
                DrawWelcome();
            
            GUILayout.EndArea();
        }
        
        private void DrawStatEditor()
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedStat.DisplayName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Stat", $"Delete '{selectedStat.DisplayName}'?", "Delete", "Cancel"))
                {
                    DeleteStat(selectedStat);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            // Fields
            selectedStat.DisplayName = EditorGUILayout.TextField("Display Name", selectedStat.DisplayName);
            selectedStat.ShortName = EditorGUILayout.TextField("Short Name", selectedStat.ShortName);
            selectedStat.Category = (StatCategory)EditorGUILayout.EnumPopup("Category", selectedStat.Category);
            selectedStat.DefaultValue = EditorGUILayout.FloatField("Default Value", selectedStat.DefaultValue);
            selectedStat.MinValue = EditorGUILayout.FloatField("Min Value", selectedStat.MinValue);
            selectedStat.MaxValue = EditorGUILayout.FloatField("Max Value", selectedStat.MaxValue);
            
            if (selectedStat.Category == StatCategory.Derived)
            {
                GUILayout.Label("Formula", EditorStyles.boldLabel);
                selectedStat.Formula = EditorGUILayout.TextArea(selectedStat.Formula, GUILayout.Height(60));
            }
            
            GUILayout.EndScrollView();
            
            if (GUI.changed)
                EditorUtility.SetDirty(selectedStat);
        }
        
        private void DrawContainerEditor()
        {
            // Header
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
            
            // Basic Info
            selectedContainer.ContainerName = EditorGUILayout.TextField("Name", selectedContainer.ContainerName);
            selectedContainer.Category = (ContainerCategory)EditorGUILayout.EnumPopup("Category", selectedContainer.Category);
            
            GUILayout.Label("Description", EditorStyles.boldLabel);
            selectedContainer.Description = EditorGUILayout.TextArea(selectedContainer.Description, GUILayout.Height(40));
            
            GUILayout.Space(15);
            
            // Stats
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            
            // Add stat dropdown
            if (allStats != null && allStats.Count > 0)
            {
                var availableStats = allStats.Where(s => !selectedContainer.HasStat(s)).ToArray();
                if (availableStats.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Add Stat:", GUILayout.Width(60));
                    
                    var names = new string[availableStats.Length + 1];
                    names[0] = "Select Stat...";
                    for (int i = 0; i < availableStats.Length; i++)
                    {
                        names[i + 1] = availableStats[i].DisplayName;
                    }
                    
                    var selectedIndex = EditorGUILayout.Popup(0, names);
                    
                    GUI.enabled = selectedIndex > 0;
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && selectedIndex > 0)
                    {
                        selectedContainer.AddStat(availableStats[selectedIndex - 1]);
                        EditorUtility.SetDirty(selectedContainer);
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("All available stats already added", EditorStyles.miniLabel);
                }
            }
            
            GUILayout.Space(10);
            
            // Stats list
            if (selectedContainer.Stats != null)
            {
                for (int i = selectedContainer.Stats.Count - 1; i >= 0; i--)
                {
                    var stat = selectedContainer.Stats[i];
                    if (stat?.statType == null) continue;
                    
                    EditorGUILayout.BeginHorizontal("box");
                    
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(stat.statType.DisplayName, EditorStyles.boldLabel);
                    GUILayout.Label($"Base: {stat.baseValue:F1}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    GUILayout.FlexibleSpace();
                    
                    GUILayout.Label("Base:", GUILayout.Width(35));
                    var newBase = EditorGUILayout.FloatField(stat.baseValue, GUILayout.Width(50));
                    if (!Mathf.Approximately(newBase, stat.baseValue))
                    {
                        stat.SetBaseValue(newBase);
                        EditorUtility.SetDirty(selectedContainer);
                    }
                    
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        selectedContainer.RemoveStat(stat.statType);
                        EditorUtility.SetDirty(selectedContainer);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            GUILayout.EndScrollView();
            
            if (GUI.changed)
                EditorUtility.SetDirty(selectedContainer);
        }
        
        private void DrawWelcome()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("StatForge Editor", EditorStyles.largeLabel);
            GUILayout.Label("Select an item from the left panel to edit", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }
        
        private void CreateNew()
        {
            if (currentView == ViewMode.StatTypes)
                CreateStatType();
            else
                CreateContainer();
        }
        
        private void CreateStatType()
        {
            var asset = CreateInstance<StatType>();
            asset.DisplayName = "New Stat";
            asset.ShortName = "NS";
            
            var path = EditorUtility.SaveFilePanel("Create Stat Type", "Assets", "NewStat", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                RefreshData();
                selectedStat = asset;
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
        
        private void DeleteStat(StatType stat)
        {
            var path = AssetDatabase.GetAssetPath(stat);
            AssetDatabase.DeleteAsset(path);
            RefreshData();
            selectedStat = null;
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
            // Load stats
            var statGuids = AssetDatabase.FindAssets("t:StatType");
            allStats = statGuids.Select(guid => 
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
    }
}
#endif