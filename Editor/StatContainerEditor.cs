#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    [CustomEditor(typeof(StatContainer))]
    public class StatContainerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var container = (StatContainer)target;

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Container Configuration", EditorStyles.boldLabel);
            container.ContainerName = EditorGUILayout.TextField("Nome", container.ContainerName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Descrição", EditorStyles.boldLabel);
            container.Description = EditorGUILayout.TextArea(container.Description, GUILayout.Height(60));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField($"Stats ({container.StatTypes.Count})", EditorStyles.boldLabel);

            var statsProp = serializedObject.FindProperty("statTypes");
            EditorGUILayout.PropertyField(statsProp, true);

            EditorGUILayout.Space(10);

            if (container.StatTypes.Count > 0)
            {
                EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);

                var style = new GUIStyle("box") { padding = new RectOffset(10, 10, 8, 8) };
                EditorGUILayout.BeginVertical(style);

                foreach (var stat in container.StatTypes)
                    if (stat != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {stat.DisplayName} ({stat.ShortName})", GUILayout.Width(200));
                        EditorGUILayout.LabelField($"Padrão: {stat.DefaultValue:F1}", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            if (Application.isPlaying && GUILayout.Button("Testar Container"))
            {
                var runtimeContainer = container.CreateRuntimeContainer();
                Debug.Log(
                    $"[StatForge] Container '{container.ContainerName}' criado com {runtimeContainer.Count} stats");

                foreach (var stat in runtimeContainer.Stats) Debug.Log($"  - {stat.Name}: {stat.Value}");
            }

            if (GUI.changed) EditorUtility.SetDirty(container);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif