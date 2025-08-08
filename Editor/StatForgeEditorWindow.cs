#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeEditorWindow : EditorWindow
    {
        private StatForgeEditorCache cache;
        private StatForgeEditorData data;
        private StatForgeEditorGUI gui;
        private StatForgeEditorLogic logic;

        private void OnEnable()
        {
            if (data == null) data = new StatForgeEditorData();
            if (cache == null) cache = new StatForgeEditorCache();
            if (logic == null) logic = new StatForgeEditorLogic(data, cache);
            if (gui == null) gui = new StatForgeEditorGUI(data, logic);

            logic.Initialize();
            
            EditorApplication.projectChanged += OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            
            cache?.Clear();
        }

        private void OnProjectChanged()
        {
            cache?.MarkDirty();
        }

        private void OnBeforeAssemblyReload()
        {
            cache?.Clear();
        }

        private void OnGUI()
        {
            if (data == null || gui == null) OnEnable();

            gui?.DrawWindow(position);
        }

        [MenuItem("Tools/StatForge")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatForgeEditorWindow>("StatForge");
            window.minSize = new Vector2(1200f, 800f);
            window.Show();
        }
    }
}
#endif