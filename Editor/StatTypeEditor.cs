#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomEditor(typeof(StatType))]
    public class StatTypeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var statType = (StatType)target;

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            statType.DisplayName = EditorGUILayout.TextField("Name", statType.DisplayName);

            EditorGUILayout.BeginHorizontal();
            statType.ShortName = EditorGUILayout.TextField("Short Name", statType.ShortName);
            if (GUILayout.Button("Auto", GUILayout.Width(50)))
            {
                statType.ShortName = GenerateShortName(statType.DisplayName);
                EditorUtility.SetDirty(statType);
            }
            EditorGUILayout.EndHorizontal();

            statType.Category = EditorGUILayout.TextField("Category", statType.Category);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Value Type", EditorStyles.boldLabel);
            var oldValueType = statType.ValueType;
            statType.ValueType = (StatValueType)EditorGUILayout.EnumPopup("Type", statType.ValueType);

            if (oldValueType != statType.ValueType)
            {
                statType.AutoAdjustRangeForType();
                EditorUtility.SetDirty(statType);
            }

            var previewStyle = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(previewStyle);
            EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
            var exampleValue = statType.ValueType == StatValueType.Percentage ? 25.5f : 150.75f;
            EditorGUILayout.LabelField($"Example: {statType.FormatValue(exampleValue)}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Value Configuration", EditorStyles.boldLabel);
            statType.DefaultValue = EditorGUILayout.FloatField("Default Value", statType.DefaultValue);

            if (statType.ValueType == StatValueType.Percentage)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Percentage Limits", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum", GUILayout.Width(60));
                statType.MinValue = EditorGUILayout.FloatField(statType.MinValue, GUILayout.Width(80));

                GUILayout.Space(20);

                EditorGUILayout.LabelField("Maximum", GUILayout.Width(60));
                statType.MaxValue = EditorGUILayout.FloatField(statType.MaxValue, GUILayout.Width(80));

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Reset to 0-100%"))
                {
                    statType.MinValue = 0f;
                    statType.MaxValue = 100f;
                    EditorUtility.SetDirty(statType);
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Formula (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use abbreviations (e.g. CON * 15 + STR * 2)", EditorStyles.miniLabel);
            statType.Formula = EditorGUILayout.TextArea(statType.Formula, GUILayout.Height(60));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            statType.Description = EditorGUILayout.TextArea(statType.Description, GUILayout.Height(80));

            if (GUI.changed) EditorUtility.SetDirty(statType);
        }

        private string GenerateShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";

            var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = "";

            foreach (var word in words)
                if (word.Length > 0)
                    result += word[0].ToString().ToUpper();

            return result.Length > 4 ? result.Substring(0, 4) : result;
        }
    }
}
#endif