#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    /// <summary>
    /// Modern, clean editor for StatForge with simplified UI
    /// </summary>
    public class ModernStatForgeEditor : EditorWindow
    {
        private enum ViewMode { Stats, Components, Templates }
        
        private ViewMode currentView = ViewMode.Stats;
        private List<StatType> allStats;
        private List<StatForgeComponent> allComponents;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private string searchFilter = "";
        
        // Selection
        private StatType selectedStat;
        private StatForgeComponent selectedComponent;
        
        // Styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle selectedCardStyle;
        
        [MenuItem("Tools/StatForge/Modern Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModernStatForgeEditor>("StatForge");
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
            headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            selectedCardStyle = new GUIStyle(cardStyle)
            {
                normal = { background = CreateColorTexture(new Color(0.3f, 0.5f, 0.9f, 0.3f)) }
            };
        }
        
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawMainContent();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Title
            EditorGUILayout.LabelField("StatForge", headerStyle);
            EditorGUILayout.LabelField("Modern Attribute System", EditorStyles.miniLabel);
            
            GUILayout.Space(10);
            
            // Navigation
            EditorGUILayout.BeginHorizontal();
            
            if (DrawTab("Stats", currentView == ViewMode.Stats))
                currentView = ViewMode.Stats;
            if (DrawTab("Components", currentView == ViewMode.Components))
                currentView = ViewMode.Components;
            if (DrawTab("Templates", currentView == ViewMode.Templates))
                currentView = ViewMode.Templates;
            
            GUILayout.FlexibleSpace();
            
            // Search
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(200));
            
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                RefreshData();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private bool DrawTab(string label, bool isActive)
        {
            var style = isActive ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            var color = GUI.color;
            
            if (isActive)
                GUI.color = new Color(0.7f, 0.9f, 1f);
            
            var result = GUILayout.Button(label, style, GUILayout.Width(100));
            GUI.color = color;
            
            return result;
        }
        
        private void DrawMainContent()
        {
            var rect = GUILayoutUtility.GetRect(0, position.height - 80, GUILayout.ExpandWidth(true));
            var leftRect = new Rect(rect.x, rect.y, 300, rect.height);
            var rightRect = new Rect(leftRect.xMax + 10, rect.y, rect.width - leftRect.width - 10, rect.height);
            
            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            // Panel header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var title = GetCurrentPanelTitle();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
                CreateNew();
            EditorGUILayout.EndHorizontal();
            
            // Content
            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);
            
            switch (currentView)
            {
                case ViewMode.Stats:
                    DrawStatsList();
                    break;
                case ViewMode.Components:
                    DrawComponentsList();
                    break;
                case ViewMode.Templates:
                    DrawTemplatesList();
                    break;
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private string GetCurrentPanelTitle()
        {
            return currentView switch
            {
                ViewMode.Stats => "Stat Types",
                ViewMode.Components => "Components",
                ViewMode.Templates => "Templates",
                _ => ""
            };
        }
        
        private void DrawStatsList()
        {
            if (allStats == null) return;
            
            var filtered = allStats.Where(s => string.IsNullOrEmpty(searchFilter) || 
                s.DisplayName.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var stat in filtered)
            {
                var isSelected = selectedStat == stat;
                var style = isSelected ? selectedCardStyle : cardStyle;
                
                EditorGUILayout.BeginVertical(style);
                
                // Title
                EditorGUILayout.LabelField(stat.DisplayName, EditorStyles.boldLabel);
                
                // Details
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{stat.ShortName}", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField($"{stat.Category}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                // Values
                if (stat.Category == StatCategory.Primary)
                {
                    EditorGUILayout.LabelField($"Range: {stat.MinValue} - {stat.MaxValue}", EditorStyles.miniLabel);
                }
                else if (stat.Category == StatCategory.Derived && !string.IsNullOrEmpty(stat.Formula))
                {
                    EditorGUILayout.LabelField($"Formula: {stat.Formula}", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedStat = stat;
                    selectedComponent = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawComponentsList()
        {
            if (allComponents == null) return;
            
            var filtered = allComponents.Where(c => string.IsNullOrEmpty(searchFilter) || 
                c.name.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            foreach (var component in filtered)
            {
                var isSelected = selectedComponent == component;
                var style = isSelected ? selectedCardStyle : cardStyle;
                
                EditorGUILayout.BeginVertical(style);
                
                EditorGUILayout.LabelField(component.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"GameObject: {component.gameObject.name}", EditorStyles.miniLabel);
                
                var attrCount = component.Attributes?.GetAttributeNames()?.Count() ?? 0;
                EditorGUILayout.LabelField($"Attributes: {attrCount}", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                
                if (Event.current.type == EventType.MouseDown && 
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedComponent = component;
                    selectedStat = null;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawTemplatesList()
        {
            EditorGUILayout.HelpBox("Templates feature coming soon!", MessageType.Info);
        }
        
        private void DrawRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);
            
            if (selectedStat != null)
            {
                DrawStatDetails();
            }
            else if (selectedComponent != null)
            {
                DrawComponentDetails();
            }
            else
            {
                DrawWelcomeScreen();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawWelcomeScreen()
        {
            GUILayout.Space(50);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Welcome to StatForge", headerStyle);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Ultra-Simplified API", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Transform your game with minimal code:");
            
            GUILayout.Space(10);
            
            var codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic
            };
            
            EditorGUILayout.TextArea(
                "public class Player : MonoBehaviour\n{\n    [Stat] public int Strength = 10;\n    [Stat] public float Health = 100f;\n    \n    void Update()\n    {\n        Health -= Time.deltaTime;  // Natural syntax!\n        if (Input.GetKeyDown(KeyCode.Space)) Level++;\n    }\n}",
                codeStyle,
                GUILayout.Height(150)
            );
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Create Your First Stat", GUILayout.Height(30)))
            {
                currentView = ViewMode.Stats;
                CreateNew();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatDetails()
        {
            EditorGUILayout.LabelField("Stat Details", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Make stat editable
            EditorGUI.BeginChangeCheck();
            
            selectedStat.DisplayName = EditorGUILayout.TextField("Display Name", selectedStat.DisplayName);
            selectedStat.ShortName = EditorGUILayout.TextField("Short Name", selectedStat.ShortName);
            selectedStat.Category = (StatCategory)EditorGUILayout.EnumPopup("Category", selectedStat.Category);
            
            GUILayout.Space(10);
            
            if (selectedStat.Category == StatCategory.Primary || selectedStat.Category == StatCategory.External)
            {
                selectedStat.DefaultValue = EditorGUILayout.FloatField("Default Value", selectedStat.DefaultValue);
                selectedStat.MinValue = EditorGUILayout.FloatField("Min Value", selectedStat.MinValue);
                selectedStat.MaxValue = EditorGUILayout.FloatField("Max Value", selectedStat.MaxValue);
            }
            
            if (selectedStat.Category == StatCategory.Derived)
            {
                EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
                selectedStat.Formula = EditorGUILayout.TextArea(selectedStat.Formula, GUILayout.Height(60));
                
                EditorGUILayout.HelpBox("Use short names of other stats in formulas (e.g., 'STR * 2 + DEX')", MessageType.Info);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedStat);
            }
        }
        
        private void DrawComponentDetails()
        {
            EditorGUILayout.LabelField("Component Details", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField($"GameObject: {selectedComponent.gameObject.name}");
            
            GUILayout.Space(10);
            
            var attributeNames = selectedComponent.Attributes?.GetAttributeNames()?.ToList();
            if (attributeNames != null && attributeNames.Any())
            {
                EditorGUILayout.LabelField("Discovered Attributes:", EditorStyles.boldLabel);
                
                foreach (var name in attributeNames)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(name, GUILayout.Width(120));
                    
                    // Try to display current value
                    try
                    {
                        var intVal = selectedComponent.Get<int>(name);
                        EditorGUILayout.LabelField($"Value: {intVal}");
                    }
                    catch
                    {
                        try
                        {
                            var floatVal = selectedComponent.Get<float>(name);
                            EditorGUILayout.LabelField($"Value: {floatVal:F2}");
                        }
                        catch
                        {
                            EditorGUILayout.LabelField("Value: N/A");
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No attributes discovered. Make sure to mark fields with [Stat] attribute.", MessageType.Info);
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Refresh Attributes"))
            {
                selectedComponent.InitializeAttributes();
                Repaint();
            }
        }
        
        private void CreateNew()
        {
            switch (currentView)
            {
                case ViewMode.Stats:
                    CreateNewStat();
                    break;
                case ViewMode.Components:
                    CreateNewComponent();
                    break;
            }
        }
        
        private void CreateNewStat()
        {
            var stat = CreateInstance<StatType>();
            stat.DisplayName = "New Stat";
            stat.ShortName = "NEW";
            
            var path = EditorUtility.SaveFilePanelInProject(
                "Create New Stat",
                "NewStat.asset",
                "asset",
                "Choose location for new stat");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(stat, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                selectedStat = stat;
                RefreshData();
            }
        }
        
        private void CreateNewComponent()
        {
            var go = new GameObject("StatForge Entity");
            go.AddComponent<StatForgeComponent>();
            Selection.activeGameObject = go;
        }
        
        private void RefreshData()
        {
            allStats = FindAllStatTypes();
            allComponents = FindObjectsOfType<StatForgeComponent>().ToList();
        }
        
        private List<StatType> FindAllStatTypes()
        {
            var guids = AssetDatabase.FindAssets("t:StatType");
            return guids.Select(guid => AssetDatabase.LoadAssetAtPath<StatType>(AssetDatabase.GUIDToAssetPath(guid)))
                       .Where(stat => stat != null)
                       .ToList();
        }
    }
}
#endif