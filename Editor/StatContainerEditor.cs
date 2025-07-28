#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace StatForge.Editor
{
    [CustomEditor(typeof(StatContainer))]
    public class StatContainerEditor : UnityEditor.Editor
    {
        private StatType statToAdd;
        
        public override void OnInspectorGUI()
        {
            var container = (StatContainer)target;
            
            EditorGUILayout.LabelField("Stat Container", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic Info
            container.ContainerName = EditorGUILayout.TextField("Name", container.ContainerName);
            container.Category = (ContainerCategory)EditorGUILayout.EnumPopup("Category", container.Category);
            
            EditorGUILayout.LabelField("Description");
            container.Description = EditorGUILayout.TextArea(container.Description, GUILayout.Height(40));
            
            EditorGUILayout.Space();
            
            // Auto-populate settings
            EditorGUILayout.LabelField("Auto-populate", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            var autoPopulatePrimary = EditorGUILayout.Toggle("Primary Stats", 
                serializedObject.FindProperty("autoPopulatePrimary").boolValue);
            var autoPopulateDerived = EditorGUILayout.Toggle("Derived Stats", 
                serializedObject.FindProperty("autoPopulateDerived").boolValue);
            var autoPopulateExternal = EditorGUILayout.Toggle("External Stats", 
                serializedObject.FindProperty("autoPopulateExternal").boolValue);
            
            serializedObject.FindProperty("autoPopulatePrimary").boolValue = autoPopulatePrimary;
            serializedObject.FindProperty("autoPopulateDerived").boolValue = autoPopulateDerived;
            serializedObject.FindProperty("autoPopulateExternal").boolValue = autoPopulateExternal;
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            // Stats management
            EditorGUILayout.LabelField($"Stats ({container.Stats?.Count ?? 0})", EditorStyles.boldLabel);
            
            // Add stat
            EditorGUILayout.BeginHorizontal();
            statToAdd = (StatType)EditorGUILayout.ObjectField("Add Stat", statToAdd, typeof(StatType), false);
            
            GUI.enabled = statToAdd != null && !container.HasStat(statToAdd);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                container.AddStat(statToAdd, statToAdd.DefaultValue);
                statToAdd = null;
                EditorUtility.SetDirty(container);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Stats list
            if (container.Stats != null && container.Stats.Count > 0)
            {
                DrawStatsList(container);
            }
            else
            {
                EditorGUILayout.HelpBox("No stats in container", MessageType.Info);
            }
            
            // Initialize button
            EditorGUILayout.Space();
            if (GUILayout.Button("Initialize Container"))
            {
                container.Initialize();
                EditorUtility.SetDirty(container);
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(container);
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void DrawStatsList(StatContainer container)
        {
            var statsToRemove = new System.Collections.Generic.List<StatValue>();
            
            // Group by category
            var primary = container.Stats.Where(s => s.statType?.Category == StatCategory.Primary).ToList();
            var derived = container.Stats.Where(s => s.statType?.Category == StatCategory.Derived).ToList();
            var external = container.Stats.Where(s => s.statType?.Category == StatCategory.External).ToList();
            
            if (primary.Count > 0)
            {
                EditorGUILayout.LabelField("Primary", EditorStyles.boldLabel);
                foreach (var stat in primary)
                    if (DrawStat(stat))
                        statsToRemove.Add(stat);
                EditorGUILayout.Space();
            }
            
            if (derived.Count > 0)
            {
                EditorGUILayout.LabelField("Derived", EditorStyles.boldLabel);
                foreach (var stat in derived)
                    if (DrawStat(stat))
                        statsToRemove.Add(stat);
                EditorGUILayout.Space();
            }
            
            if (external.Count > 0)
            {
                EditorGUILayout.LabelField("External", EditorStyles.boldLabel);
                foreach (var stat in external)
                    if (DrawStat(stat))
                        statsToRemove.Add(stat);
            }
            
            // Remove marked stats
            foreach (var stat in statsToRemove)
            {
                container.RemoveStat(stat.statType);
                EditorUtility.SetDirty(container);
            }
        }
        
        private bool DrawStat(StatValue stat)
        {
            if (stat?.statType == null) return true;
            
            EditorGUILayout.BeginHorizontal("box");
            
            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(stat.statType.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"({stat.statType.ShortName}) - Total: {stat.TotalValue:F1}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // Base value
            EditorGUILayout.LabelField("Base:", GUILayout.Width(35));
            var newBase = EditorGUILayout.FloatField(stat.baseValue, GUILayout.Width(50));
            if (!Mathf.Approximately(newBase, stat.baseValue))
            {
                stat.SetBaseValue(newBase);
            }
            
            // Remove button
            bool shouldRemove = GUILayout.Button("×", GUILayout.Width(20));
            
            EditorGUILayout.EndHorizontal();
            
            return shouldRemove;
        }
    }
}
#endif