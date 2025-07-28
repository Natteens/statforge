#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeManager : EditorWindow
    {
        private enum ViewMode { StatTypes, Containers, Templates, Tests }
        private ViewMode currentView = ViewMode.StatTypes;
        
        private List<StatType> allStats;
        private List<StatContainer> allContainers;
        private List<ContainerTemplate> allTemplates;
        
        private Vector2 leftPanelScroll;
        private Vector2 rightPanelScroll;
        private string searchFilter = "";
        private StatCategory filterCategory = (StatCategory)(-1);
        private ContainerCategory filterContainerCategory = (ContainerCategory)(-1);
        
        private StatType selectedStat;
        private StatContainer selectedContainer;
        private ContainerTemplate selectedTemplate;
        
        private bool isCreatingNew;
        private string newName = "";
        private StatCategory newCategory = StatCategory.Primary;
        private ContainerCategory newContainerCategory = ContainerCategory.Base;
        private string newFormula = "";
        private float newDefaultValue;
        private float newMinValue;
        private float newMaxValue = 100f;
        private string newShortName = "";
        private string newDescription = "";
        private List<StatType> newTemplateStats;
        
        private StatContainer testContainer;
        private bool testInitialized;
        private Dictionary<StatType, float> testTempBonuses;
        
        [MenuItem("Tools/StatForge/Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeManager>("StatForge Manager");
            window.minSize = new Vector2(1200f, 700f);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshData();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            var contentRect = new Rect(0, 40, position.width, position.height - 40);
            
            var leftRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.3f, contentRect.height);
            DrawLeftPanel(leftRect);
            
            var rightRect = new Rect(leftRect.xMax, contentRect.y, contentRect.width * 0.7f, contentRect.height);
            DrawRightPanel(rightRect);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Toggle(currentView == ViewMode.StatTypes, "Stat Types", EditorStyles.toolbarButton))
            {
                if (currentView != ViewMode.StatTypes)
                {
                    currentView = ViewMode.StatTypes;
                    ClearSelection();
                }
            }
            
            if (GUILayout.Toggle(currentView == ViewMode.Containers, "Containers", EditorStyles.toolbarButton))
            {
                if (currentView != ViewMode.Containers)
                {
                    currentView = ViewMode.Containers;
                    ClearSelection();
                }
            }
            
            if (GUILayout.Toggle(currentView == ViewMode.Templates, "Templates", EditorStyles.toolbarButton))
            {
                if (currentView != ViewMode.Templates)
                {
                    currentView = ViewMode.Templates;
                    ClearSelection();
                }
            }
            
            if (GUILayout.Toggle(currentView == ViewMode.Tests, "Tests", EditorStyles.toolbarButton))
            {
                if (currentView != ViewMode.Tests)
                {
                    currentView = ViewMode.Tests;
                    ClearSelection();
                }
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            GUILayout.Space(10);
            
            if (currentView == ViewMode.StatTypes)
            {
                GUILayout.Label("Category:", GUILayout.Width(60));
                var categories = new[] { "All", "Primary", "Derived", "External" };
                var selectedIndex = (int)filterCategory + 1;
                var newIndex = EditorGUILayout.Popup(selectedIndex, categories, EditorStyles.toolbarPopup, GUILayout.Width(100));
                filterCategory = (StatCategory)(newIndex - 1);
            }
            else if (currentView == ViewMode.Containers)
            {
                GUILayout.Label("Category:", GUILayout.Width(60));
                var categories = new[] { "All", "Base", "Equipment", "Character", "Skill", "Buff", "Debuff" };
                var selectedIndex = (int)filterContainerCategory + 1;
                var newIndex = EditorGUILayout.Popup(selectedIndex, categories, EditorStyles.toolbarPopup, GUILayout.Width(100));
                filterContainerCategory = (ContainerCategory)(newIndex - 1);
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshData();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            string Title = "";
            int count = 0;
            
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    Title = "Stat Types";
                    count = GetFilteredStats().Count;
                    break;
                case ViewMode.Containers:
                    Title = "Containers";
                    count = GetFilteredContainers().Count;
                    break;
                case ViewMode.Templates:
                    Title = "Templates";
                    count = allTemplates.Count;
                    break;
                case ViewMode.Tests:
                    Title = "Test Environment";
                    count = allContainers.Count;
                    break;
            }
            
            GUILayout.Label($"{Title} ({count})", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (currentView != ViewMode.Tests && GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
            {
                StartCreatingNew();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            leftPanelScroll = GUILayout.BeginScrollView(leftPanelScroll);
            
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    DrawStatTypesList();
                    break;
                case ViewMode.Containers:
                    DrawContainersList();
                    break;
                case ViewMode.Templates:
                    DrawTemplatesList();
                    break;
                case ViewMode.Tests:
                    DrawTestsList();
                    break;
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawRightPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            
            if (isCreatingNew)
            {
                DrawCreateNewPanel();
            }
            else
            {
                switch (currentView)
                {
                    case ViewMode.StatTypes when selectedStat != null:
                        DrawStatTypeEditor();
                        break;
                    case ViewMode.Containers when selectedContainer != null:
                        DrawContainerEditor();
                        break;
                    case ViewMode.Templates when selectedTemplate != null:
                        DrawTemplateEditor();
                        break;
                    case ViewMode.Tests:
                        DrawTestPanel();
                        break;
                    default:
                        DrawWelcomePanel();
                        break;
                }
            }
            
            GUILayout.EndArea();
        }
        
        private void DrawTemplatesList()
        {
            foreach (var template in allTemplates)
            {
                var isSelected = selectedTemplate == template;
                
                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");
                
                GUILayout.Label(template.templateName, EditorStyles.boldLabel);
                GUILayout.Label($"{template.statTypes.Count} stats", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                
                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    selectedTemplate = template;
                    isCreatingNew = false;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawTestsList()
        {
            GUILayout.Label("Available Containers for Testing:", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            foreach (var container in allContainers)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(container.ContainerName, EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Test", GUILayout.Width(60)))
                {
                    InitializeTest(container);
                }
                
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Label($"Category: {container.Category}", EditorStyles.miniLabel);
                GUILayout.Label($"Stats: {container.Stats.Count}", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawStatTypesList()
        {
            var filteredStats = GetFilteredStats();
            
            foreach (var stat in filteredStats)
            {
                var isSelected = selectedStat == stat;
                
                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");
                
                if (GUILayout.Button("", GUIStyle.none, GUILayout.Height(0)))
                {
                    selectedStat = stat;
                    isCreatingNew = false;
                }
                
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();
                
                var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
                switch (stat.Category)
                {
                    case StatCategory.Primary:
                        categoryStyle.normal.textColor = Color.cyan;
                        break;
                    case StatCategory.Derived:
                        categoryStyle.normal.textColor = Color.green;
                        break;
                    case StatCategory.External:
                        categoryStyle.normal.textColor = Color.yellow;
                        break;
                }
                GUILayout.Label(stat.Category.ToString(), categoryStyle);
                
                EditorGUILayout.EndHorizontal();
                
                if (!string.IsNullOrEmpty(stat.ShortName))
                {
                    GUILayout.Label($"({stat.ShortName})", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                
                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    selectedStat = stat;
                    isCreatingNew = false;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawContainersList()
        {
            var filteredContainers = GetFilteredContainers();
            
            foreach (var container in filteredContainers)
            {
                var isSelected = selectedContainer == container;
                
                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");
                
                GUILayout.Label(container.ContainerName, EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{container.Stats.Count} stats", EditorStyles.miniLabel);
                
                GUILayout.FlexibleSpace();
                
                var categoryColor = GetCategoryColor(container.Category);
                var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
                categoryStyle.normal.textColor = categoryColor;
                GUILayout.Label(container.Category.ToString(), categoryStyle);
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    selectedContainer = container;
                    isCreatingNew = false;
                    Repaint();
                    Event.current.Use();
                }
            }
        }
        
        private void DrawCreateNewPanel()
        {
            string itemType = "";
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    itemType = "Stat Type";
                    break;
                case ViewMode.Containers:
                    itemType = "Container";
                    break;
                case ViewMode.Templates:
                    itemType = "Template";
                    break;
            }
            
            GUILayout.Label($"Create New {itemType}", EditorStyles.boldLabel);
            
            GUILayout.Space(20);
            
            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);
            
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    DrawCreateStatTypeForm();
                    break;
                case ViewMode.Containers:
                    DrawCreateContainerForm();
                    break;
                case ViewMode.Templates:
                    DrawCreateTemplateForm();
                    break;
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30)))
            {
                CancelCreation();
            }
            
            GUILayout.Space(10);
            
            GUI.enabled = !string.IsNullOrEmpty(newName);
            if (GUILayout.Button("Create", GUILayout.Width(80), GUILayout.Height(30)))
            {
                switch (currentView)
                {
                    case ViewMode.StatTypes:
                        CreateStatType();
                        break;
                    case ViewMode.Containers:
                        CreateContainer();
                        break;
                    case ViewMode.Templates:
                        CreateTemplate();
                        break;
                }
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawCreateTemplateForm()
        {
            DrawFormField("Template Name", () => {
                newName = EditorGUILayout.TextField(newName);
            });
            
            DrawFormField("Description", () => {
                newDescription = EditorGUILayout.TextArea(newDescription, GUILayout.Height(60));
            });
            
            GUILayout.Space(20);
            
            GUILayout.Label("Template Stats", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Stat:", GUILayout.Width(70));
            
            var availableStats = allStats.Where(s => !newTemplateStats.Contains(s)).ToArray();
            var statNames = availableStats.Select(s => s.DisplayName).ToArray();
            
            if (availableStats.Length > 0)
            {
                var selectedIndex = EditorGUILayout.Popup(0, statNames);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    newTemplateStats.Add(availableStats[selectedIndex]);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            for (int i = newTemplateStats.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(newTemplateStats[i].DisplayName);
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    newTemplateStats.RemoveAt(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawCreateContainerForm()
        {
            DrawFormField("Container Name", () => {
                newName = EditorGUILayout.TextField(newName);
            });
            
            DrawFormField("Category", () => {
                newContainerCategory = (ContainerCategory)EditorGUILayout.EnumPopup(newContainerCategory);
            });
            
            DrawFormField("Description", () => {
                newDescription = EditorGUILayout.TextArea(newDescription, GUILayout.Height(60));
            });
            
            if (allTemplates.Count > 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("Create from Template", EditorStyles.boldLabel);
                
                foreach (var template in allTemplates)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(template.templateName, EditorStyles.boldLabel);
                    GUILayout.Label($"{template.statTypes.Count} stats", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Use", GUILayout.Width(50)))
                    {
                        CreateContainerFromTemplate(template);
                        return;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        private void DrawTemplateEditor()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedTemplate.templateName}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Template", 
                    $"Are you sure you want to delete '{selectedTemplate.templateName}'?", "Delete", "Cancel"))
                {
                    DeleteTemplate(selectedTemplate);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);
            
            DrawFormField("Template Name", () => {
                var newTemplateName = EditorGUILayout.TextField(selectedTemplate.templateName);
                if (newTemplateName != selectedTemplate.templateName)
                {
                    selectedTemplate.templateName = newTemplateName;
                    EditorUtility.SetDirty(selectedTemplate);
                }
            });
            
            DrawFormField("Description", () => {
                var newDesc = EditorGUILayout.TextArea(selectedTemplate.description, GUILayout.Height(60));
                if (newDesc != selectedTemplate.description)
                {
                    selectedTemplate.description = newDesc;
                    EditorUtility.SetDirty(selectedTemplate);
                }
            });
            
            GUILayout.Space(20);
            
            GUILayout.Label("Template Stats", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Stat:", GUILayout.Width(70));
            
            var availableStats = allStats.Where(s => !selectedTemplate.statTypes.Contains(s)).ToArray();
            var statNames = availableStats.Select(s => s.DisplayName).ToArray();
            
            if (availableStats.Length > 0)
            {
                var selectedIndex = EditorGUILayout.Popup(0, statNames);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    selectedTemplate.statTypes.Add(availableStats[selectedIndex]);
                    EditorUtility.SetDirty(selectedTemplate);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            for (int i = selectedTemplate.statTypes.Count - 1; i >= 0; i--)
            {
                var stat = selectedTemplate.statTypes[i];
                if (stat == null) continue;
                
                EditorGUILayout.BeginHorizontal("box");
                
                GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    selectedTemplate.statTypes.RemoveAt(i);
                    EditorUtility.SetDirty(selectedTemplate);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Create Container from Template", GUILayout.Height(30)))
            {
                CreateContainerFromTemplate(selectedTemplate);
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawTestPanel()
        {
            if (!testInitialized || testContainer == null)
            {
                GUILayout.FlexibleSpace();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Test Environment", EditorStyles.largeLabel);
                GUILayout.Space(10);
                GUILayout.Label("Select a container from the left panel to start testing", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                return;
            }
            
            GUILayout.Label($"Testing: {testContainer.ContainerName}", EditorStyles.boldLabel);
            
            GUILayout.Space(20);
            
            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Test Controls:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Reset Test", GUILayout.Width(80)))
            {
                ResetTest();
            }
            
            if (GUILayout.Button("Clear Bonuses", GUILayout.Width(100)))
            {
                testTempBonuses.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            GUILayout.Label("Primary Stats", EditorStyles.boldLabel);
            
            var primaryStats = testContainer.GetPrimaryStats();
            foreach (var stat in primaryStats)
            {
                DrawTestStatRow(stat, true);
            }
            
            GUILayout.Space(15);
            
            var derivedStats = testContainer.GetDerivedStats();
            if (derivedStats.Count > 0)
            {
                GUILayout.Label("Derived Stats", EditorStyles.boldLabel);
                
                foreach (var stat in derivedStats)
                {
                    DrawTestStatRow(stat, false);
                }
            }
            
            GUILayout.Space(15);
            
            var externalStats = testContainer.GetExternalStats();
            if (externalStats.Count > 0)
            {
                GUILayout.Label("External Stats", EditorStyles.boldLabel);
                
                foreach (var stat in externalStats)
                {
                    DrawTestStatRow(stat, false);
                }
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawTestStatRow(StatValue stat, bool allowAllocation)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label(stat.statType.DisplayName, EditorStyles.boldLabel, GUILayout.Width(120));
            
            var currentValue = testContainer.GetStatValue(stat.statType);
            testTempBonuses.TryGetValue(stat.statType, out float tempBonus);
            currentValue += tempBonus;
            
            GUILayout.Label($"{currentValue:F1}", EditorStyles.boldLabel, GUILayout.Width(40));
            
            var details = $"(Base: {stat.baseValue:F1}";
            if (stat.allocatedPoints > 0)
                details += $" + Alloc: {stat.allocatedPoints:F1}";
            if (stat.bonusValue != 0)
                details += $" + Bonus: {stat.bonusValue:F1}";
            if (tempBonus != 0)
                details += $" + Temp: {tempBonus:F1}";
            details += ")";
            
            GUILayout.Label(details, EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            if (allowAllocation)
            {
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    stat.SetAllocatedPoints(stat.allocatedPoints + 1f);
                }
                
                if (GUILayout.Button("-", GUILayout.Width(25)) && stat.allocatedPoints > 0)
                {
                    stat.SetAllocatedPoints(stat.allocatedPoints - 1f);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Temp Bonus:", GUILayout.Width(80));
            var newTempBonus = EditorGUILayout.FloatField(tempBonus, GUILayout.Width(50));
            if (!Mathf.Approximately(newTempBonus, tempBonus))
            {
                if (Mathf.Approximately(newTempBonus, 0f))
                    testTempBonuses.Remove(stat.statType);
                else
                    testTempBonuses[stat.statType] = newTempBonus;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }
        
        private void DrawCreateStatTypeForm()
        {
            DrawFormField("Display Name", () => {
                newName = EditorGUILayout.TextField(newName);
            });
            
            DrawFormField("Short Name", () => {
                EditorGUILayout.BeginHorizontal();
                newShortName = EditorGUILayout.TextField(newShortName, GUILayout.Width(100));
                if (GUILayout.Button("Auto", GUILayout.Width(50)))
                {
                    newShortName = GenerateShortName(newName);
                }
                EditorGUILayout.EndHorizontal();
            });
            
            DrawFormField("Category", () => {
                newCategory = (StatCategory)EditorGUILayout.EnumPopup(newCategory);
            });
            
            DrawFormField("Default Value", () => {
                newDefaultValue = EditorGUILayout.FloatField(newDefaultValue);
            });
            
            DrawFormField("Min Value", () => {
                newMinValue = EditorGUILayout.FloatField(newMinValue);
            });
            
            DrawFormField("Max Value", () => {
                newMaxValue = EditorGUILayout.FloatField(newMaxValue);
            });
            
            if (newCategory == StatCategory.Derived)
            {
                DrawFormField("Formula", () => {
                    newFormula = EditorGUILayout.TextArea(newFormula, GUILayout.Height(60));
                });
            }
        }
        
        private void DrawStatTypeEditor()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedStat.DisplayName}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Stat Type", 
                    $"Are you sure you want to delete '{selectedStat.DisplayName}'?", "Delete", "Cancel"))
                {
                    DeleteStatType(selectedStat);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);
            
            DrawFormField("Display Name", () => {
                var newDisplayName = EditorGUILayout.TextField(selectedStat.DisplayName);
                if (newDisplayName != selectedStat.DisplayName)
                {
                    selectedStat.DisplayName = newDisplayName;
                    EditorUtility.SetDirty(selectedStat);
                }
            });
            
            DrawFormField("Short Name", () => {
                EditorGUILayout.BeginHorizontal();
                var shortName = EditorGUILayout.TextField(selectedStat.ShortName ?? "", GUILayout.Width(100));
                if (shortName != selectedStat.ShortName)
                {
                    selectedStat.ShortName = shortName;
                    EditorUtility.SetDirty(selectedStat);
                }
                if (GUILayout.Button("Auto", GUILayout.Width(50)))
                {
                    selectedStat.ShortName = GenerateShortName(selectedStat.DisplayName);
                    EditorUtility.SetDirty(selectedStat);
                }
                EditorGUILayout.EndHorizontal();
            });
            
            DrawFormField("Category", () => {
                var category = (StatCategory)EditorGUILayout.EnumPopup(selectedStat.Category);
                if (category != selectedStat.Category)
                {
                    selectedStat.Category = category;
                    if (category != StatCategory.Derived)
                        selectedStat.Formula = "";
                    EditorUtility.SetDirty(selectedStat);
                }
            });
            
            DrawFormField("Default Value", () => {
                var newDefault = EditorGUILayout.FloatField(selectedStat.DefaultValue);
                if (!Mathf.Approximately(newDefault, selectedStat.DefaultValue))
                {
                    selectedStat.DefaultValue = newDefault;
                    EditorUtility.SetDirty(selectedStat);
                }
            });
            
            DrawFormField("Min Value", () => {
                var newMin = EditorGUILayout.FloatField(selectedStat.MinValue);
                if (!Mathf.Approximately(newMin, selectedStat.MinValue))
                {
                    selectedStat.MinValue = newMin;
                    EditorUtility.SetDirty(selectedStat);
                }
            });
            
            DrawFormField("Max Value", () => {
                var newMax = EditorGUILayout.FloatField(selectedStat.MaxValue);
                if (!Mathf.Approximately(newMax, selectedStat.MaxValue))
                {
                    selectedStat.MaxValue = newMax;
                    EditorUtility.SetDirty(selectedStat);
                }
            });
            
            if (selectedStat.Category == StatCategory.Derived)
            {
                DrawFormField("Formula", () => {
                    var formula = EditorGUILayout.TextArea(selectedStat.Formula ?? "", GUILayout.Height(60));
                    if (formula != selectedStat.Formula)
                    {
                        selectedStat.Formula = formula;
                        EditorUtility.SetDirty(selectedStat);
                    }
                });
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawContainerEditor()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {selectedContainer.ContainerName}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Container", 
                    $"Are you sure you want to delete '{selectedContainer.ContainerName}'?", "Delete", "Cancel"))
                {
                    DeleteContainer(selectedContainer);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);
            
            DrawFormField("Container Name", () => {
                var containerName = EditorGUILayout.TextField(selectedContainer.ContainerName);
                if (containerName != selectedContainer.ContainerName)
                {
                    selectedContainer.ContainerName = containerName;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });
            
            DrawFormField("Category", () => {
                var category = (ContainerCategory)EditorGUILayout.EnumPopup(selectedContainer.Category);
                if (category != selectedContainer.Category)
                {
                    selectedContainer.Category = category;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });
            
            DrawFormField("Description", () => {
                var newDesc = EditorGUILayout.TextArea(selectedContainer.Description, GUILayout.Height(40));
                if (newDesc != selectedContainer.Description)
                {
                    selectedContainer.Description = newDesc;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });
            
            GUILayout.Space(20);
            
            GUILayout.Label("Stats in Container", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Stat:", GUILayout.Width(70));
            
            var availableStats = allStats.Where(s => !selectedContainer.HasStat(s)).ToArray();
            var statNames = availableStats.Select(s => s.DisplayName).ToArray();
            
            if (availableStats.Length > 0)
            {
                var selectedIndex = EditorGUILayout.Popup(0, statNames);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    selectedContainer.AddStat(availableStats[selectedIndex]);
                    EditorUtility.SetDirty(selectedContainer);
                }
            }
            else
            {
                GUILayout.Label("All stats already added", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            foreach (var stat in selectedContainer.Stats.ToList())
            {
                if (stat.statType == null) continue;
                
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label(stat.statType.DisplayName, EditorStyles.boldLabel);
                GUILayout.Label($"Base: {stat.baseValue:F1}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                GUILayout.Label("Base:", GUILayout.Width(35));
                var newBase = EditorGUILayout.FloatField(stat.baseValue, GUILayout.Width(60));
                if (!Mathf.Approximately(newBase, stat.baseValue))
                {
                    stat.SetBaseValue(newBase);
                    EditorUtility.SetDirty(selectedContainer);
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    selectedContainer.RemoveStat(stat.statType);
                    EditorUtility.SetDirty(selectedContainer);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawWelcomePanel()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label("StatForge Manager", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            string helpText = "";
            switch (currentView)
            {
                case ViewMode.StatTypes:
                    helpText = "Select a stat type from the left panel to edit it";
                    break;
                case ViewMode.Containers:
                    helpText = "Select a container from the left panel to edit it";
                    break;
                case ViewMode.Templates:
                    helpText = "Select a template from the left panel to edit it";
                    break;
                case ViewMode.Tests:
                    helpText = "Select a container from the left panel to test it";
                    break;
            }
            
            GUILayout.Label(helpText, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("or click + to create a new one", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
        }
        
        private void DrawFormField(string label, System.Action fieldDrawer)
        {
            GUILayout.Space(10);
            GUILayout.Label(label, EditorStyles.boldLabel);
            GUILayout.Space(5);
            fieldDrawer();
        }
        
        private List<StatType> GetFilteredStats()
        {
            var filtered = allStats.AsEnumerable();
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filtered = filtered.Where(s => s.DisplayName.ToLower().Contains(searchFilter.ToLower()) ||
                                              (s.ShortName != null && s.ShortName.ToLower().Contains(searchFilter.ToLower())));
            }
            
            if (filterCategory != (StatCategory)(-1))
            {
                filtered = filtered.Where(s => s.Category == filterCategory);
            }
            
            return filtered.OrderBy(s => s.Category).ThenBy(s => s.DisplayName).ToList();
        }
        
        private List<StatContainer> GetFilteredContainers()
        {
            var filtered = allContainers.AsEnumerable();
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filtered = filtered.Where(c => c.ContainerName.ToLower().Contains(searchFilter.ToLower()));
            }
            
            if (filterContainerCategory != (ContainerCategory)(-1))
            {
                filtered = filtered.Where(c => c.Category == filterContainerCategory);
            }
            
            return filtered.OrderBy(c => c.Category).ThenBy(c => c.ContainerName).ToList();
        }
        
        private Color GetCategoryColor(ContainerCategory category)
        {
            switch (category)
            {
                case ContainerCategory.Base:
                    return Color.white;
                case ContainerCategory.Entity:
                    return Color.green;
                case ContainerCategory.Classe:
                    return Color.red;
                case ContainerCategory.Item:
                    return Color.blue;
                case ContainerCategory.Skill:
                    return Color.yellow;
                default:
                    return Color.gray;
            }
        }
        
        private string GenerateShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";
            
            var words = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var shortName = "";
            
            foreach (var word in words)
            {
                if (word.Length > 0)
                    shortName += word[0].ToString().ToUpper();
            }
            
            return shortName.Length > 5 ? shortName.Substring(0, 5) : shortName;
        }
        
        private void StartCreatingNew()
        {
            isCreatingNew = true;
            selectedStat = null;
            selectedContainer = null;
            selectedTemplate = null;
            
            newName = "";
            newShortName = "";
            newCategory = StatCategory.Primary;
            newContainerCategory = ContainerCategory.Base;
            newFormula = "";
            newDefaultValue = 0f;
            newMinValue = 0f;
            newMaxValue = 100f;
            newDescription = "";
            newTemplateStats.Clear();
        }
        
        private void CancelCreation()
        {
            isCreatingNew = false;
        }
        
        private void ClearSelection()
        {
            selectedStat = null;
            selectedContainer = null;
            selectedTemplate = null;
            isCreatingNew = false;
        }
        
        private void InitializeTest(StatContainer container)
        {
            testContainer = ScriptableObject.Instantiate(container);
            testContainer.Initialize();
            testInitialized = true;
            testTempBonuses.Clear();
        }
        
        private void ResetTest()
        {
            if (testContainer != null)
            {
                foreach (var stat in testContainer.GetPrimaryStats())
                {
                    stat.SetAllocatedPoints(0f);
                }
                testTempBonuses.Clear();
            }
        }
        
        private void CreateStatType()
        {
            var asset = CreateInstance<StatType>();
            asset.DisplayName = newName;
            asset.ShortName = newShortName;
            asset.Category = newCategory;
            asset.DefaultValue = newDefaultValue;
            asset.MinValue = newMinValue;
            asset.MaxValue = newMaxValue;
            if (newCategory == StatCategory.Derived)
                asset.Formula = newFormula;
            
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{newName.Replace(" ", "")}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            selectedStat = asset;
            isCreatingNew = false;
        }
        
        private void CreateContainer()
        {
            var asset = CreateInstance<StatContainer>();
            asset.ContainerName = newName;
            asset.Category = newContainerCategory;
            asset.Description = newDescription;
            
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{newName.Replace(" ", "")}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            selectedContainer = asset;
            isCreatingNew = false;
        }
        
        private void CreateTemplate()
        {
            var asset = CreateInstance<ContainerTemplate>();
            asset.templateName = newName;
            asset.description = newDescription;
            asset.statTypes = new List<StatType>(newTemplateStats);
            
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{newName.Replace(" ", "")}Template.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            selectedTemplate = asset;
            isCreatingNew = false;
        }
        
        private void CreateContainerFromTemplate(ContainerTemplate template)
        {
            var asset = CreateInstance<StatContainer>();
            asset.ContainerName = $"{template.templateName} Container";
            asset.Category = newContainerCategory;
            asset.Description = newDescription;
            
            foreach (var statType in template.statTypes)
            {
                if (statType != null)
                {
                    asset.AddStat(statType, statType.DefaultValue);
                }
            }
            
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{asset.ContainerName.Replace(" ", "")}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            selectedContainer = asset;
            isCreatingNew = false;
            currentView = ViewMode.Containers;
        }
        
        private void DeleteStatType(StatType stat)
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
        
        private void DeleteTemplate(ContainerTemplate template)
        {
            var path = AssetDatabase.GetAssetPath(template);
            AssetDatabase.DeleteAsset(path);
            RefreshData();
            selectedTemplate = null;
        }
        
        private void RefreshData()
        {
            var statGuids = AssetDatabase.FindAssets("t:StatType");
            allStats = statGuids.Select(guid => AssetDatabase.LoadAssetAtPath<StatType>(AssetDatabase.GUIDToAssetPath(guid)))
                               .Where(stat => stat != null)
                               .ToList();
            
            var containerGuids = AssetDatabase.FindAssets("t:StatContainer");
            allContainers = containerGuids.Select(guid => AssetDatabase.LoadAssetAtPath<StatContainer>(AssetDatabase.GUIDToAssetPath(guid)))
                                         .Where(container => container != null)
                                         .ToList();
            
            var templateGuids = AssetDatabase.FindAssets("t:ContainerTemplate");
            allTemplates = templateGuids.Select(guid => AssetDatabase.LoadAssetAtPath<ContainerTemplate>(AssetDatabase.GUIDToAssetPath(guid)))
                                       .Where(template => template != null)
                                       .ToList();
            
            Repaint();
        }
    }
}
#endif