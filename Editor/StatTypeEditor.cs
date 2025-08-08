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

            EditorGUILayout.LabelField("Informações Básicas", EditorStyles.boldLabel);
            statType.DisplayName = EditorGUILayout.TextField("Nome", statType.DisplayName);

            EditorGUILayout.BeginHorizontal();
            statType.ShortName = EditorGUILayout.TextField("Abreviação", statType.ShortName);
            if (GUILayout.Button("Auto", GUILayout.Width(50)))
            {
                statType.ShortName = GenerateShortName(statType.DisplayName);
                EditorUtility.SetDirty(statType);
            }

            EditorGUILayout.EndHorizontal();

            statType.Category = EditorGUILayout.TextField("Categoria", statType.Category);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Configuração de Valores", EditorStyles.boldLabel);
            statType.DefaultValue = EditorGUILayout.FloatField("Valor Padrão", statType.DefaultValue);

            // Layout corrigido para Min e Max
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mínimo", GUILayout.Width(60));
            statType.MinValue = EditorGUILayout.FloatField(statType.MinValue, GUILayout.Width(80));

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Máximo", GUILayout.Width(60));
            statType.MaxValue = EditorGUILayout.FloatField(statType.MaxValue, GUILayout.Width(80));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Fórmula (Opcional)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use abreviações (ex: CON * 15 + STR * 2)", EditorStyles.miniLabel);
            statType.Formula = EditorGUILayout.TextArea(statType.Formula, GUILayout.Height(60));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Descrição", EditorStyles.boldLabel);
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