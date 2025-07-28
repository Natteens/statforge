#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeManager : EditorWindow
    {
        private List<StatContainer> allContainers = new();

        private List<StatType> allStats = new();
        private List<ContainerTemplate> allTemplates = new();
        private string containersPath = "Assets/StatForge/Containers";
        private ViewMode currentView = ViewMode.StatTypes;
        private StatCategory filterCategory = (StatCategory)(-1);
        private ContainerCategory filterContainerCategory = (ContainerCategory)(-1);

        private bool isCreatingNew;

        private Vector2 leftPanelScroll;
        private StatCategory newCategory = StatCategory.Primary;
        private ContainerCategory newContainerCategory = ContainerCategory.Base;
        private float newDefaultValue;
        private string newDescription = "";
        private string newFormula = "";
        private float newMaxValue = 100f;
        private float newMinValue;
        private string newName = "";
        private string newShortName = "";
        private List<StatType> newTemplateStats = new();
        private Vector2 rightPanelScroll;
        private string searchFilter = "";
        private StatContainer selectedContainer;

        private StatType selectedStat;
        private ContainerTemplate selectedTemplate;

        private string statTypesPath = "Assets/StatForge/StatTypes";
        private string templatesPath = "Assets/StatForge/Templates";

        private StatContainer testContainer;
        private bool testInitialized;
        private Dictionary<StatType, float> testTempBonuses = new();

        private void OnEnable()
        {
            RefreshData();
            LoadSettings();
        }

        private void OnGUI()
        {
            try
            {
                DrawToolbar();

                var contentRect = new Rect(0, 40, position.width, position.height - 40);

                var leftRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.3f, contentRect.height);
                DrawLeftPanel(leftRect);

                var rightRect = new Rect(leftRect.xMax, contentRect.y, contentRect.width * 0.7f, contentRect.height);
                DrawRightPanel(rightRect);
            }
            catch (Exception e)
            {
                Debug.LogError($"StatForge Manager GUI Error: {e.Message}");
                ClearSelection();
                Repaint();
            }
        }

        [MenuItem("Tools/StatForge/Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeManager>("StatForge Manager");
            window.minSize = new Vector2(1200f, 700f);
            window.Show();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var toolbarButtons = new[]
            {
                new { Mode = ViewMode.StatTypes, Label = "Stat Types" },
                new { Mode = ViewMode.Containers, Label = "Containers" },
                new { Mode = ViewMode.Templates, Label = "Templates" },
                new { Mode = ViewMode.Tests, Label = "Tests" },
                new { Mode = ViewMode.Settings, Label = "Settings" }
            };

            foreach (var button in toolbarButtons)
                if (GUILayout.Toggle(currentView == button.Mode, button.Label, EditorStyles.toolbarButton))
                    if (currentView != button.Mode)
                    {
                        currentView = button.Mode;
                        ClearSelection();
                    }

            GUILayout.Space(20);

            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.Space(10);

            DrawCategoryFilter();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) RefreshData();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryFilter()
        {
            if (currentView == ViewMode.StatTypes)
            {
                GUILayout.Label("Category:", GUILayout.Width(60));
                var categories = new[] { "All", "Primary", "Derived", "External" };
                var selectedIndex = (int)filterCategory + 1;
                var newIndex = EditorGUILayout.Popup(selectedIndex, categories, EditorStyles.toolbarPopup,
                    GUILayout.Width(100));
                filterCategory = (StatCategory)(newIndex - 1);
            }
            else if (currentView == ViewMode.Containers)
            {
                GUILayout.Label("Category:", GUILayout.Width(60));
                var categories = new[] { "All", "Base", "Equipment", "Character", "Skill", "Buff", "Debuff" };
                var selectedIndex = (int)filterContainerCategory + 1;
                var newIndex = EditorGUILayout.Popup(selectedIndex, categories, EditorStyles.toolbarPopup,
                    GUILayout.Width(100));
                filterContainerCategory = (ContainerCategory)(newIndex - 1);
            }
        }

        private void DrawLeftPanel(Rect rect)
        {
            try
            {
                GUILayout.BeginArea(rect, EditorStyles.helpBox);

                DrawLeftPanelHeader();
                DrawLeftPanelContent();

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                Debug.LogError($"Left Panel Error: {e.Message}");
                GUILayout.EndArea();
            }
        }

        private void DrawLeftPanelHeader()
        {
            EditorGUILayout.BeginHorizontal();

            var (Title, count) = GetPanelTitleAndCount();
            GUILayout.Label($"{Title} ({count})", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (currentView != ViewMode.Tests && currentView != ViewMode.Settings &&
                GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
                StartCreatingNew();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private (string title, int count) GetPanelTitleAndCount()
        {
            return currentView switch
            {
                ViewMode.StatTypes => ("Stat Types", GetFilteredStats().Count),
                ViewMode.Containers => ("Containers", GetFilteredContainers().Count),
                ViewMode.Templates => ("Templates", allTemplates?.Count ?? 0),
                ViewMode.Tests => ("Test Environment", allContainers?.Count ?? 0),
                ViewMode.Settings => ("Settings", 0),
                _ => ("Unknown", 0)
            };
        }

        private void DrawLeftPanelContent()
        {
            leftPanelScroll = GUILayout.BeginScrollView(leftPanelScroll);

            try
            {
                switch (currentView)
                {
                    case ViewMode.StatTypes:
                        DrawItemsList(GetFilteredStats(), DrawStatTypeItem, ref selectedStat);
                        break;
                    case ViewMode.Containers:
                        DrawItemsList(GetFilteredContainers(), DrawContainerItem, ref selectedContainer);
                        break;
                    case ViewMode.Templates:
                        DrawItemsList(allTemplates ?? new List<ContainerTemplate>(), DrawTemplateItem,
                            ref selectedTemplate);
                        break;
                    case ViewMode.Tests:
                        DrawTestsList();
                        break;
                    case ViewMode.Settings:
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Left Panel Content Error: {e.Message}");
            }

            GUILayout.EndScrollView();
        }

        private void DrawItemsList<T>(List<T> items, Action<T> drawItem, ref T selectedItem) where T : class
        {
            if (items == null) return;

            foreach (var item in items)
            {
                if (item == null) continue;

                var isSelected = selectedItem == item;

                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");

                drawItem(item);

                EditorGUILayout.EndVertical();

                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    selectedItem = item;
                    isCreatingNew = false;
                    Repaint();
                    Event.current.Use();
                }
            }
        }

        private void DrawStatTypeItem(StatType stat)
        {
            if (stat == null) return;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(stat.DisplayName ?? "Unnamed Stat", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
            categoryStyle.normal.textColor = GetStatCategoryColor(stat.Category);
            GUILayout.Label(stat.Category.ToString(), categoryStyle);

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(stat.ShortName)) GUILayout.Label($"({stat.ShortName})", EditorStyles.miniLabel);
        }

        private void DrawContainerItem(StatContainer container)
        {
            if (container == null) return;

            GUILayout.Label(container.ContainerName ?? "Unnamed Container", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{container.Stats?.Count ?? 0} stats", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
            categoryStyle.normal.textColor = GetContainerCategoryColor(container.Category);
            GUILayout.Label(container.Category.ToString(), categoryStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTemplateItem(ContainerTemplate template)
        {
            if (template == null) return;

            GUILayout.Label(template.templateName ?? "Unnamed Template", EditorStyles.boldLabel);
            GUILayout.Label($"{template.statTypes?.Count ?? 0} stats", EditorStyles.miniLabel);
        }

        private void DrawTestsList()
        {
            GUILayout.Label("Available Containers for Testing:", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (allContainers == null) return;

            foreach (var container in allContainers)
            {
                if (container == null) continue;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(container.ContainerName ?? "Unnamed Container", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Test", GUILayout.Width(60))) InitializeTest(container);

                EditorGUILayout.EndHorizontal();

                GUILayout.Label($"Category: {container.Category}", EditorStyles.miniLabel);
                GUILayout.Label($"Stats: {container.Stats?.Count ?? 0}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRightPanel(Rect rect)
        {
            try
            {
                GUILayout.BeginArea(rect, EditorStyles.helpBox);

                if (isCreatingNew)
                    DrawCreateNewPanel();
                else
                    DrawRightPanelContent();

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                Debug.LogError($"Right Panel Error: {e.Message}");
                GUILayout.EndArea();
            }
        }

        private void DrawRightPanelContent()
        {
            switch (currentView)
            {
                case ViewMode.StatTypes when selectedStat != null:
                    DrawEditor("Stat Type", selectedStat.DisplayName, DrawStatTypeEditor,
                        () => DeleteStatType(selectedStat));
                    break;
                case ViewMode.Containers when selectedContainer != null:
                    DrawEditor("Container", selectedContainer.ContainerName, DrawContainerEditor,
                        () => DeleteContainer(selectedContainer));
                    break;
                case ViewMode.Templates when selectedTemplate != null:
                    DrawEditor("Template", selectedTemplate.templateName, DrawTemplateEditor,
                        () => DeleteTemplate(selectedTemplate));
                    break;
                case ViewMode.Tests:
                    DrawTestPanel();
                    break;
                case ViewMode.Settings:
                    DrawSettingsPanel();
                    break;
                default:
                    DrawWelcomePanel();
                    break;
            }
        }

        private void DrawEditor(string itemType, string itemName, Action editorDrawer, Action deleteAction)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Edit: {itemName ?? $"Unnamed {itemType}"}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(25)))
                if (EditorUtility.DisplayDialog($"Delete {itemType}",
                        $"Are you sure you want to delete '{itemName ?? $"Unnamed {itemType}"}'?", "Delete", "Cancel"))
                    try
                    {
                        deleteAction?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error deleting {itemType}: {e.Message}");
                    }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);

            try
            {
                editorDrawer?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"{itemType} Editor Error: {e.Message}");
            }

            GUILayout.EndScrollView();
        }

        private void DrawCreateNewPanel()
        {
            var itemType = currentView switch
            {
                ViewMode.StatTypes => "Stat Type",
                ViewMode.Containers => "Container",
                ViewMode.Templates => "Template",
                _ => "Item"
            };

            GUILayout.Label($"Create New {itemType}", EditorStyles.boldLabel);
            GUILayout.Space(20);

            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);

            try
            {
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
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Form Error: {e.Message}");
            }

            GUILayout.EndScrollView();

            DrawCreateButtons();
        }

        private void DrawCreateButtons()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30))) CancelCreation();

            GUILayout.Space(10);

            GUI.enabled = !string.IsNullOrEmpty(newName);
            if (GUILayout.Button("Create", GUILayout.Width(80), GUILayout.Height(30)))
                try
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
                catch (Exception e)
                {
                    Debug.LogError($"Creation Error: {e.Message}");
                }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreateStatTypeForm()
        {
            DrawFormField("Display Name", () => { newName = EditorGUILayout.TextField(newName ?? ""); });

            DrawFormField("Short Name", () =>
            {
                EditorGUILayout.BeginHorizontal();
                newShortName = EditorGUILayout.TextField(newShortName ?? "", GUILayout.Width(100));
                if (GUILayout.Button("Auto", GUILayout.Width(50))) newShortName = GenerateShortName(newName);
                EditorGUILayout.EndHorizontal();
            });

            DrawFormField("Category", () => { newCategory = (StatCategory)EditorGUILayout.EnumPopup(newCategory); });

            DrawFormField("Default Value", () => { newDefaultValue = EditorGUILayout.FloatField(newDefaultValue); });

            DrawFormField("Min Value", () => { newMinValue = EditorGUILayout.FloatField(newMinValue); });

            DrawFormField("Max Value", () => { newMaxValue = EditorGUILayout.FloatField(newMaxValue); });

            if (newCategory == StatCategory.Derived)
                DrawFormField("Formula",
                    () => { newFormula = EditorGUILayout.TextArea(newFormula ?? "", GUILayout.Height(60)); });
        }

        private void DrawCreateContainerForm()
        {
            DrawFormField("Container Name", () => { newName = EditorGUILayout.TextField(newName ?? ""); });

            DrawFormField("Category",
                () => { newContainerCategory = (ContainerCategory)EditorGUILayout.EnumPopup(newContainerCategory); });

            DrawFormField("Description",
                () => { newDescription = EditorGUILayout.TextArea(newDescription ?? "", GUILayout.Height(60)); });

            if (allTemplates?.Count > 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("Create from Template", EditorStyles.boldLabel);
                GUILayout.Space(10);

                try
                {
                    foreach (var template in allTemplates)
                    {
                        if (template == null) continue;

                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical();
                        GUILayout.Label(template.templateName ?? "Unnamed Template", EditorStyles.boldLabel);
                        GUILayout.Label($"{template.statTypes?.Count ?? 0} stats", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Use", GUILayout.Width(50)))
                            try
                            {
                                CreateContainerFromTemplate(template);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                return;
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Error using template: {e.Message}");
                            }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(5);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error drawing templates: {e.Message}");
                }
            }
        }

        private void DrawCreateTemplateForm()
        {
            DrawFormField("Template Name", () => { newName = EditorGUILayout.TextField(newName ?? ""); });

            DrawFormField("Description",
                () => { newDescription = EditorGUILayout.TextArea(newDescription ?? "", GUILayout.Height(60)); });

            GUILayout.Space(20);

            GUILayout.Label("Template Stats", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Stat:", GUILayout.Width(70));

            if (allStats != null && newTemplateStats != null)
            {
                var availableStats = allStats.Where(s => s != null && !newTemplateStats.Contains(s)).ToArray();
                var statNames = availableStats.Select(s => s.DisplayName ?? "Unnamed Stat").ToArray();

                if (availableStats.Length > 0)
                {
                    var selectedIndex = EditorGUILayout.Popup(0, statNames);
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                        newTemplateStats.Add(availableStats[selectedIndex]);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (newTemplateStats != null)
                for (var i = newTemplateStats.Count - 1; i >= 0; i--)
                {
                    if (i >= newTemplateStats.Count || newTemplateStats[i] == null) continue;

                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label(newTemplateStats[i].DisplayName ?? "Unnamed Stat");
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("×", GUILayout.Width(20))) newTemplateStats.RemoveAt(i);

                    EditorGUILayout.EndHorizontal();
                }
        }

        private void DrawStatTypeEditor()
        {
            if (selectedStat == null) return;

            DrawFormField("Display Name", () =>
            {
                var newDisplayName = EditorGUILayout.TextField(selectedStat.DisplayName ?? "");
                if (newDisplayName != selectedStat.DisplayName)
                {
                    selectedStat.DisplayName = newDisplayName;
                    EditorUtility.SetDirty(selectedStat);
                }
            });

            DrawFormField("Short Name", () =>
            {
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

            DrawFormField("Category", () =>
            {
                var category = (StatCategory)EditorGUILayout.EnumPopup(selectedStat.Category);
                if (category != selectedStat.Category)
                {
                    selectedStat.Category = category;
                    if (category != StatCategory.Derived)
                        selectedStat.Formula = "";
                    EditorUtility.SetDirty(selectedStat);
                }
            });

            DrawFormField("Default Value", () =>
            {
                var newDefault = EditorGUILayout.FloatField(selectedStat.DefaultValue);
                if (!Mathf.Approximately(newDefault, selectedStat.DefaultValue))
                {
                    selectedStat.DefaultValue = newDefault;
                    EditorUtility.SetDirty(selectedStat);
                }
            });

            DrawFormField("Min Value", () =>
            {
                var newMin = EditorGUILayout.FloatField(selectedStat.MinValue);
                if (!Mathf.Approximately(newMin, selectedStat.MinValue))
                {
                    selectedStat.MinValue = newMin;
                    EditorUtility.SetDirty(selectedStat);
                }
            });

            DrawFormField("Max Value", () =>
            {
                var newMax = EditorGUILayout.FloatField(selectedStat.MaxValue);
                if (!Mathf.Approximately(newMax, selectedStat.MaxValue))
                {
                    selectedStat.MaxValue = newMax;
                    EditorUtility.SetDirty(selectedStat);
                }
            });

            if (selectedStat.Category == StatCategory.Derived)
                DrawFormField("Formula", () =>
                {
                    var formula = EditorGUILayout.TextArea(selectedStat.Formula ?? "", GUILayout.Height(60));
                    if (formula != selectedStat.Formula)
                    {
                        selectedStat.Formula = formula;
                        EditorUtility.SetDirty(selectedStat);
                    }
                });
        }

        private void DrawContainerEditor()
        {
            if (selectedContainer == null) return;

            DrawFormField("Container Name", () =>
            {
                var containerName = EditorGUILayout.TextField(selectedContainer.ContainerName ?? "");
                if (containerName != selectedContainer.ContainerName)
                {
                    selectedContainer.ContainerName = containerName;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });

            DrawFormField("Category", () =>
            {
                var category = (ContainerCategory)EditorGUILayout.EnumPopup(selectedContainer.Category);
                if (category != selectedContainer.Category)
                {
                    selectedContainer.Category = category;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });

            DrawFormField("Description", () =>
            {
                var newDesc = EditorGUILayout.TextArea(selectedContainer.Description ?? "", GUILayout.Height(40));
                if (newDesc != selectedContainer.Description)
                {
                    selectedContainer.Description = newDesc;
                    EditorUtility.SetDirty(selectedContainer);
                }
            });

            GUILayout.Space(20);

            GUILayout.Label("Stats in Container", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Garantir que o container está inicializado
            if (selectedContainer.Stats == null)
            {
                selectedContainer.Initialize();
                EditorUtility.SetDirty(selectedContainer);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Stat:", GUILayout.Width(70));

            if (allStats != null && allStats.Count > 0 && selectedContainer.Stats != null)
            {
                var availableStats = allStats.Where(s => s != null && !selectedContainer.HasStat(s)).ToArray();

                if (availableStats.Length > 0)
                {
                    var statNames = availableStats.Select(s => s.DisplayName ?? "Unnamed Stat").ToArray();
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
            }
            else
            {
                if (allStats == null || allStats.Count == 0)
                    GUILayout.Label("No stat types available", EditorStyles.miniLabel);
                else
                    GUILayout.Label("Container not initialized", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            if (selectedContainer.Stats != null && selectedContainer.Stats.Count > 0)
            {
                var statsToRemove = new List<StatValue>();

                foreach (var stat in selectedContainer.Stats)
                {
                    if (stat?.statType == null)
                    {
                        statsToRemove.Add(stat);
                        continue;
                    }

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(stat.statType.DisplayName ?? "Unnamed Stat", EditorStyles.boldLabel);
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

                    if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20))) statsToRemove.Add(stat);

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                foreach (var statToRemove in statsToRemove)
                {
                    if (statToRemove?.statType != null)
                        selectedContainer.RemoveStat(statToRemove.statType);
                    else
                        selectedContainer.Stats.Remove(statToRemove);
                    EditorUtility.SetDirty(selectedContainer);
                }
            }
            else
            {
                GUILayout.Label("No stats in container", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawTemplateEditor()
        {
            if (selectedTemplate == null) return;

            DrawFormField("Template Name", () =>
            {
                var newTemplateName = EditorGUILayout.TextField(selectedTemplate.templateName ?? "");
                if (newTemplateName != selectedTemplate.templateName)
                {
                    selectedTemplate.templateName = newTemplateName;
                    EditorUtility.SetDirty(selectedTemplate);
                }
            });

            DrawFormField("Description", () =>
            {
                var newDesc = EditorGUILayout.TextArea(selectedTemplate.description ?? "", GUILayout.Height(60));
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

            if (allStats != null && selectedTemplate.statTypes != null)
            {
                var availableStats =
                    allStats.Where(s => s != null && !selectedTemplate.statTypes.Contains(s)).ToArray();
                var statNames = availableStats.Select(s => s.DisplayName ?? "Unnamed Stat").ToArray();

                if (availableStats.Length > 0)
                {
                    var selectedIndex = EditorGUILayout.Popup(0, statNames);
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                    {
                        selectedTemplate.statTypes.Add(availableStats[selectedIndex]);
                        EditorUtility.SetDirty(selectedTemplate);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            if (selectedTemplate.statTypes != null)
                for (var i = selectedTemplate.statTypes.Count - 1; i >= 0; i--)
                {
                    if (i >= selectedTemplate.statTypes.Count) continue;

                    var stat = selectedTemplate.statTypes[i];
                    if (stat == null)
                    {
                        selectedTemplate.statTypes.RemoveAt(i);
                        EditorUtility.SetDirty(selectedTemplate);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal("box");

                    GUILayout.Label(stat.DisplayName ?? "Unnamed Stat", EditorStyles.boldLabel);
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
                CreateContainerFromTemplate(selectedTemplate);
        }

        private void DrawTestPanel()
        {
            if (!testInitialized || testContainer == null)
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical();
                GUILayout.Label("Test Environment", EditorStyles.largeLabel);
                GUILayout.Space(10);
                GUILayout.Label("Select a container from the left panel to start testing",
                    EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                return;
            }

            GUILayout.Label($"Testing: {testContainer.ContainerName ?? "Unnamed Container"}", EditorStyles.boldLabel);

            GUILayout.Space(20);

            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Test Controls:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Test", GUILayout.Width(80))) ResetTest();

            if (GUILayout.Button("Clear Bonuses", GUILayout.Width(100))) testTempBonuses.Clear();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            try
            {
                GUILayout.Label("Primary Stats", EditorStyles.boldLabel);
                var primaryStats = testContainer.GetPrimaryStats();
                if (primaryStats != null)
                    foreach (var stat in primaryStats)
                        if (stat != null)
                            DrawTestStatRow(stat, true);

                GUILayout.Space(15);

                var derivedStats = testContainer.GetDerivedStats();
                if (derivedStats?.Count > 0)
                {
                    GUILayout.Label("Derived Stats", EditorStyles.boldLabel);

                    foreach (var stat in derivedStats)
                        if (stat != null)
                            DrawTestStatRow(stat, false);
                }

                GUILayout.Space(15);

                var externalStats = testContainer.GetExternalStats();
                if (externalStats?.Count > 0)
                {
                    GUILayout.Label("External Stats", EditorStyles.boldLabel);

                    foreach (var stat in externalStats)
                        if (stat != null)
                            DrawTestStatRow(stat, false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Test Panel Stats Error: {e.Message}");
                GUILayout.Label("Error displaying stats", EditorStyles.miniLabel);
            }

            GUILayout.EndScrollView();
        }

        private void DrawTestStatRow(StatValue stat, bool allowAllocation)
        {
            if (stat?.statType == null) return;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(stat.statType.DisplayName ?? "Unnamed Stat", EditorStyles.boldLabel, GUILayout.Width(120));

            var currentValue = testContainer.GetStatValue(stat.statType);
            testTempBonuses.TryGetValue(stat.statType, out var tempBonus);
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
                if (GUILayout.Button("+", GUILayout.Width(25))) stat.SetAllocatedPoints(stat.allocatedPoints + 1f);

                if (GUILayout.Button("-", GUILayout.Width(25)) && stat.allocatedPoints > 0)
                    stat.SetAllocatedPoints(stat.allocatedPoints - 1f);
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

        private void DrawSettingsPanel()
        {
            GUILayout.Label("StatForge Settings", EditorStyles.boldLabel);
            GUILayout.Space(20);

            rightPanelScroll = GUILayout.BeginScrollView(rightPanelScroll);

            DrawFormField("Stat Types Path", () =>
            {
                EditorGUILayout.BeginHorizontal();
                statTypesPath = EditorGUILayout.TextField(statTypesPath ?? "Assets/StatForge/StatTypes");
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Stat Types Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                        statTypesPath = "Assets" + path.Substring(Application.dataPath.Length);
                }

                EditorGUILayout.EndHorizontal();
            });

            DrawFormField("Containers Path", () =>
            {
                EditorGUILayout.BeginHorizontal();
                containersPath = EditorGUILayout.TextField(containersPath ?? "Assets/StatForge/Containers");
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Containers Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                        containersPath = "Assets" + path.Substring(Application.dataPath.Length);
                }

                EditorGUILayout.EndHorizontal();
            });

            DrawFormField("Templates Path", () =>
            {
                EditorGUILayout.BeginHorizontal();
                templatesPath = EditorGUILayout.TextField(templatesPath ?? "Assets/StatForge/Templates");
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Templates Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                        templatesPath = "Assets" + path.Substring(Application.dataPath.Length);
                }

                EditorGUILayout.EndHorizontal();
            });

            GUILayout.Space(20);

            if (GUILayout.Button("Create Directories", GUILayout.Height(30))) CreateDirectories();

            GUILayout.Space(10);

            if (GUILayout.Button("Save Settings", GUILayout.Height(30))) SaveSettings();

            GUILayout.EndScrollView();
        }

        private void DrawWelcomePanel()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("StatForge Manager", EditorStyles.largeLabel);
            GUILayout.Space(10);

            var helpText = currentView switch
            {
                ViewMode.StatTypes => "Select a stat type from the left panel to edit it",
                ViewMode.Containers => "Select a container from the left panel to edit it",
                ViewMode.Templates => "Select a template from the left panel to edit it",
                ViewMode.Tests => "Select a container from the left panel to test it",
                ViewMode.Settings => "Configure StatForge paths and settings",
                _ => "Select an item from the left panel"
            };

            GUILayout.Label(helpText, EditorStyles.centeredGreyMiniLabel);
            if (currentView != ViewMode.Tests && currentView != ViewMode.Settings)
                GUILayout.Label("or click + to create a new one", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        private void DrawFormField(string label, Action fieldDrawer)
        {
            if (fieldDrawer == null) return;

            GUILayout.Space(10);
            GUILayout.Label(label ?? "Field", EditorStyles.boldLabel);
            GUILayout.Space(5);

            try
            {
                fieldDrawer();
            }
            catch (Exception e)
            {
                Debug.LogError($"Form Field '{label}' Error: {e.Message}");
                GUILayout.Label($"Error in field: {label}", EditorStyles.miniLabel);
            }
        }

        private List<StatType> GetFilteredStats()
        {
            if (allStats == null) return new List<StatType>();

            var filtered = allStats.Where(s => s != null).AsEnumerable();

            if (!string.IsNullOrEmpty(searchFilter))
                filtered = filtered.Where(s =>
                    (s.DisplayName != null && s.DisplayName.ToLower().Contains(searchFilter.ToLower())) ||
                    (s.ShortName != null && s.ShortName.ToLower().Contains(searchFilter.ToLower())));

            if (filterCategory != (StatCategory)(-1)) filtered = filtered.Where(s => s.Category == filterCategory);

            return filtered.OrderBy(s => s.Category).ThenBy(s => s.DisplayName ?? "").ToList();
        }

        private List<StatContainer> GetFilteredContainers()
        {
            if (allContainers == null) return new List<StatContainer>();

            var filtered = allContainers.Where(c => c != null).AsEnumerable();

            if (!string.IsNullOrEmpty(searchFilter))
                filtered = filtered.Where(c =>
                    c.ContainerName != null && c.ContainerName.ToLower().Contains(searchFilter.ToLower()));

            if (filterContainerCategory != (ContainerCategory)(-1))
                filtered = filtered.Where(c => c.Category == filterContainerCategory);

            return filtered.OrderBy(c => c.Category).ThenBy(c => c.ContainerName ?? "").ToList();
        }

        private Color GetStatCategoryColor(StatCategory category)
        {
            return category switch
            {
                StatCategory.Primary => Color.cyan,
                StatCategory.Derived => Color.green,
                StatCategory.External => Color.yellow,
                _ => Color.white
            };
        }

        private Color GetContainerCategoryColor(ContainerCategory category)
        {
            return category switch
            {
                ContainerCategory.Base => Color.white,
                ContainerCategory.Entity => Color.green,
                ContainerCategory.Classe => Color.red,
                ContainerCategory.Item => Color.blue,
                ContainerCategory.Skill => Color.yellow,
                _ => Color.gray
            };
        }

        private string GenerateShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";

            var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var shortName = "";

            foreach (var word in words)
                if (word.Length > 0)
                    shortName += word[0].ToString().ToUpper();

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

            if (newTemplateStats == null)
                newTemplateStats = new List<StatType>();
            else
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

            testContainer = null;
            testInitialized = false;
            if (testTempBonuses != null)
                testTempBonuses.Clear();
        }

        private void InitializeTest(StatContainer container)
        {
            if (container == null) return;

            try
            {
                testContainer = Instantiate(container);
                testContainer.Initialize();
                testInitialized = true;

                if (testTempBonuses == null)
                    testTempBonuses = new Dictionary<StatType, float>();
                else
                    testTempBonuses.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Test Initialization Error: {e.Message}");
                testContainer = null;
                testInitialized = false;
            }
        }

        private void ResetTest()
        {
            if (testContainer == null) return;

            try
            {
                var primaryStats = testContainer.GetPrimaryStats();
                if (primaryStats != null)
                    foreach (var stat in primaryStats)
                        if (stat != null)
                            stat.SetAllocatedPoints(0f);

                if (testTempBonuses != null)
                    testTempBonuses.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Test Reset Error: {e.Message}");
            }
        }

        private void CreateStatType()
        {
            if (string.IsNullOrEmpty(newName)) return;

            try
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

                EnsureDirectoryExists(statTypesPath);
                var path = AssetDatabase.GenerateUniqueAssetPath($"{statTypesPath}/{newName.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                RefreshData();
                selectedStat = asset;
                isCreatingNew = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Stat Type Error: {e.Message}");
            }
        }

        private void CreateContainer()
        {
            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                var asset = CreateInstance<StatContainer>();
                asset.ContainerName = newName;
                asset.Category = newContainerCategory;
                asset.Description = newDescription;

                EnsureDirectoryExists(containersPath);
                var path = AssetDatabase.GenerateUniqueAssetPath($"{containersPath}/{newName.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                RefreshData();
                selectedContainer = asset;
                isCreatingNew = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Container Error: {e.Message}");
            }
        }

        private void CreateTemplate()
        {
            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                var asset = CreateInstance<ContainerTemplate>();
                asset.templateName = newName;
                asset.description = newDescription;
                asset.statTypes = new List<StatType>(newTemplateStats ?? new List<StatType>());

                EnsureDirectoryExists(templatesPath);
                var path = AssetDatabase.GenerateUniqueAssetPath(
                    $"{templatesPath}/{newName.Replace(" ", "")}Template.asset");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                RefreshData();
                selectedTemplate = asset;
                isCreatingNew = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Template Error: {e.Message}");
            }
        }

        private void CreateContainerFromTemplate(ContainerTemplate template)
        {
            if (template == null)
            {
                Debug.LogError("Template is null");
                return;
            }

            try
            {
                var asset = CreateInstance<StatContainer>();
                asset.ContainerName = string.IsNullOrEmpty(template.templateName)
                    ? "Template Container"
                    : $"{template.templateName} Container";
                asset.Category = newContainerCategory;
                asset.Description = string.IsNullOrEmpty(newDescription) ? template.description : newDescription;

                if (asset.Stats == null) asset.Stats = new List<StatValue>();

                if (template.statTypes != null && template.statTypes.Count > 0)
                    foreach (var statType in template.statTypes)
                        if (statType != null)
                        {
                            var statValue = new StatValue(statType, statType.DefaultValue);
                            asset.Stats.Add(statValue);
                        }

                asset.Initialize();

                EnsureDirectoryExists(containersPath);
                var safeName = asset.ContainerName.Replace(" ", "").Replace("/", "").Replace("\\", "");
                var path = AssetDatabase.GenerateUniqueAssetPath($"{containersPath}/{safeName}.asset");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                RefreshData();
                selectedContainer = asset;
                isCreatingNew = false;
                currentView = ViewMode.Containers;

                Debug.Log(
                    $"Container '{asset.ContainerName}' created from template '{template.templateName}' with {template.statTypes?.Count ?? 0} stats");
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Container from Template Error: {e.Message}\nStack: {e.StackTrace}");
            }
        }

        private void DeleteStatType(StatType stat)
        {
            if (stat == null) return;

            try
            {
                var path = AssetDatabase.GetAssetPath(stat);
                AssetDatabase.DeleteAsset(path);
                RefreshData();
                selectedStat = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Delete Stat Type Error: {e.Message}");
            }
        }

        private void DeleteContainer(StatContainer container)
        {
            if (container == null) return;

            try
            {
                var path = AssetDatabase.GetAssetPath(container);
                AssetDatabase.DeleteAsset(path);
                RefreshData();
                selectedContainer = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Delete Container Error: {e.Message}");
            }
        }

        private void DeleteTemplate(ContainerTemplate template)
        {
            if (template == null) return;

            try
            {
                var path = AssetDatabase.GetAssetPath(template);
                AssetDatabase.DeleteAsset(path);
                RefreshData();
                selectedTemplate = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Delete Template Error: {e.Message}");
            }
        }

        private void RefreshData()
        {
            try
            {
                var statGuids = AssetDatabase.FindAssets("t:StatType");
                allStats = statGuids.Select(guid =>
                        AssetDatabase.LoadAssetAtPath<StatType>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(stat => stat != null)
                    .ToList();

                var containerGuids = AssetDatabase.FindAssets("t:StatContainer");
                allContainers = containerGuids.Select(guid =>
                        AssetDatabase.LoadAssetAtPath<StatContainer>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(container => container != null)
                    .ToList();

                var templateGuids = AssetDatabase.FindAssets("t:ContainerTemplate");
                allTemplates = templateGuids.Select(guid =>
                        AssetDatabase.LoadAssetAtPath<ContainerTemplate>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(template => template != null)
                    .ToList();

                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Refresh Data Error: {e.Message}");

                if (allStats == null) allStats = new List<StatType>();
                if (allContainers == null) allContainers = new List<StatContainer>();
                if (allTemplates == null) allTemplates = new List<ContainerTemplate>();
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parentPath = Path.GetDirectoryName(path)!.Replace('\\', '/');
                    var folderName = Path.GetFileName(path);

                    if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
                        EnsureDirectoryExists(parentPath);

                    AssetDatabase.CreateFolder(parentPath, folderName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Directory Error: {e.Message}");
            }
        }

        private void CreateDirectories()
        {
            try
            {
                EnsureDirectoryExists(statTypesPath);
                EnsureDirectoryExists(containersPath);
                EnsureDirectoryExists(templatesPath);

                AssetDatabase.Refresh();
                Debug.Log("StatForge directories created successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Directories Error: {e.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                EditorPrefs.SetString("StatForge.StatTypesPath", statTypesPath);
                EditorPrefs.SetString("StatForge.ContainersPath", containersPath);
                EditorPrefs.SetString("StatForge.TemplatesPath", templatesPath);

                Debug.Log("StatForge settings saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"Save Settings Error: {e.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                statTypesPath = EditorPrefs.GetString("StatForge.StatTypesPath", "Assets/StatForge/StatTypes");
                containersPath = EditorPrefs.GetString("StatForge.ContainersPath", "Assets/StatForge/Containers");
                templatesPath = EditorPrefs.GetString("StatForge.TemplatesPath", "Assets/StatForge/Templates");
            }
            catch (Exception e)
            {
                Debug.LogError($"Load Settings Error: {e.Message}");
            }
        }

        private enum ViewMode
        {
            StatTypes,
            Containers,
            Templates,
            Tests,
            Settings
        }
    }
}
#endif