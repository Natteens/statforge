#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace StatForge.Editor
{
    public enum ViewMode
    {
        Stats,
        Containers,
        Testing,
        Settings
    }

    public enum EditMode
    {
        None,
        CreateStat,
        EditStat,
        CreateContainer,
        EditContainer
    }

    public class StatForgeEditorData
    {
        public static readonly Color SidebarColor = new(0.22f, 0.22f, 0.22f);
        public static readonly Color HeaderColor = new(0.19f, 0.19f, 0.19f);
        public static readonly Color SelectedColor = new(0.24f, 0.49f, 0.91f, 0.3f);
        public string[] AllCategories = new string[0];

        public List<StatContainerAsset> AllContainers = new();
        public List<StatType> AllStatTypes = new();
        public EditMode CurrentEdit = EditMode.None;
        public ViewMode CurrentView = ViewMode.Stats;
        public StatContainerAsset EditingContainer;

        public StatType EditingStatType;
        public Vector2 EditScrollPos;
        public bool IsCreatingNew;
        public string NewContainerDescription = "";

        public string NewContainerName = "";
        public string NewStatCategory = "General";
        public float NewStatDefault;
        public string NewStatDescription = "";
        public string NewStatFormula = "";
        public float NewStatMax = 100f;
        public float NewStatMin;

        public string NewStatName = "";
        public string NewStatShortName = "";
        public Vector2 ScrollPos;
        public string SearchFilter = "";
        public string SelectedCategory = "All";
        public StatContainerAsset SelectedContainer;

        public StatType SelectedStat;
        public List<StatType> SelectedStats = new();

        public void Reset()
        {
            CurrentEdit = EditMode.None;
            EditingStatType = null;
            EditingContainer = null;
            IsCreatingNew = false;
            ResetStatEditor();
            ResetContainerEditor();
        }

        public void ResetStatEditor()
        {
            NewStatName = "";
            NewStatShortName = "";
            NewStatCategory = "General";
            NewStatFormula = "";
            NewStatDescription = "";
            NewStatDefault = 0f;
            NewStatMin = 0f;
            NewStatMax = 100f;
        }

        public void ResetContainerEditor()
        {
            NewContainerName = "";
            NewContainerDescription = "";
            SelectedStats.Clear();
        }
    }
}
#endif