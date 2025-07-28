#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomEditor(typeof(AttributeSystem))]
    public class AttributeSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var system = (AttributeSystem)target;
            
            DrawDefaultInspector();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Runtime features available only in play mode", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            DrawRuntimeControls(system);
            DrawStats(system);
        }
        
        private void DrawRuntimeControls(AttributeSystem system)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Available Points: {system.AvailablePoints}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("+5", GUILayout.Width(30)))
                system.AddAvailablePoints(5);
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
                system.ResetAllocatedPoints();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawStats(AttributeSystem system)
        {
            var primaryStats = system.GetPrimaryStats();
            if (primaryStats.Count > 0)
            {
                EditorGUILayout.LabelField("Primary Stats", EditorStyles.boldLabel);
                foreach (var stat in primaryStats)
                    DrawStat(system, stat, true);
                EditorGUILayout.Space();
            }
            
            var derivedStats = system.GetDerivedStats();
            if (derivedStats.Count > 0)
            {
                EditorGUILayout.LabelField("Derived Stats", EditorStyles.boldLabel);
                foreach (var stat in derivedStats)
                    DrawStat(system, stat, false);
                EditorGUILayout.Space();
            }
            
            var externalStats = system.GetExternalStats();
            if (externalStats.Count > 0)
            {
                EditorGUILayout.LabelField("External Stats", EditorStyles.boldLabel);
                foreach (var stat in externalStats)
                    DrawStat(system, stat, false);
            }
        }
        
        private void DrawStat(AttributeSystem system, StatValue stat, bool canAllocate)
        {
            if (stat?.statType == null) return;
            
            EditorGUILayout.BeginHorizontal("box");
            
            // Name and value
            var value = system.GetStatValue(stat.statType);
            EditorGUILayout.LabelField(stat.statType.DisplayName, GUILayout.Width(100));
            EditorGUILayout.LabelField($"{value:F1}", EditorStyles.boldLabel, GUILayout.Width(40));
            
            // Details
            var details = $"(B:{stat.baseValue:F0}";
            if (stat.allocatedPoints > 0) details += $" A:{stat.allocatedPoints:F0}";
            if (stat.bonusValue != 0) details += $" Bo:{stat.bonusValue:F0}";
            var temp = system.GetTemporaryBonus(stat.statType);
            if (temp != 0) details += $" T:{temp:F0}";
            details += ")";
            
            EditorGUILayout.LabelField(details, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            // Controls
            if (canAllocate)
            {
                GUI.enabled = system.CanAllocatePoint(stat.statType);
                if (GUILayout.Button("+", GUILayout.Width(25)))
                    system.AllocatePoint(stat.statType);
                
                GUI.enabled = system.CanDeallocatePoint(stat.statType);
                if (GUILayout.Button("-", GUILayout.Width(25)))
                    system.DeallocatePoint(stat.statType);
                
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif