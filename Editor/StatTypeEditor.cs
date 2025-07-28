#if UNITY_EDITOR
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
            
            EditorGUILayout.LabelField("Stat Type", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic Info
            statType.DisplayName = EditorGUILayout.TextField("Display Name", statType.DisplayName);
            
            EditorGUILayout.BeginHorizontal();
            statType.ShortName = EditorGUILayout.TextField("Short Name", statType.ShortName, GUILayout.Width(200));
            if (GUILayout.Button("Auto", GUILayout.Width(50)))
            {
                statType.ShortName = GenerateShortName(statType.DisplayName);
                EditorUtility.SetDirty(statType);
            }
            EditorGUILayout.EndHorizontal();
            
            statType.Category = (StatCategory)EditorGUILayout.EnumPopup("Category", statType.Category);
            
            EditorGUILayout.Space();
            
            // Values
            EditorGUILayout.LabelField("Values", EditorStyles.boldLabel);
            statType.DefaultValue = EditorGUILayout.FloatField("Default", statType.DefaultValue);
            statType.MinValue = EditorGUILayout.FloatField("Minimum", statType.MinValue);
            statType.MaxValue = EditorGUILayout.FloatField("Maximum", statType.MaxValue);
            
            // Derived formula
            if (statType.Category == StatCategory.Derived)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
                statType.Formula = EditorGUILayout.TextArea(statType.Formula, GUILayout.Height(60));
                
                if (!string.IsNullOrEmpty(statType.Formula))
                {
                    EditorGUILayout.HelpBox("Use stat ShortNames in formula (e.g., 'STR + DEX * 2')", MessageType.Info);
                }
            }
            
            // Validation
            if (statType.MinValue > statType.MaxValue)
            {
                EditorGUILayout.HelpBox("Min value is greater than Max value", MessageType.Warning);
            }
            
            if (string.IsNullOrEmpty(statType.DisplayName))
            {
                EditorGUILayout.HelpBox("Display Name is required", MessageType.Error);
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(statType);
            }
        }
        
        private string GenerateShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";
            
            var words = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var result = "";
            
            foreach (var word in words)
            {
                if (word.Length > 0)
                    result += word[0].ToString().ToUpper();
            }
            
            return result.Length > 5 ? result.Substring(0, 5) : result;
        }
    }
}
#endif