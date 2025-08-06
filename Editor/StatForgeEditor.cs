#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeEditor : EditorWindow
    {
        private enum ViewMode { StatTypes, Containers, StatCollections }
        
        private ViewMode currentView = ViewMode.StatTypes;
        private List<StatType> allStats;
        private List<StatContainer> allContainers;
        private List<GameObject> statObjects; // Objects with StatCollections
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private string searchFilter = "";
        
        // Selection
        private StatType selectedStat;
        private StatContainer selectedContainer;
        private GameObject selectedStatObject;
        
        // Styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle selectedCardStyle;
        private bool stylesInitialized;
        
        [MenuItem("Tools/StatForge/Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeEditor>("StatForge");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshData();
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 5, 5)
            };
            
            cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            selectedCardStyle = new GUIStyle("selectionRect")
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            DrawModernToolbar();
            
            var rect = new Rect(0, 35, position.width, position.height - 35);
            var leftRect = new Rect(rect.x + 5, rect.y, 280, rect.height - 10);
            var rightRect = new Rect(leftRect.xMax + 10, rect.y, rect.width - leftRect.width - 20, rect.height - 10);
            
            DrawModernLeftPanel(leftRect);
            DrawModernRightPanel(rightRect);
        }
        
        private void DrawModernToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(30));
            
            // View mode buttons with modern styling
            var tabStyle = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 25 };
            
            if (GUILayout.Toggle(currentView == ViewMode.StatTypes, "Stat Types", tabStyle, GUILayout.Width(80)))
            {
                if (currentView != ViewMode.StatTypes)
                {
                    currentView = ViewMode.StatTypes;
                    ClearSelection();
                }
            }
            if (GUILayout.Toggle(currentView == ViewMode.Containers, "Containers", tabStyle, GUILayout.Width(80)))
            {
                if (currentView != ViewMode.Containers)
                {
                    currentView = ViewMode.Containers;
                    ClearSelection();
                }
            }
            if (GUILayout.Toggle(currentView == ViewMode.StatCollections, "Live Stats", tabStyle, GUILayout.Width(80)))
            {
                if (currentView != ViewMode.StatCollections)
                {
                    currentView = ViewMode.StatCollections;
                    ClearSelection();
                    RefreshStatObjects();
                }
            }
            
            GUILayout.Space(20);
            
            // Search field with icon
            GUILayout.Label("Search:", GUILayout.Width(45));
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            
            GUILayout.FlexibleSpace();
            
            // Action buttons
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshData();
            
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
                CreateNew();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void ClearSelection()
        {
            selectedStat = null;
            selectedContainer = null;
            selectedStatObject = null;
        }
        
        private void DrawModernLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            // Modern header
            EditorGUILayout.BeginHorizontal();
            var title = GetCurrentViewTitle();
            GUILayout.Label(title, headerStyle);
            GUILayout.FlexibleSpace();
            
            var createButtonStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 30, fixedHeight = 25 };
            if (GUILayout.Button("+", createButtonStyle))
                CreateNew();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Content list with modern cards
            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);
            
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    DrawModernStatTypesList();
                    break;
                case ViewMode.Containers:
                    DrawModernContainersList();
                    break;
                case ViewMode.StatCollections:
                    DrawModernStatObjectsList();
                    break;
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private string GetCurrentViewTitle()
        {
            return currentView switch
            {
                ViewMode.StatTypes => "Stat Types",
                ViewMode.Containers => "Containers", 
                ViewMode.StatCollections => "Live Objects",
                _ => "Items"
            };
        }
        
        private void DrawModernStatTypesList()
        {
            if (allStats == null) return;
            
            var filtered = allStats.Where(s => string.IsNullOrEmpty(searchFilter) || 
                s.DisplayName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var stat in filtered)
            {
                var isSelected = selectedStat == stat;
                var style = isSelected ? selectedCardStyle : cardStyle;
                
                EditorGUILayout.BeginVertical(style);
                
                // Title and category in same line
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                // Category badge
                var categoryColor = GetCategoryColor(stat.Category);
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = categoryColor;
                GUILayout.Label(stat.Category.ToString(), EditorStyles.miniButton, GUILayout.Width(60));
                GUI.backgroundColor = oldColor;
                
                EditorGUILayout.EndHorizontal();
                
                // Short name and default value
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"({stat.ShortName})", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (stat.Category != StatCategory.Derived)
                {
                    GUILayout.Label($"Default: {stat.DefaultValue:F1}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                // Formula for derived stats
                if (stat.Category == StatCategory.Derived && !string.IsNullOrEmpty(stat.Formula))
                {
                    GUILayout.Label($"Formula: {stat.Formula}", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedStat = stat;
                    selectedContainer = null;
                    selectedStatObject = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private Color GetCategoryColor(StatCategory category)
        {
            return category switch
            {
                StatCategory.Primary => new Color(0.4f, 0.8f, 0.4f),
                StatCategory.Derived => new Color(0.4f, 0.6f, 0.9f),
                StatCategory.External => new Color(0.9f, 0.7f, 0.4f),
                _ => Color.gray
            };
        }
        
        private void DrawModernContainersList()
        {
            if (allContainers == null) return;
            
            var filtered = allContainers.Where(c => string.IsNullOrEmpty(searchFilter) || 
                c.ContainerName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var container in filtered)
            {
                var isSelected = selectedContainer == container;
                var style = isSelected ? selectedCardStyle : cardStyle;
                
                EditorGUILayout.BeginVertical(style);
                
                // Title and category
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(container.ContainerName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                var categoryColor = GetContainerCategoryColor(container.Category);
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = categoryColor;
                GUILayout.Label(container.Category.ToString(), EditorStyles.miniButton, GUILayout.Width(60));
                GUI.backgroundColor = oldColor;
                
                EditorGUILayout.EndHorizontal();
                
                // Stats count and description
                GUILayout.Label($"{container.Stats?.Count ?? 0} stats", EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(container.Description))
                {
                    GUILayout.Label(container.Description.Length > 50 
                        ? container.Description.Substring(0, 50) + "..." 
                        : container.Description, EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedContainer = container;
                    selectedStat = null;
                    selectedStatObject = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private Color GetContainerCategoryColor(ContainerCategory category)
        {
            return category switch
            {
                ContainerCategory.Base => new Color(0.6f, 0.8f, 0.6f),
                ContainerCategory.Equipment => new Color(0.8f, 0.6f, 0.8f),
                ContainerCategory.Temporary => new Color(0.8f, 0.8f, 0.6f),
                _ => Color.gray
            };
        }
        
        private void DrawModernStatObjectsList()
        {
            if (statObjects == null) 
            {
                GUILayout.Label("No objects with stats found in scene", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            var filtered = statObjects.Where(obj => obj != null && 
                (string.IsNullOrEmpty(searchFilter) || obj.name.ToLower().Contains(searchFilter.ToLower()))).ToList();
            
            foreach (var obj in filtered)
            {
                var isSelected = selectedStatObject == obj;
                var style = isSelected ? selectedCardStyle : cardStyle;
                
                EditorGUILayout.BeginVertical(style);
                
                // Object name and active status
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(obj.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                var statusColor = obj.activeInHierarchy ? Color.green : Color.gray;
                var oldColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(obj.activeInHierarchy ? "Active" : "Inactive", EditorStyles.miniButton, GUILayout.Width(50));
                GUI.color = oldColor;
                
                EditorGUILayout.EndHorizontal();
                
                // Stats info
                var collection = obj.GetStatCollection();
                if (collection != null)
                {
                    GUILayout.Label($"{collection.Stats.Count} stats active", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedStatObject = obj;
                    selectedStat = null;
                    selectedContainer = null;
                    Selection.activeGameObject = obj; // Highlight in hierarchy
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawModernRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            if (selectedStat != null)
                DrawModernStatEditor();
            else if (selectedContainer != null)
                DrawModernContainerEditor();
            else if (selectedStatObject != null)
                DrawModernStatObjectEditor();
            else
                DrawModernWelcome();
            
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
        
        private void DrawModernWelcome()
        {
            GUILayout.FlexibleSpace();
            
            var logoStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("StatForge", logoStyle);
            GUILayout.Space(10);
            
            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            GUILayout.Label("Modern stat management for Unity", subtitleStyle);
            GUILayout.Space(20);
            
            var instructionStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12,
                wordWrap = true
            };
            
            GUILayout.Label("Select an item from the left panel to begin editing", instructionStyle);
            
            GUILayout.Space(30);
            
            // Quick actions
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Create Stat Type", GUILayout.Width(120), GUILayout.Height(30)))
            {
                currentView = ViewMode.StatTypes;
                CreateStatType();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Container", GUILayout.Width(120), GUILayout.Height(30)))
            {
                currentView = ViewMode.Containers;
                CreateContainer();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
        }
        
        private void DrawModernStatEditor()
        {
            // Keep the original DrawStatEditor functionality but with modern styling
            DrawStatEditor();
        }
        
        private void DrawModernContainerEditor()
        {
            // Keep the original DrawContainerEditor functionality but with modern styling  
            DrawContainerEditor();
        }
        
        private void DrawModernStatObjectEditor()
        {
            if (selectedStatObject == null) return;
            
            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Live Stats: {selectedStatObject.name}", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Select in Scene", GUILayout.Width(100)))
            {
                Selection.activeGameObject = selectedStatObject;
                EditorGUIUtility.PingObject(selectedStatObject);
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            var collection = selectedStatObject.GetStatCollection();
            if (collection != null && collection.Stats.Count > 0)
            {
                foreach (var stat in collection.Stats)
                {
                    DrawStatCard(stat, selectedStatObject);
                    GUILayout.Space(5);
                }
            }
            else
            {
                GUILayout.Label("No stats found on this object", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(20);
                
                if (GUILayout.Button("Initialize Stats", GUILayout.Height(30)))
                {
                    selectedStatObject.InitializeStats();
                    Repaint();
                }
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawStatCard(StatData stat, GameObject owner)
        {
            EditorGUILayout.BeginVertical(cardStyle);
            
            // Stat name and current value
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(stat.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Value: {stat.GetValue():F2}", EditorStyles.label);
            EditorGUILayout.EndHorizontal();
            
            // Base value editor
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Base Value:", GUILayout.Width(80));
            var newBase = EditorGUILayout.FloatField(stat.BaseValue);
            if (!Mathf.Approximately(newBase, stat.BaseValue))
            {
                owner.SetStat(stat.Name, newBase);
            }
            EditorGUILayout.EndHorizontal();
            
            // Show modifiers if any
            var modifiers = owner.GetStatModifiers(stat.Name);
            if (modifiers.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Active Modifiers:", EditorStyles.miniLabel);
                
                foreach (var modifier in modifiers)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label($"{modifier.Type}: {modifier.Value:F2}", EditorStyles.miniLabel);
                    
                    if (modifier.HasDuration)
                    {
                        GUILayout.Label($"({modifier.RemainingDuration:F1}s)", EditorStyles.miniLabel);
                    }
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        owner.RemoveStatModifier(stat.Name, modifier);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            // Quick modifier buttons
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quick Modifiers:", EditorStyles.miniLabel, GUILayout.Width(100));
            
            if (GUILayout.Button("+10", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                owner.ApplyTemporaryModifier(stat.Name, ModifierType.Additive, 10f, 5f);
            }
            if (GUILayout.Button("-10", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                owner.ApplyTemporaryModifier(stat.Name, ModifierType.Additive, -10f, 5f);
            }
            if (GUILayout.Button("x2", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                owner.ApplyTemporaryModifier(stat.Name, ModifierType.Multiplicative, 2f, 5f);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void RefreshStatObjects()
        {
            statObjects = new List<GameObject>();
            
            // Find all GameObjects in scene with StatCollection components or [Stat] attributes
            var allObjects = FindObjectsOfType<MonoBehaviour>(true);
            var objectsWithStats = new HashSet<GameObject>();
            
            foreach (var obj in allObjects)
            {
                // Check if object has a StatCollection
                var collection = obj.gameObject.GetStatCollection();
                if (collection != null && collection.Stats.Count > 0)
                {
                    objectsWithStats.Add(obj.gameObject);
                    continue;
                }
                
                // Check if object has [Stat] attributes
                var type = obj.GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<StatAttribute>() != null)
                    {
                        objectsWithStats.Add(obj.gameObject);
                        break;
                    }
                }
            }
            
            statObjects = objectsWithStats.ToList();
        }
        
        private void CreateNew()
        {
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    CreateStatType();
                    break;
                case ViewMode.Containers:
                    CreateContainer();
                    break;
                case ViewMode.StatCollections:
                    // For stat collections, we can't create new ones directly
                    // Instead show a helpful message
                    EditorUtility.DisplayDialog("Info", 
                        "Live stat collections are automatically created when objects use [Stat] attributes or call StatExtensions methods.\n\n" +
                        "To create stats on an object:\n" +
                        "1. Add [Stat] attributes to fields\n" +
                        "2. Call this.SetStat() in code\n" +
                        "3. Use this.GetStat() to access values", "OK");
                    break;
            }
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
            
            // Refresh stat objects if we're viewing them
            if (currentView == ViewMode.StatCollections)
            {
                RefreshStatObjects();
            }
            
            Repaint();
        }
    }
}
#endif