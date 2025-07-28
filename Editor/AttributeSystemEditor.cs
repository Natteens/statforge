#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomEditor(typeof(AttributeSystem))]
    public class AttributeSystemEditor : UnityEditor.Editor
    {
        private AttributeSystem attributeSystem;

        private void OnEnable()
        {
            attributeSystem = (AttributeSystem)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Runtime features are only available during play mode.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Available Points:", GUILayout.Width(120));
            EditorGUILayout.LabelField(attributeSystem.AvailablePoints.ToString(), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add 5", GUILayout.Width(60)))
                attributeSystem.AddAvailablePoints(5);
            if (GUILayout.Button("Add 10", GUILayout.Width(60)))
                attributeSystem.AddAvailablePoints(10);
            if (GUILayout.Button("Reset All", GUILayout.Width(70)))
                if (EditorUtility.DisplayDialog("Reset Points", "Reset all allocated points?", "Yes", "No"))
                    attributeSystem.ResetAllocatedPoints();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            var primaryStats = attributeSystem.GetPrimaryStats();
            if (primaryStats.Count > 0)
            {
                EditorGUILayout.LabelField("Primary Stats", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                foreach (var stat in primaryStats) DrawStatRow(stat, true);

                EditorGUILayout.Space(10);
            }

            var derivedStats = attributeSystem.GetDerivedStats();
            if (derivedStats.Count > 0)
            {
                EditorGUILayout.LabelField("Derived Stats", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                foreach (var stat in derivedStats) DrawStatRow(stat, false);

                EditorGUILayout.Space(10);
            }

            var externalStats = attributeSystem.GetExternalStats();
            if (externalStats.Count > 0)
            {
                EditorGUILayout.LabelField("External Stats", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                foreach (var stat in externalStats) DrawStatRow(stat, false);
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void DrawStatRow(StatValue stat, bool allowAllocation)
        {
            if (stat.statType == null) return;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(stat.statType.DisplayName, EditorStyles.boldLabel, GUILayout.Width(120));

            var currentValue = attributeSystem.GetStatValue(stat.statType);
            EditorGUILayout.LabelField($"{currentValue:F1}", EditorStyles.boldLabel, GUILayout.Width(40));

            var details = $"(Base: {stat.baseValue:F1}";
            if (stat.allocatedPoints > 0)
                details += $" + Alloc: {stat.allocatedPoints:F1}";
            if (stat.bonusValue != 0)
                details += $" + Bonus: {stat.bonusValue:F1}";
            var tempBonus = attributeSystem.GetTemporaryBonus(stat.statType);
            if (tempBonus != 0)
                details += $" + Temp: {tempBonus:F1}";
            details += ")";

            EditorGUILayout.LabelField(details, EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (allowAllocation)
            {
                GUI.enabled = attributeSystem.CanAllocatePoint(stat.statType);
                if (GUILayout.Button("+", GUILayout.Width(25)))
                    attributeSystem.AllocatePoint(stat.statType);

                GUI.enabled = attributeSystem.CanDeallocatePoint(stat.statType);
                if (GUILayout.Button("-", GUILayout.Width(25)))
                    attributeSystem.DeallocatePoint(stat.statType);

                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Temp Bonus:", GUILayout.Width(80));
            var newTempBonus = EditorGUILayout.FloatField(tempBonus, GUILayout.Width(50));
            if (!Mathf.Approximately(newTempBonus, tempBonus))
                attributeSystem.SetTemporaryBonus(stat.statType, newTempBonus);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }
    }
}
#endif