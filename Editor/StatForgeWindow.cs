using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using StatForge;

namespace StatForge.Editor
{
    /// <summary>
    /// Advanced StatForge editor window for managing stats and testing formulas.
    /// Provides a powerful interface for stat creation, formula editing, and debugging.
    /// </summary>
    public class StatForgeWindow : EditorWindow
    {
        [MenuItem("Tools/StatForge/StatForge Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeWindow>("StatForge");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        // Window state
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private readonly string[] tabs = { "Stat Editor", "Formula Tester", "Debug Console", "Settings" };
        
        // Stat Editor
        private string newStatName = "NewStat";
        private float newStatBaseValue = 0f;
        private string newStatFormula = "";
        private float newStatMinValue = 0f;
        private float newStatMaxValue = 100f;
        private bool newStatAllowModifiers = true;
        private List<Stat> createdStats = new List<Stat>();
        
        // Formula Tester
        private string testFormula = "";
        private string testResult = "";
        private List<string> testStatNames = new List<string>();
        private List<float> testStatValues = new List<float>();
        private bool showFormulaHelp = false;
        
        // Debug Console
        private List<string> debugMessages = new List<string>();
        private Vector2 debugScrollPosition;
        private bool autoRefreshDebug = true;
        
        // Settings
        private bool enableRuntimeDebugging = true;
        private bool showPerformanceMetrics = false;
        
        private void OnEnable()
        {
            // Subscribe to stat events for debugging
            StatEvents.OnStatChanged += OnStatChangedDebug;
            StatEvents.OnModifierAdded += OnModifierAddedDebug;
            StatEvents.OnModifierRemoved += OnModifierRemovedDebug;
            
            // Add some test stats for demo
            if (testStatNames.Count == 0)
            {
                testStatNames.AddRange(new[] { "health", "mana", "strength", "level" });
                testStatValues.AddRange(new[] { 100f, 50f, 10f, 1f });
            }
        }
        
        private void OnDisable()
        {
            StatEvents.OnStatChanged -= OnStatChangedDebug;
            StatEvents.OnModifierAdded -= OnModifierAddedDebug;
            StatEvents.OnModifierRemoved -= OnModifierRemovedDebug;
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawTabSelection();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawStatEditor(); break;
                case 1: DrawFormulaTester(); break;
                case 2: DrawDebugConsole(); break;
                case 3: DrawSettings(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            if (autoRefreshDebug && selectedTab == 2)
            {
                Repaint();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("StatForge - Advanced Stat Management", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Help", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Application.OpenURL("https://github.com/Natteens/statforge");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabSelection()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            EditorGUILayout.Space();
        }
        
        private void DrawStatEditor()
        {
            EditorGUILayout.LabelField("Stat Editor", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Create and configure individual stats with formulas and modifiers.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Stat Creation Section
            EditorGUILayout.LabelField("Create New Stat", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            newStatName = EditorGUILayout.TextField("Name", newStatName);
            newStatBaseValue = EditorGUILayout.FloatField("Base Value", newStatBaseValue);
            newStatFormula = EditorGUILayout.TextField("Formula (optional)", newStatFormula);
            
            EditorGUILayout.BeginHorizontal();
            newStatMinValue = EditorGUILayout.FloatField("Min Value", newStatMinValue);
            newStatMaxValue = EditorGUILayout.FloatField("Max Value", newStatMaxValue);
            EditorGUILayout.EndHorizontal();
            
            newStatAllowModifiers = EditorGUILayout.Toggle("Allow Modifiers", newStatAllowModifiers);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure min <= max
                if (newStatMinValue > newStatMaxValue)
                    newStatMinValue = newStatMaxValue;
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Stat", GUILayout.Height(30)))
            {
                CreateNewStat();
            }
            
            if (GUILayout.Button("Validate Formula", GUILayout.Height(30)))
            {
                ValidateFormula();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Created Stats Section
            if (createdStats.Count > 0)
            {
                EditorGUILayout.LabelField("Created Stats", EditorStyles.boldLabel);
                
                for (int i = 0; i < createdStats.Count; i++)
                {
                    var stat = createdStats[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(stat.Name, EditorStyles.boldLabel);
                    
                    if (stat.IsDerived)
                    {
                        EditorGUILayout.LabelField("(Derived)", EditorStyles.miniLabel, GUILayout.Width(60));
                    }
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        createdStats.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Value", stat.Value.ToString("F2"));
                    if (stat.IsDerived)
                    {
                        EditorGUILayout.LabelField("Formula", stat.Formula);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Base Value", stat.BaseValue.ToString("F2"));
                    }
                    EditorGUILayout.LabelField("Modifiers", stat.Modifiers.Count.ToString());
                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.EndVertical();
                }
            }
        }
        
        private void DrawFormulaTester()
        {
            EditorGUILayout.LabelField("Formula Tester", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Test formulas with custom stat values and see real-time results.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Formula Input
            EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
            var newFormula = EditorGUILayout.TextArea(testFormula, GUILayout.Height(60));
            if (newFormula != testFormula)
            {
                testFormula = newFormula;
                TestFormula();
            }
            
            EditorGUILayout.Space();
            
            // Stat Values
            EditorGUILayout.LabelField("Test Stat Values", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Stat"))
            {
                testStatNames.Add("newStat");
                testStatValues.Add(0f);
            }
            
            showFormulaHelp = EditorGUILayout.Toggle("Show Help", showFormulaHelp);
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < testStatNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                testStatNames[i] = EditorGUILayout.TextField(testStatNames[i], GUILayout.Width(100));
                
                EditorGUI.BeginChangeCheck();
                testStatValues[i] = EditorGUILayout.FloatField(testStatValues[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    TestFormula();
                }
                
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    testStatNames.RemoveAt(i);
                    testStatValues.RemoveAt(i);
                    TestFormula();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // Result
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(testResult, EditorStyles.textField, GUILayout.Height(30));
            
            if (showFormulaHelp)
            {
                EditorGUILayout.Space();
                DrawFormulaHelp();
            }
        }
        
        private void DrawDebugConsole()
        {
            EditorGUILayout.LabelField("Debug Console", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Real-time debugging of stat changes and events during gameplay.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            autoRefreshDebug = EditorGUILayout.Toggle("Auto Refresh", autoRefreshDebug);
            
            if (GUILayout.Button("Clear"))
            {
                debugMessages.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            debugScrollPosition = EditorGUILayout.BeginScrollView(debugScrollPosition, GUILayout.Height(400));
            
            foreach (var message in debugMessages.TakeLast(100)) // Show last 100 messages
            {
                EditorGUILayout.SelectableLabel(message, GUILayout.Height(18));
            }
            
            EditorGUILayout.EndScrollView();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Debug console is active during play mode.", MessageType.Warning);
            }
        }
        
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Configure StatForge editor behavior and debugging options.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            enableRuntimeDebugging = EditorGUILayout.Toggle("Enable Runtime Debugging", enableRuntimeDebugging);
            showPerformanceMetrics = EditorGUILayout.Toggle("Show Performance Metrics", showPerformanceMetrics);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                enableRuntimeDebugging = true;
                showPerformanceMetrics = false;
                autoRefreshDebug = true;
            }
        }
        
        private void DrawFormulaHelp()
        {
            EditorGUILayout.LabelField("Formula Help", EditorStyles.boldLabel);
            
            var helpText = @"Formula Syntax:
• Basic operators: +, -, *, /, ()
• Stat references: Use stat names directly (e.g., health, strength)
• Percentage: 50% health (50% of health value)
• Math functions: min(a,b), max(a,b), abs(x), floor(x), ceil(x), round(x)
• Trigonometry: sin(x), cos(x), tan(x) (x in degrees)

Examples:
• strength * 2 + 10
• health * 0.1 + strength * 2
• min(level * 0.02, 0.95)
• max(health - 50, 0)
• 25% health + 10";
            
            EditorGUILayout.TextArea(helpText, EditorStyles.helpBox);
        }
        
        private void CreateNewStat()
        {
            if (string.IsNullOrEmpty(newStatName))
            {
                EditorUtility.DisplayDialog("Error", "Stat name cannot be empty.", "OK");
                return;
            }
            
            if (createdStats.Any(s => s.Name == newStatName))
            {
                EditorUtility.DisplayDialog("Error", $"Stat '{newStatName}' already exists.", "OK");
                return;
            }
            
            Stat newStat;
            if (!string.IsNullOrEmpty(newStatFormula))
            {
                newStat = new Stat(newStatName, newStatFormula);
                newStat.BaseValue = newStatBaseValue;
            }
            else
            {
                newStat = new Stat(newStatName, newStatBaseValue, newStatMinValue, newStatMaxValue, newStatAllowModifiers);
            }
            
            createdStats.Add(newStat);
            
            AddDebugMessage($"Created stat: {newStatName} (Value: {newStat.Value})");
        }
        
        private void ValidateFormula()
        {
            if (string.IsNullOrEmpty(newStatFormula))
            {
                EditorUtility.DisplayDialog("Info", "No formula to validate.", "OK");
                return;
            }
            
            bool isValid = IndividualStatFormulaEvaluator.ValidateFormula(newStatFormula);
            var message = isValid ? "Formula is valid!" : "Formula has syntax errors.";
            var messageType = isValid ? "Info" : "Error";
            
            EditorUtility.DisplayDialog(messageType, message, "OK");
        }
        
        private void TestFormula()
        {
            if (string.IsNullOrEmpty(testFormula))
            {
                testResult = "";
                return;
            }
            
            try
            {
                // Create a dummy stat collection for testing
                var testCollection = new StatCollection();
                testCollection.Initialize();
                
                for (int i = 0; i < testStatNames.Count; i++)
                {
                    if (!string.IsNullOrEmpty(testStatNames[i]))
                    {
                        testCollection.Set(testStatNames[i], testStatValues[i]);
                    }
                }
                
                var result = IndividualStatFormulaEvaluator.Evaluate(testFormula, null, testCollection, null);
                testResult = $"Result: {result:F6}";
                
                // Also show extracted stat references
                var references = IndividualStatFormulaEvaluator.ExtractStatReferences(testFormula);
                if (references.Length > 0)
                {
                    testResult += $"\nReferences: {string.Join(", ", references)}";
                }
            }
            catch (System.Exception e)
            {
                testResult = $"Error: {e.Message}";
            }
        }
        
        private void OnStatChangedDebug(GameObject owner, string statName, float oldValue, float newValue)
        {
            if (enableRuntimeDebugging)
            {
                var ownerName = owner != null ? owner.name : "Unknown";
                AddDebugMessage($"[{System.DateTime.Now:HH:mm:ss}] Stat Changed: {ownerName}.{statName} {oldValue:F2} -> {newValue:F2}");
            }
        }
        
        private void OnModifierAddedDebug(GameObject owner, string statName, IStatModifier modifier)
        {
            if (enableRuntimeDebugging)
            {
                var ownerName = owner != null ? owner.name : "Unknown";
                AddDebugMessage($"[{System.DateTime.Now:HH:mm:ss}] Modifier Added: {ownerName}.{statName} +{modifier.Type} {modifier.Value}");
            }
        }
        
        private void OnModifierRemovedDebug(GameObject owner, string statName, IStatModifier modifier)
        {
            if (enableRuntimeDebugging)
            {
                var ownerName = owner != null ? owner.name : "Unknown";
                AddDebugMessage($"[{System.DateTime.Now:HH:mm:ss}] Modifier Removed: {ownerName}.{statName} -{modifier.Type} {modifier.Value}");
            }
        }
        
        private void AddDebugMessage(string message)
        {
            debugMessages.Add(message);
            
            // Keep only last 1000 messages to prevent memory issues
            if (debugMessages.Count > 1000)
            {
                debugMessages.RemoveAt(0);
            }
        }
    }
}