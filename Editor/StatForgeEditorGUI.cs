#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeEditorGUI
    {
        private Event currentEvent;
        private readonly StatForgeEditorData data;
        private readonly StatForgeEditorLogic logic;
        
        private static readonly System.Collections.Generic.Dictionary<Type, FieldInfo[]> fieldCache = new();

        private Vector2 containerScrollPosition;
        private bool[] statSelectionState;
        private int cachedStatCount = -1;
        
        private string formulaToTest = "";
        private string formulaResult = "";
        private Vector2 formulaTestScrollPos;
        private readonly System.Collections.Generic.Dictionary<string, float> testValues = new();
        

        private const float SIDEBAR_WIDTH = 180f;
        private const float EDIT_PANEL_WIDTH = 350f;
        private const float MIN_MAIN_CONTENT_WIDTH = 400f;
        private const float RIGHT_MARGIN = 10f;

        public StatForgeEditorGUI(StatForgeEditorData data, StatForgeEditorLogic logic)
        {
            this.data = data;
            this.logic = logic;
        }

        public void DrawWindow(Rect position)
        {
            currentEvent = Event.current;

            var windowWidth = position.width - RIGHT_MARGIN;
            var hasEditPanel = data.CurrentEdit != EditMode.None || data.SelectedStat != null || data.SelectedContainer != null;
            
            var sidebarWidth = Mathf.Min(SIDEBAR_WIDTH, windowWidth * 0.2f);
            var editPanelWidth = hasEditPanel ? Mathf.Min(EDIT_PANEL_WIDTH, windowWidth * 0.3f) : 0f;
            var mainContentWidth = windowWidth - sidebarWidth - editPanelWidth;
            
            if (mainContentWidth < MIN_MAIN_CONTENT_WIDTH && hasEditPanel)
            {
                editPanelWidth = windowWidth - sidebarWidth - MIN_MAIN_CONTENT_WIDTH;
                mainContentWidth = MIN_MAIN_CONTENT_WIDTH;
            }

            EditorGUILayout.BeginVertical();
            
            DrawHeader();
            
            EditorGUILayout.BeginHorizontal();
            DrawSidebar(sidebarWidth);
            DrawMainContent(mainContentWidth);
            if (hasEditPanel) 
                DrawEditPanel(editPanelWidth);
            
            GUILayout.Space(RIGHT_MARGIN);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Escape)
                {
                    if (data.CurrentEdit != EditMode.None)
                    {
                        logic.CancelEdit();
                        currentEvent.Use();
                        GUI.FocusControl(null);
                    }
                    else if (data.SelectedStat != null || data.SelectedContainer != null)
                    {
                        data.SelectedStat = null;
                        data.SelectedContainer = null;
                        currentEvent.Use();
                    }
                }
                else if (currentEvent.control)
                {
                    switch (currentEvent.keyCode)
                    {
                        case KeyCode.N when currentEvent.shift:
                            logic.StartCreateContainer();
                            currentEvent.Use();
                            break;
                        case KeyCode.N:
                            logic.StartCreateStat();
                            currentEvent.Use();
                            break;
                        case KeyCode.R:
                            logic.RefreshAll();
                            UpdateTestValuesFromStats();
                            currentEvent.Use();
                            break;
                    }
                }
            }
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle("Toolbar")
            {
                fixedHeight = 40f,
                padding = new RectOffset(10, 10, 5, 5)
            };

            var headerRect = EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.Height(40));
            EditorGUI.DrawRect(headerRect, StatForgeEditorData.HeaderColor);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            GUILayout.Label("StatForge", titleStyle, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            var searchWidth = Mathf.Max(200f, Screen.width * 0.2f);
            GUILayout.Label("Search:", GUILayout.Width(50));
            
            var searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            data.SearchFilter = EditorGUI.TextField(
                GUILayoutUtility.GetRect(searchWidth, EditorGUIUtility.singleLineHeight), 
                data.SearchFilter ?? "", 
                searchStyle
            );

            var buttonWidth = Screen.width > 1000 ? 80 : 60;
            if (GUILayout.Button(new GUIContent("New Stat", "Create a new stat type (Ctrl+N)"), GUILayout.Width(buttonWidth))) 
                logic.StartCreateStat();

            if (GUILayout.Button(new GUIContent("New Container", "Create a new stat container (Ctrl+Shift+N)"), GUILayout.Width(buttonWidth + 20))) 
                logic.StartCreateContainer();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar(float width)
        {
            var sidebarStyle = new GUIStyle("Box")
            {
                padding = new RectOffset(8, 8, 8, 8)
            };

            var sidebarRect = EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(width));
            EditorGUI.DrawRect(sidebarRect, StatForgeEditorData.SidebarColor);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                fontSize = 12
            };
            
            GUILayout.Label("Navigation", labelStyle);
            GUILayout.Space(5);

            var buttonHeight = 25f;
            if (DrawSidebarButton("📊 Stats", data.CurrentView == ViewMode.Stats, buttonHeight))
            {
                data.CurrentView = ViewMode.Stats;
                data.SelectedContainer = null;
            }

            if (DrawSidebarButton("📦 Containers", data.CurrentView == ViewMode.Containers, buttonHeight))
            {
                data.CurrentView = ViewMode.Containers;
                data.SelectedStat = null;
            }

            if (DrawSidebarButton("🧪 Testing", data.CurrentView == ViewMode.Testing, buttonHeight))
            {
                data.CurrentView = ViewMode.Testing;
                UpdateTestValuesFromStats();
            }

            if (DrawSidebarButton("⚙️ Settings", data.CurrentView == ViewMode.Settings, buttonHeight))
                data.CurrentView = ViewMode.Settings;

            GUILayout.Space(10);

            GUILayout.Label("Quick Stats", labelStyle);
            GUILayout.Space(3);
            
            var miniStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.gray }
            };
            
            GUILayout.Label($"Stats: {data.AllStatTypes.Count}", miniStyle);
            GUILayout.Label($"Containers: {data.AllContainers.Count}", miniStyle);
            GUILayout.Label($"Categories: {data.AllCategories.Length - 1}", miniStyle);
            
            if (Application.isPlaying)
            {
                var entities = logic.FindEntitiesWithStats();
                GUILayout.Label($"Runtime: {entities.Count} entities", miniStyle);
            }

            GUILayout.Space(10);

            if (data.CurrentView == ViewMode.Stats && data.AllCategories != null)
            {
                GUILayout.Label("Categories", labelStyle);
                GUILayout.Space(3);

                foreach (var category in data.AllCategories)
                {
                    var isSelected = data.SelectedCategory == category;
                    if (DrawCategoryButton(category, isSelected)) 
                        data.SelectedCategory = category;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMainContent(float width)
        {
            var mainStyle = new GUIStyle("Box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
            
            EditorGUILayout.BeginVertical(mainStyle, GUILayout.Width(width));
            data.ScrollPos = EditorGUILayout.BeginScrollView(data.ScrollPos);

            switch (data.CurrentView)
            {
                case ViewMode.Stats:
                    DrawStatsView();
                    break;
                case ViewMode.Containers:
                    DrawContainersView();
                    break;
                case ViewMode.Testing:
                    DrawTestingView();
                    break;
                case ViewMode.Settings:
                    DrawSettingsView();
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEditPanel(float width)
        {
            var editStyle = new GUIStyle("Box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(editStyle, GUILayout.Width(width));
            
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label(GetEditPanelTitle(), titleStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(new GUIContent("✕", "Close (Esc)"), GUILayout.Width(20), GUILayout.Height(20))) 
            {
                if (data.CurrentEdit != EditMode.None)
                    logic.CancelEdit();
                else
                {
                    data.SelectedStat = null;
                    data.SelectedContainer = null;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            data.EditScrollPos = EditorGUILayout.BeginScrollView(data.EditScrollPos);

            if (data.CurrentEdit != EditMode.None)
            {
                switch (data.CurrentEdit)
                {
                    case EditMode.CreateStat:
                    case EditMode.EditStat:
                        DrawStatEditor();
                        break;
                    case EditMode.CreateContainer:
                    case EditMode.EditContainer:
                        DrawContainerEditor();
                        break;
                }
            }
            else if (data.SelectedStat != null)
            {
                DrawStatPreview(data.SelectedStat);
            }
            else if (data.SelectedContainer != null)
            {
                DrawContainerPreview(data.SelectedContainer);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatPreview(StatType stat)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label("Stat Preview", headerStyle);
            GUILayout.Space(8);

            DrawPreviewSection("Basic Information", () =>
            {
                DrawReadOnlyField("Name", stat.DisplayName);
                DrawReadOnlyField("Short Name", stat.ShortName);
                DrawReadOnlyField("Category", stat.Category);
                DrawReadOnlyField("Value Type", stat.ValueType.ToString());
            });

            var previewStyle = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(previewStyle);
            EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
            var exampleValue = stat.ValueType == StatValueType.Percentage ? 25.5f : 150.75f;
            EditorGUILayout.LabelField($"Example: {stat.FormatValue(exampleValue)}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            DrawPreviewSection("Values", () =>
            {
                DrawReadOnlyField("Default Value", stat.DefaultValue.ToString("F1"));
                
                if (stat.ValueType == StatValueType.Percentage)
                {
                    DrawReadOnlyField("Minimum", stat.MinValue.ToString("F1"));
                    DrawReadOnlyField("Maximum", stat.MaxValue.ToString("F1"));
                }
                else
                {
                    EditorGUILayout.HelpBox("No limits - values can be any number", MessageType.Info);
                }
            });

            if (stat.HasFormula)
            {
                DrawPreviewSection("Formula", () =>
                {
                    var formulaStyle = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true
                    };
                    EditorGUILayout.SelectableLabel(stat.Formula, formulaStyle, GUILayout.Height(40));
                });
            }

            if (!string.IsNullOrEmpty(stat.Description))
            {
                DrawPreviewSection("Description", () =>
                {
                    var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    EditorGUILayout.SelectableLabel(stat.Description, descStyle, GUILayout.Height(60));
                });
            }

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Edit", GUILayout.Height(30)))
            {
                logic.StartEditStat(stat);
            }
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Duplicate", GUILayout.Height(30)))
            {
                logic.DuplicateStat(stat);
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete {stat.DisplayName}?", "Yes", "No"))
                {
                    logic.DeleteStat(stat);
                    data.SelectedStat = null;
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContainerPreview(StatContainerAsset container)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label("Container Preview", headerStyle);
            GUILayout.Space(8);

            DrawPreviewSection("Basic Information", () =>
            {
                DrawReadOnlyField("Name", container.ContainerName);
                DrawReadOnlyField("Stats Count", container.StatTypes.Count.ToString());
            });

            if (!string.IsNullOrEmpty(container.Description))
            {
                DrawPreviewSection("Description", () =>
                {
                    var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    EditorGUILayout.SelectableLabel(container.Description, descStyle, GUILayout.Height(60));
                });
            }

            if (container.StatTypes.Count > 0)
            {
                DrawPreviewSection("Container Stats", () =>
                {
                    var listStyle = new GUIStyle("box") { padding = new RectOffset(8, 8, 8, 8) };
                    EditorGUILayout.BeginVertical(listStyle);

                    foreach (var stat in container.StatTypes)
                    {
                        if (stat != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            var typeIcon = stat.ValueType switch
                            {
                                StatValueType.Normal => "🔢",
                                StatValueType.Percentage => "📊",
                                StatValueType.Rate => "⏱️",
                                _ => "💧"
                            };
                            
                            GUILayout.Label($"{typeIcon} {stat.DisplayName}", GUILayout.Width(150));
                            GUILayout.Label($"({stat.ShortName})", EditorStyles.miniLabel, GUILayout.Width(50));
                            GUILayout.Label(stat.FormatValue(stat.DefaultValue), EditorStyles.boldLabel);
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.EndVertical();
                });
            }

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Edit", GUILayout.Height(30)))
            {
                logic.StartEditContainer(container);
            }
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Runtime", GUILayout.Height(30)))
            {
                var runtimeContainer = container.CreateRuntimeContainer();
                Debug.Log($"[StatForge] Runtime container '{container.ContainerName}' created with {runtimeContainer.Count} stats");
                
                foreach (var stat in runtimeContainer.Stats)
                {
                    Debug.Log($"  - {stat.Name}: {stat.Value}");
                }
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete container {container.ContainerName}?", "Yes", "No"))
                {
                    logic.DeleteContainer(container);
                    data.SelectedContainer = null;
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection(string title, Action content)
        {
            var sectionStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 8, 8)
            };

            EditorGUILayout.BeginVertical(sectionStyle);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(3);

            content?.Invoke();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawReadOnlyField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            var readOnlyStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = Color.gray }
            };
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(value, readOnlyStyle);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatsView()
        {
            var filteredStats = logic.GetFilteredStats();

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📊 Stat Types", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (!string.IsNullOrEmpty(data.SearchFilter))
            {
                var resultStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                GUILayout.Label($"{filteredStats.Count} results", resultStyle);
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(8);

            if (filteredStats.Count == 0)
            {
                if (string.IsNullOrEmpty(data.SearchFilter))
                    DrawEmptyState("No stats found", "Create your first stat to get started!");
                else
                    DrawEmptyState("No results", $"No stats match '{data.SearchFilter}'");
                return;
            }

            if (data.SelectedCategory == "All")
            {
                var categories = filteredStats.GroupBy(s => s.Category).OrderBy(g => g.Key);

                foreach (var categoryGroup in categories)
                {
                    DrawCategoryHeader(categoryGroup.Key, categoryGroup.Count());

                    foreach (var stat in categoryGroup.OrderBy(s => s.DisplayName)) 
                        DrawStatCard(stat);

                    GUILayout.Space(8);
                }
            }
            else
            {
                var categoryStats = filteredStats.Where(s => s.Category == data.SelectedCategory);

                foreach (var stat in categoryStats.OrderBy(s => s.DisplayName)) 
                    DrawStatCard(stat);
            }
        }

        private void DrawContainersView()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📦 Stat Containers", headerStyle);
            GUILayout.FlexibleSpace();
            
            var countStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            GUILayout.Label($"{data.AllContainers.Count} containers", countStyle);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(8);

            if (data.AllContainers.Count == 0)
            {
                DrawEmptyState("No containers found", "Create your first container to organize stats!");
                return;
            }

            foreach (var container in data.AllContainers) 
                DrawContainerCard(container);
        }

        private void DrawTestingView()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label("🧪 Testing Tools", headerStyle);
            GUILayout.Space(8);

            DrawFormulaTestingSection();
            
            GUILayout.Space(15);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to see runtime stats", MessageType.Info);
                return;
            }

            var entities = logic.FindEntitiesWithStats();

            if (entities.Count == 0)
            {
                DrawEmptyState("No entities with stats found", "Run the game to see stats in runtime!");
                return;
            }

            GUILayout.Label("Runtime Entities", EditorStyles.boldLabel);
            GUILayout.Space(5);

            foreach (var entity in entities) 
                DrawEntityCard(entity);
        }
        private void DrawFormulaTestingSection()
        {
            var sectionStyle = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label("Formula Tester", titleStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear All", GUILayout.Width(70)))
            {
                formulaToTest = "";
                formulaResult = "";
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Test formulas using your created stats. Values are automatically loaded from StatTypes.", MessageType.Info);

            GUILayout.Space(8);

            GUILayout.Label("Formula:", EditorStyles.boldLabel);
            var formulaStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            formulaToTest = EditorGUILayout.TextField(formulaToTest, formulaStyle, GUILayout.Height(25));

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🧮 Test Formula", GUILayout.Height(30)))
            {
                TestFormulaWithRealStats();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Refresh Stats", GUILayout.Height(30)))
            {
                UpdateTestValuesFromStats();
                if (!string.IsNullOrEmpty(formulaToTest))
                    TestFormulaWithRealStats();
            }

            EditorGUILayout.EndHorizontal();

            if (testValues.Count > 0)
            {
                GUILayout.Space(8);
                DrawAvailableStats();
            }

            if (!string.IsNullOrEmpty(formulaResult))
            {
                GUILayout.Space(10);
                DrawFormulaResult();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAvailableStats()
        {
            GUILayout.Label("Available Stats:", EditorStyles.boldLabel);
            
            var valueStyle = new GUIStyle("box") { padding = new RectOffset(8, 8, 8, 8) };
            EditorGUILayout.BeginVertical(valueStyle);

            var keys = testValues.Keys.OrderBy(k => k).ToArray();
            for (int i = 0; i < keys.Length; i += 3)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int j = 0; j < 3 && i + j < keys.Length; j++)
                {
                    var key = keys[i + j];
                    var value = testValues[key];
                    
                    var statType = data.AllStatTypes.FirstOrDefault(s => s.ShortName == key || s.DisplayName == key);
                    var formattedValue = statType?.FormatValue(value) ?? value.ToString("F1");
                    
                    EditorGUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label(key, EditorStyles.boldLabel);
                    GUILayout.Label(formattedValue, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    if (j < 2) GUILayout.Space(10);
                }
                
                EditorGUILayout.EndHorizontal();
                if (i + 3 < keys.Length) GUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private void UpdateTestValuesFromStats()
        {
            testValues.Clear();
            
            foreach (var statType in data.AllStatTypes)
            {
                if (statType != null)
                {
                    if (!string.IsNullOrEmpty(statType.DisplayName))
                        testValues[statType.DisplayName] = statType.DefaultValue;
                    
                    if (!string.IsNullOrEmpty(statType.ShortName))
                        testValues[statType.ShortName] = statType.DefaultValue;
                }
            }
        }

        private void TestFormulaWithRealStats()
        {
            if (string.IsNullOrEmpty(formulaToTest))
            {
                formulaResult = "Please enter a formula to test";
                return;
            }

            if (testValues.Count == 0)
                UpdateTestValuesFromStats();

            try
            {
                var testResult = FormulaEvaluator.EvaluateMathExpression(
                    System.Text.RegularExpressions.Regex.Replace(formulaToTest, @"\b([A-Za-z][A-Za-z0-9_]*)\b", match =>
                    {
                        var statName = match.Groups[1].Value;
                        return testValues.TryGetValue(statName, out var value) ? value.ToString(CultureInfo.InvariantCulture) : "0";
                    })
                );

                var usedStats = System.Text.RegularExpressions.Regex.Matches(formulaToTest, @"\b([A-Za-z][A-Za-z0-9_]*)\b")
                    .Select(m => m.Groups[1].Value)
                    .Where(name => testValues.ContainsKey(name))
                    .Distinct()
                    .ToList();

                formulaResult = $"Formula: {formulaToTest}\n";
                if (usedStats.Count > 0)
                {
                    formulaResult += $"Using stats: {string.Join(", ", usedStats.Select(s => $"{s}={testValues[s]:F1}"))}\n";
                }
                formulaResult += $"Result: {testResult:F2}";
            }
            catch (Exception e)
            {
                formulaResult = $"Error: {e.Message}";
            }
        }

        private void DrawFormulaResult()
        {
            var resultLines = formulaResult.Split('\n');
            
            if (resultLines.Length >= 3)
            {
                var resultStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 12, 12),
                    normal = { background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.2f, 0.3f)) }
                };

                EditorGUILayout.BeginVertical(resultStyle);
                
                var bigResultStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    normal = { textColor = Color.green },
                    alignment = TextAnchor.MiddleCenter
                };
                
                var resultText = resultLines[2].Replace("Result: ", "");
                GUILayout.Label($"= {resultText}", bigResultStyle);
                
                EditorGUILayout.EndVertical();
                
                GUILayout.Space(8);
            }

            var detailStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(detailStyle);
            
            GUILayout.Label("Calculation Details:", EditorStyles.boldLabel);
            
            if (resultLines.Length >= 1)
            {
                var formulaDetailStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = Color.gray }
                };
                GUILayout.Label($"Formula: {resultLines[0].Replace("Formula: ", "")}", formulaDetailStyle);
            }
            
            if (resultLines.Length >= 2)
            {
                GUILayout.Space(3);
                var valuesStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = Color.gray }
                };
                GUILayout.Label(resultLines[1], valuesStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawSettingsView()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label("⚙️ Settings & Tools", headerStyle);
            GUILayout.Space(8);

            DrawSection("Project Actions", () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Refresh All", "Refresh all cached data (Ctrl+R)"))) 
                {
                    logic.RefreshAll();
                    UpdateTestValuesFromStats();
                }
                
                if (GUILayout.Button(new GUIContent("Validate Stats", "Check for issues in stat definitions")))
                    logic.ValidateAllStats();
                EditorGUILayout.EndHorizontal();
                
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Force Recalculate All Runtime Stats"))
                        logic.RefreshAllStats();
                }
            });

            DrawSection("Statistics", () =>
            {
                var stats = data.AllStatTypes;
                var containers = data.AllContainers;
                
                GUILayout.Label($"📊 Total Stats: {stats.Count}");
                GUILayout.Label($"📐 With Formulas: {stats.Count(s => s.HasFormula)}");
                GUILayout.Label($"📦 Containers: {containers.Count}");
                GUILayout.Label($"📁 Categories: {data.AllCategories.Length - 1}");
                
                if (stats.Count > 0)
                {
                    var byType = stats.GroupBy(s => s.ValueType).ToDictionary(g => g.Key, g => g.Count());
                    foreach (var typeGroup in byType)
                    {
                        var icon = typeGroup.Key switch
                        {
                            StatValueType.Normal => "🔢",
                            StatValueType.Percentage => "📊",
                            StatValueType.Rate => "⏱️",
                            _ => "💧"
                        };
                        GUILayout.Label($"{icon} {typeGroup.Key}: {typeGroup.Value}");
                    }
                }

                if (Application.isPlaying)
                {
                    var entities = logic.FindEntitiesWithStats();
                    GUILayout.Label($"🎮 Runtime Entities: {entities.Count}");
                }
            });
        }

        private void DrawStatEditor()
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            
            GUILayout.Label("Basic Information", sectionStyle);
            GUILayout.Space(3);

            data.NewStatName = EditorGUILayout.TextField("Name", data.NewStatName ?? "");
            
            EditorGUILayout.BeginHorizontal();
            data.NewStatShortName = EditorGUILayout.TextField("Short Name", data.NewStatShortName ?? "");
            if (GUILayout.Button("Auto", GUILayout.Width(50)) && !string.IsNullOrEmpty(data.NewStatName))
            {
                data.NewStatShortName = logic.GenerateShortName(data.NewStatName);
            }
            EditorGUILayout.EndHorizontal();
            
            data.NewStatCategory = EditorGUILayout.TextField("Category", data.NewStatCategory ?? "General");

            GUILayout.Space(5);
            
            var oldValueType = data.NewStatValueType;
            data.NewStatValueType = (StatValueType)EditorGUILayout.EnumPopup("Value Type", data.NewStatValueType);

            if (oldValueType != data.NewStatValueType)
            {
                if (data.NewStatValueType == StatValueType.Percentage)
                {
                    data.NewStatMin = 0f;
                    data.NewStatMax = 100f;
                }
            }

            var previewStyle = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(previewStyle);
            EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
            var exampleValue = data.NewStatValueType == StatValueType.Percentage ? 25.5f : 150.75f;
            var preview = data.NewStatValueType switch
            {
                StatValueType.Normal => Mathf.RoundToInt(exampleValue).ToString(),
                StatValueType.Percentage => $"{exampleValue:F2}%",
                StatValueType.Rate => $"{exampleValue:F1}/s",
                _ => exampleValue.ToString("F1")
            };
            EditorGUILayout.LabelField($"Example: {preview}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.Label("Values", sectionStyle);
            GUILayout.Space(3);

            data.NewStatDefault = EditorGUILayout.FloatField("Default Value", data.NewStatDefault);
            
            if (data.NewStatValueType == StatValueType.Percentage)
            {
                data.NewStatMin = EditorGUILayout.FloatField("Minimum", data.NewStatMin);
                data.NewStatMax = EditorGUILayout.FloatField("Maximum", data.NewStatMax);

                if (data.NewStatMin > data.NewStatMax)
                {
                    EditorGUILayout.HelpBox("Minimum value cannot be greater than maximum!", MessageType.Warning);
                    data.NewStatMin = data.NewStatMax;
                }

                if (data.NewStatDefault < data.NewStatMin || data.NewStatDefault > data.NewStatMax)
                {
                    EditorGUILayout.HelpBox("Default value must be between min and max!", MessageType.Warning);
                    data.NewStatDefault = Mathf.Clamp(data.NewStatDefault, data.NewStatMin, data.NewStatMax);
                }

                if (GUILayout.Button("Reset to 0-100%"))
                {
                    data.NewStatMin = 0f;
                    data.NewStatMax = 100f;
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Formula (Optional)", sectionStyle);
            GUILayout.Space(3);
            GUILayout.Label("Use abbreviations (e.g. CON * 15 + STR * 2)", EditorStyles.miniLabel);
            data.NewStatFormula = EditorGUILayout.TextArea(data.NewStatFormula ?? "", GUILayout.Height(50));

            GUILayout.Space(8);
            GUILayout.Label("Description", sectionStyle);
            GUILayout.Space(3);
            data.NewStatDescription = EditorGUILayout.TextArea(data.NewStatDescription ?? "", GUILayout.Height(60));

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(data.IsCreatingNew ? "Create Stat" : "Save", GUILayout.Height(25))) 
                logic.SaveStat();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUILayout.Height(25))) 
                logic.CancelEdit();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawContainerEditor()
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            
            GUILayout.Label("Basic Information", sectionStyle);
            GUILayout.Space(3);
            data.NewContainerName = EditorGUILayout.TextField("Container Name", data.NewContainerName ?? "");

            GUILayout.Space(8);
            GUILayout.Label("Description", sectionStyle);
            GUILayout.Space(3);
            data.NewContainerDescription = EditorGUILayout.TextArea(data.NewContainerDescription ?? "", GUILayout.Height(50));

            GUILayout.Space(8);
            GUILayout.Label("Container Stats", sectionStyle);
            GUILayout.Space(3);

            EditorGUILayout.HelpBox("Select the stats that will be part of this container:", MessageType.Info);

            InitializeStatSelection();

            EditorGUILayout.BeginVertical("box");
            
            containerScrollPosition = EditorGUILayout.BeginScrollView(containerScrollPosition, GUILayout.Height(150));

            for (int i = 0; i < data.AllStatTypes.Count; i++)
            {
                var stat = data.AllStatTypes[i];
                if (stat == null) continue;

                EditorGUILayout.BeginHorizontal();

                var wasSelected = statSelectionState[i];
                statSelectionState[i] = GUILayout.Toggle(statSelectionState[i], "", GUILayout.Width(15));

                if (statSelectionState[i] != wasSelected)
                {
                    if (statSelectionState[i])
                    {
                        if (!data.SelectedStats.Contains(stat))
                            data.SelectedStats.Add(stat);
                    }
                    else
                    {
                        data.SelectedStats.Remove(stat);
                    }
                }

                var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                if (statSelectionState[i])
                {
                    labelStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                    labelStyle.fontStyle = FontStyle.Bold;
                }

                GUILayout.Label($"{stat.DisplayName} ({stat.ShortName})", labelStyle);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{data.SelectedStats.Count} stats selected", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select All", GUILayout.Width(60)))
            {
                data.SelectedStats.Clear();
                data.SelectedStats.AddRange(data.AllStatTypes.Where(s => s != null));
                UpdateStatSelectionState();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50))) 
            {
                data.SelectedStats.Clear();
                UpdateStatSelectionState();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            var buttonText = data.IsCreatingNew ? "Create Container" : "Save Container";
            if (GUILayout.Button(buttonText, GUILayout.Height(25))) 
                logic.SaveContainer();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Cancel", GUILayout.Height(25))) 
                logic.CancelEdit();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStatSelection()
        {
            if (statSelectionState == null || statSelectionState.Length != data.AllStatTypes.Count || cachedStatCount != data.AllStatTypes.Count)
            {
                cachedStatCount = data.AllStatTypes.Count;
                statSelectionState = new bool[cachedStatCount];
                UpdateStatSelectionState();
            }
        }

        private void UpdateStatSelectionState()
        {
            if (statSelectionState == null) return;

            for (int i = 0; i < data.AllStatTypes.Count && i < statSelectionState.Length; i++)
            {
                var stat = data.AllStatTypes[i];
                statSelectionState[i] = stat != null && data.SelectedStats.Contains(stat);
            }
        }

        private bool DrawSidebarButton(string text, bool isSelected, float height)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 5, 5),
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            GUI.backgroundColor = isSelected ? StatForgeEditorData.SelectedColor : Color.clear;

            var result = GUILayout.Button(text, style, GUILayout.Height(height));
            GUI.backgroundColor = Color.white;

            return result;
        }

        private bool DrawCategoryButton(string category, bool isSelected)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(12, 5, 3, 3),
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            if (isSelected)
            {
                style.normal.textColor = new Color(0.24f, 0.49f, 0.91f);
                style.fontStyle = FontStyle.Bold;
            }

            var rect = GUILayoutUtility.GetRect(new GUIContent(category), style);

            if (Event.current.type == EventType.Repaint)
                style.Draw(rect, new GUIContent(category), false, false, false, false);

            return Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition);
        }

        private void DrawStatCard(StatType stat)
        {
            var cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };

            var isSelected = data.SelectedStat == stat;
            if (isSelected) GUI.backgroundColor = StatForgeEditorData.SelectedColor;

            var cardRect = EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            
            var typeIcon = stat.ValueType switch
            {
                StatValueType.Normal => "🔢",
                StatValueType.Percentage => "📊",
                StatValueType.Rate => "⏱️",
                _ => "💧"
            };
            
            GUILayout.Label($"{typeIcon} {stat.DisplayName}", nameStyle);

            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            var infoText = $"{stat.ShortName} | {stat.Category} | {stat.FormatValue(stat.DefaultValue)}";
            if (stat.HasFormula) infoText += " | Formula ✓";

            GUILayout.Label(infoText, infoStyle);

            if (!string.IsNullOrEmpty(stat.Description))
            {
                var desc = stat.Description.Length > 80 ? stat.Description.Substring(0, 77) + "..." : stat.Description;
                GUILayout.Label(desc, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(60));

            if (GUILayout.Button("Edit", GUILayout.Height(22))) 
                logic.StartEditStat(stat);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(22)))
                if (EditorUtility.DisplayDialog("Confirm", $"Delete {stat.DisplayName}?", "Yes", "No"))
                    logic.DeleteStat(stat);

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
            {
                data.SelectedStat = isSelected ? null : stat;
                Event.current.Use();
            }
        }

        private void DrawContainerCard(StatContainerAsset container)
        {
            var cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };

            var isSelected = data.SelectedContainer == container;
            if (isSelected) GUI.backgroundColor = StatForgeEditorData.SelectedColor;

            var cardRect = EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUILayout.Label($"📦 {container.ContainerName}", nameStyle);

            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            GUILayout.Label($"{container.StatTypes.Count} stats", infoStyle);

            if (container.StatTypes.Count > 0)
            {
                var statsPreview = string.Join(", ", container.StatTypes.Take(3).Select(s => s?.ShortName ?? "null"));
                if (container.StatTypes.Count > 3) statsPreview += "...";
                GUILayout.Label(statsPreview, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            if (GUILayout.Button("Edit", GUILayout.Height(22))) 
                logic.StartEditContainer(container);

            if (GUILayout.Button("Inspect", GUILayout.Height(22)))
            {
                Selection.activeObject = container;
                EditorGUIUtility.PingObject(container);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(22)))
                if (EditorUtility.DisplayDialog("Confirm", $"Delete container {container.ContainerName}?", "Yes", "No"))
                    logic.DeleteContainer(container);

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
            {
                data.SelectedContainer = isSelected ? null : container;
                Event.current.Use();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawEntityCard(GameObject entity)
        {
            var cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 6, 6)
            };

            EditorGUILayout.BeginVertical(cardStyle);

            GUILayout.Label($"🎮 {entity.name}", EditorStyles.boldLabel);

            var components = entity.GetComponents<MonoBehaviour>();
            foreach (var comp in components) 
                DrawStatsInComponent(comp);

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawStatsInComponent(MonoBehaviour component)
        {
            var componentType = component.GetType();
            
            if (!fieldCache.TryGetValue(componentType, out var fields))
            {
                fields = componentType.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                fieldCache[componentType] = fields;
            }

            foreach (var field in fields)
                if (field.FieldType == typeof(Stat))
                {
                    var stat = field.GetValue(component) as Stat;
                    if (stat?.StatType != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        var typeIcon = stat.ValueType switch
                        {
                            StatValueType.Normal => "🔢",
                            StatValueType.Percentage => "📊",
                            StatValueType.Rate => "⏱️",
                            _ => "💧"
                        };
                        
                        GUILayout.Label($"  {typeIcon} {stat.Name}:", GUILayout.Width(100));

                        var valueStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.green } };
                        GUILayout.Label(stat.FormattedValue, valueStyle, GUILayout.Width(80));

                        if (stat.HasModifiers)
                        {
                            var modStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.blue } };
                            GUILayout.Label($"({stat.Modifiers.Count} mods)", modStyle);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
        }

        private void DrawEmptyState(string title, string subtitle)
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(300));

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };

            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray },
                wordWrap = true
            };

            GUILayout.Label(title, titleStyle);
            GUILayout.Space(5);
            GUILayout.Label(subtitle, subtitleStyle);

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        private void DrawCategoryHeader(string category, int count)
        {
            EditorGUILayout.BeginHorizontal();

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUILayout.Label($"📁 {category}", headerStyle);

            var countStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            GUILayout.Label($"({count})", countStyle);

            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax + 1, rect.width, 1), Color.gray);

            GUILayout.Space(5);
        }

        private void DrawSection(string title, Action content)
        {
            var sectionStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 8, 8)
            };

            EditorGUILayout.BeginVertical(sectionStyle);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(3);

            content?.Invoke();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private string GetEditPanelTitle()
        {
            if (data.CurrentEdit != EditMode.None)
            {
                return data.CurrentEdit switch
                {
                    EditMode.CreateStat => "Create Stat",
                    EditMode.EditStat => "Edit Stat",
                    EditMode.CreateContainer => "Create Container",
                    EditMode.EditContainer => "Edit Container",
                    _ => "Editor"
                };
            } 
            if (data.SelectedStat != null)
            {
                return $"Stat: {data.SelectedStat.DisplayName}";
            } 
            if (data.SelectedContainer != null)
            {
                return $"Container: {data.SelectedContainer.ContainerName}";
            }
            
            return "Preview";
        }
    }
}
#endif