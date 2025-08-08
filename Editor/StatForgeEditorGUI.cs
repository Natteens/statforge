#if UNITY_EDITOR
using System;
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
        
        private static System.Collections.Generic.Dictionary<Type, FieldInfo[]> fieldCache = 
            new System.Collections.Generic.Dictionary<System.Type, FieldInfo[]>();

        private Vector2 containerScrollPosition;
        private bool[] statSelectionState;
        private int cachedStatCount = -1;

        // Constantes para layout responsivo
        private const float SIDEBAR_WIDTH = 180f;
        private const float EDIT_PANEL_WIDTH = 350f;
        private const float MIN_MAIN_CONTENT_WIDTH = 400f;
        private const float RIGHT_MARGIN = 10f; // Espaço à direita

        public StatForgeEditorGUI(StatForgeEditorData data, StatForgeEditorLogic logic)
        {
            this.data = data;
            this.logic = logic;
        }

        public void DrawWindow(Rect position)
        {
            currentEvent = Event.current;

            // Layout responsivo baseado no tamanho da janela
            var windowWidth = position.width - RIGHT_MARGIN; // Subtraindo margem direita
            var hasEditPanel = data.CurrentEdit != EditMode.None;
            
            // Calcular larguras dinamicamente
            var sidebarWidth = Mathf.Min(SIDEBAR_WIDTH, windowWidth * 0.2f);
            var editPanelWidth = hasEditPanel ? Mathf.Min(EDIT_PANEL_WIDTH, windowWidth * 0.3f) : 0f;
            var mainContentWidth = windowWidth - sidebarWidth - editPanelWidth;
            
            // Garantir largura mínima para o conteúdo principal
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
            
            // Espaço à direita
            GUILayout.Space(RIGHT_MARGIN);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            if (currentEvent.type == EventType.KeyDown)
                if (currentEvent.keyCode == KeyCode.Escape && data.CurrentEdit != EditMode.None)
                {
                    logic.CancelEdit();
                    currentEvent.Use();
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

            // Título mais compacto
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            GUILayout.Label("⚡ StatForge", titleStyle, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            // Barra de pesquisa responsiva
            var searchWidth = Mathf.Max(150f, Screen.width * 0.15f);
            GUILayout.Label("Search:", GUILayout.Width(50));
            data.SearchFilter = GUILayout.TextField(data.SearchFilter ?? "", GUILayout.Width(searchWidth));

            // Botões responsivos
            var buttonWidth = Screen.width > 1000 ? 80 : 60;
            if (GUILayout.Button("New Stat", GUILayout.Width(buttonWidth))) 
                logic.StartCreateStat();

            if (GUILayout.Button("New Container", GUILayout.Width(buttonWidth + 20))) 
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

            // Botões mais compactos
            var buttonHeight = 25f;
            if (DrawSidebarButton("📊 Stats", data.CurrentView == ViewMode.Stats, buttonHeight))
                data.CurrentView = ViewMode.Stats;

            if (DrawSidebarButton("📦 Containers", data.CurrentView == ViewMode.Containers, buttonHeight))
                data.CurrentView = ViewMode.Containers;

            if (DrawSidebarButton("🧪 Testing", data.CurrentView == ViewMode.Testing, buttonHeight))
                data.CurrentView = ViewMode.Testing;

            if (DrawSidebarButton("⚙️ Settings", data.CurrentView == ViewMode.Settings, buttonHeight))
                data.CurrentView = ViewMode.Settings;

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
            
            // Header do painel de edição
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label(GetEditPanelTitle(), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(20))) 
                logic.CancelEdit();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            data.EditScrollPos = EditorGUILayout.BeginScrollView(data.EditScrollPos);

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

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatsView()
        {
            var filteredStats = logic.GetFilteredStats();

            if (filteredStats.Count == 0)
            {
                DrawEmptyState("No stats found", "Create your first stat to get started!");
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
            GUILayout.Label("📦 Stat Containers", headerStyle);
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
            GUILayout.Label("🧪 Runtime Testing", headerStyle);
            GUILayout.Space(5);

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

            foreach (var entity in entities) 
                DrawEntityCard(entity);
        }

        private void DrawSettingsView()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label("⚙️ StatForge Settings", headerStyle);
            GUILayout.Space(8);

            DrawSection("Quick Actions", () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🎮 RPG Template")) 
                    logic.CreateRPGTemplate();
                if (GUILayout.Button("⚔️ Action Template")) 
                    logic.CreateActionTemplate();
                EditorGUILayout.EndHorizontal();
            });

            DrawSection("Debug", () =>
            {
                if (GUILayout.Button("🔄 Refresh All Stats")) 
                    logic.RefreshAllStats();

                if (GUILayout.Button("📊 Performance Log")) 
                    logic.LogPerformanceStats();
            });

            DrawSection("Statistics", () =>
            {
                GUILayout.Label($"📊 Total Stats: {data.AllStatTypes.Count}");
                GUILayout.Label($"📐 With Formulas: {data.AllStatTypes.Count(s => s.HasFormula)}");
                GUILayout.Label($"📦 Containers: {data.AllContainers.Count}");
                GUILayout.Label($"📁 Categories: {data.AllCategories.Length - 1}");

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
            data.NewStatShortName = EditorGUILayout.TextField("Short Name", data.NewStatShortName ?? "");
            data.NewStatCategory = EditorGUILayout.TextField("Category", data.NewStatCategory ?? "General");

            GUILayout.Space(8);
            GUILayout.Label("Values", sectionStyle);
            GUILayout.Space(3);

            data.NewStatDefault = EditorGUILayout.FloatField("Default Value", data.NewStatDefault);
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

            if (isSelected)
                GUI.backgroundColor = StatForgeEditorData.SelectedColor;
            else
                GUI.backgroundColor = Color.clear;

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
            GUILayout.Label(stat.DisplayName, nameStyle);

            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            var infoText = $"{stat.ShortName} | {stat.Category} | Default: {stat.DefaultValue:F1}";
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

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUILayout.Label($"📦 {container.ContainerName}", nameStyle);

            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            GUILayout.Label($"{container.StatTypes.Count} stats", infoStyle);

            if (container.StatTypes.Count > 0)
            {
                var statsPreview = string.Join(", ", container.StatTypes.Take(3).Select(s => s.ShortName));
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
                        GUILayout.Label($"  📊 {stat.Name}:", GUILayout.Width(100));

                        var valueStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.green } };
                        GUILayout.Label(stat.Value.ToString("F1"), valueStyle, GUILayout.Width(50));

                        var modifiers = stat.Modifiers.Where(m => m.Type == ModifierType.Additive).Sum(m => m.Value);
                        if (modifiers != 0)
                        {
                            var modStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.blue } };
                            GUILayout.Label($"(+{modifiers:F1})", modStyle);
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

            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };

            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
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
            switch (data.CurrentEdit)
            {
                case EditMode.CreateStat: return "Create Stat";
                case EditMode.EditStat: return "Edit Stat";
                case EditMode.CreateContainer: return "Create Container";
                case EditMode.EditContainer: return "Edit Container";
                default: return "Editor";
            }
        }
    }
}
#endif