#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    public class StatForgeEditorLogic
    {
        private const string RESOURCES_PATH = "Assets/Resources";
        private const string STATFORGE_PATH = "Assets/Resources/StatForge";
        private const string STATS_PATH = "Assets/Resources/StatForge/Stats";
        private const string CONTAINERS_PATH = "Assets/Resources/StatForge/Containers";
        
        private readonly StatForgeEditorCache cache;
        private readonly StatForgeEditorData data;
        private List<GameObject> cachedEntities;
        private double lastEntityScanTime;

        public StatForgeEditorLogic(StatForgeEditorData data, StatForgeEditorCache cache)
        {
            this.data = data;
            this.cache = cache;
        }

        public void Initialize()
        {
            EnsureDirectoriesExist();
            RefreshAll();
        }

        private void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(STATFORGE_PATH))
                AssetDatabase.CreateFolder("Assets/Resources", "StatForge");

            if (!AssetDatabase.IsValidFolder(STATS_PATH))
                AssetDatabase.CreateFolder("Assets/Resources/StatForge", "Stats");

            if (!AssetDatabase.IsValidFolder(CONTAINERS_PATH))
                AssetDatabase.CreateFolder("Assets/Resources/StatForge", "Containers");
        }

        public void RefreshAll()
        {
            cache.MarkDirty();
            data.AllStatTypes = cache.StatTypes;
            data.AllContainers = cache.Containers;
            data.AllCategories = cache.Categories;
        }

        public List<StatType> GetFilteredStats()
        {
            return cache.GetFilteredStats(data.SearchFilter, data.SelectedCategory);
        }

        public void StartCreateStat()
        {
            data.CurrentEdit = EditMode.CreateStat;
            data.IsCreatingNew = true;
            data.ResetStatEditor();
        }

        public void StartEditStat(StatType stat)
        {
            data.CurrentEdit = EditMode.EditStat;
            data.IsCreatingNew = false;
            data.EditingStatType = stat;
            LoadStatToEditor(stat);
        }

        private void LoadStatToEditor(StatType stat)
        {
            data.NewStatName = stat.DisplayName;
            data.NewStatShortName = stat.ShortName;
            data.NewStatCategory = stat.Category;
            data.NewStatFormula = stat.Formula;
            data.NewStatDescription = stat.Description;
            data.NewStatDefault = stat.DefaultValue;
            data.NewStatMin = stat.MinValue;
            data.NewStatMax = stat.MaxValue;
        }

        public void SaveStat()
        {
            if (string.IsNullOrEmpty(data.NewStatName))
            {
                EditorUtility.DisplayDialog("Error", "Name is required!", "OK");
                return;
            }

            StatType stat;
            string path;

            if (data.IsCreatingNew)
            {
                var fileName = data.NewStatName.Replace(" ", "").Replace("/", "_");
                path = $"{STATS_PATH}/{fileName}.asset";

                if (File.Exists(path)) path = AssetDatabase.GenerateUniqueAssetPath(path);

                stat = ScriptableObject.CreateInstance<StatType>();
                AssetDatabase.CreateAsset(stat, path);
            }
            else
            {
                stat = data.EditingStatType;
            }

            stat.DisplayName = data.NewStatName;
            stat.ShortName = data.NewStatShortName;
            stat.Category = data.NewStatCategory;
            stat.Formula = data.NewStatFormula;
            stat.Description = data.NewStatDescription;
            stat.DefaultValue = data.NewStatDefault;
            stat.MinValue = data.NewStatMin;
            stat.MaxValue = data.NewStatMax;

            EditorUtility.SetDirty(stat);
            AssetDatabase.SaveAssets();

            CancelEdit();
            RefreshAll();

            Debug.Log($"[StatForge] Stat '{data.NewStatName}' saved successfully!");
        }

        public void DeleteStat(StatType stat)
        {
            var path = AssetDatabase.GetAssetPath(stat);
            AssetDatabase.DeleteAsset(path);

            if (data.SelectedStat == stat)
                data.SelectedStat = null;

            RefreshAll();
            Debug.Log($"[StatForge] Stat '{stat.DisplayName}' deleted!");
        }

        public void StartCreateContainer()
        {
            data.CurrentEdit = EditMode.CreateContainer;
            data.IsCreatingNew = true;
            data.ResetContainerEditor();
        }

        public void StartEditContainer(StatContainerAsset container)
        {
            data.CurrentEdit = EditMode.EditContainer;
            data.IsCreatingNew = false;
            data.EditingContainer = container;
            LoadContainerToEditor(container);
        }

        private void LoadContainerToEditor(StatContainerAsset container)
        {
            data.NewContainerName = container.ContainerName;
            data.NewContainerDescription = container.Description;
            data.SelectedStats.Clear();
            data.SelectedStats.AddRange(container.StatTypes.Where(s => s != null));
        }

        public void SaveContainer()
        {
            if (string.IsNullOrEmpty(data.NewContainerName))
            {
                EditorUtility.DisplayDialog("Error", "Container name is required!", "OK");
                return;
            }

            if (data.SelectedStats.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Select at least one stat for the container!", "OK");
                return;
            }

            StatContainerAsset container;
            string path;

            if (data.IsCreatingNew)
            {
                var fileName = data.NewContainerName.Replace(" ", "").Replace("/", "_");
                path = $"{CONTAINERS_PATH}/{fileName}.asset";

                if (File.Exists(path)) path = AssetDatabase.GenerateUniqueAssetPath(path);

                container = ScriptableObject.CreateInstance<StatContainerAsset>();
                AssetDatabase.CreateAsset(container, path);
            }
            else
            {
                container = data.EditingContainer;
            }

            container.ContainerName = data.NewContainerName;
            container.Description = data.NewContainerDescription;
            container.StatTypes.Clear();
            container.StatTypes.AddRange(data.SelectedStats.Where(s => s != null));

            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StatForge] Container '{data.NewContainerName}' saved with {data.SelectedStats.Count} stats!");

            CancelEdit();
            RefreshAll();
        }

        public void DeleteContainer(StatContainerAsset container)
        {
            var path = AssetDatabase.GetAssetPath(container);
            AssetDatabase.DeleteAsset(path);

            if (data.SelectedContainer == container)
                data.SelectedContainer = null;

            RefreshAll();
            Debug.Log($"[StatForge] Container '{container.ContainerName}' deleted!");
        }

        public void CancelEdit()
        {
            data.Reset();
        }

        public void CreateRPGTemplate()
        {
            CreateTemplateStats(new[]
            {
                ("Constitution", "CON", "Attribute", 10f, 1f, 50f, ""),
                ("Strength", "STR", "Attribute", 10f, 1f, 50f, ""),
                ("Dexterity", "DEX", "Attribute", 10f, 1f, 50f, ""),
                ("Intelligence", "INT", "Attribute", 10f, 1f, 50f, ""),
                ("Health Points", "HP", "Combat", 100f, 1f, 999f, ""),
                ("Mana Points", "MP", "Combat", 50f, 0f, 999f, ""),
                ("Max Health", "MaxHP", "Derived", 100f, 1f, 9999f, "CON * 10 + STR * 2"),
                ("Max Mana", "MaxMP", "Derived", 50f, 1f, 9999f, "INT * 5")
            });

            Debug.Log("[StatForge] RPG Basic template created successfully!");
            RefreshAll();
        }

        public void CreateActionTemplate()
        {
            CreateTemplateStats(new[]
            {
                ("Health", "HP", "Core", 100f, 1f, 200f, ""),
                ("Armor", "ARM", "Core", 50f, 0f, 100f, ""),
                ("Damage", "DMG", "Core", 25f, 1f, 100f, ""),
                ("Speed", "SPD", "Core", 100f, 1f, 200f, "")
            });

            Debug.Log("[StatForge] Action Game template created successfully!");
            RefreshAll();
        }

        private void CreateTemplateStats(
            (string name, string shortName, string category, float defaultVal, float min, float max, string formula)[]
                statsData)
        {
            foreach (var statData in statsData)
            {
                var fileName = statData.name.Replace(" ", "").Replace("/", "_");
                var path = $"{STATS_PATH}/{fileName}.asset";

                if (File.Exists(path)) continue;

                var stat = ScriptableObject.CreateInstance<StatType>();
                stat.DisplayName = statData.name;
                stat.ShortName = statData.shortName;
                stat.Category = statData.category;
                stat.DefaultValue = statData.defaultVal;
                stat.MinValue = statData.min;
                stat.MaxValue = statData.max;
                stat.Formula = statData.formula;
                stat.Description = $"Template stat for {statData.name}";

                AssetDatabase.CreateAsset(stat, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public List<GameObject> FindEntitiesWithStats()
        {
            if (!Application.isPlaying)
            {
                cachedEntities?.Clear();
                return new List<GameObject>();
            }

            var currentTime = EditorApplication.timeSinceStartup;
            if (cachedEntities != null && currentTime - lastEntityScanTime < 1.0)
                return cachedEntities;

            if (cachedEntities == null)
                cachedEntities = new List<GameObject>();
            else
                cachedEntities.Clear();

            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var processedObjects = new HashSet<GameObject>();

            foreach (var obj in allObjects)
            {
                if (processedObjects.Contains(obj.gameObject)) continue;

                var fields = obj.GetType().GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                if (fields.Any(f => f.FieldType == typeof(Stat)))
                {
                    cachedEntities.Add(obj.gameObject);
                    processedObjects.Add(obj.gameObject);
                }
            }

            lastEntityScanTime = currentTime;
            return cachedEntities;
        }

        public void RefreshAllStats()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[StatForge] Refresh only works in runtime!");
                return;
            }

            var entities = FindEntitiesWithStats();
            var count = 0;

            foreach (var entity in entities)
            {
                var components = entity.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    var fields = component.GetType().GetFields(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance);

                    foreach (var field in fields)
                        if (field.FieldType == typeof(Stat))
                        {
                            var stat = field.GetValue(component) as Stat;
                            stat?.ForceRecalculate();
                            count++;
                        }
                }
            }

            Debug.Log($"[StatForge] {count} stats updated!");
        }

        public void LogPerformanceStats()
        {
            Debug.Log($"[StatForge] StatType Assets: {data.AllStatTypes.Count}");
            Debug.Log($"[StatForge] Container Assets: {data.AllContainers.Count}");
            Debug.Log($"[StatForge] Categories: {data.AllCategories.Length - 1}");

            if (Application.isPlaying)
            {
                var entities = FindEntitiesWithStats();
                Debug.Log($"[StatForge] Runtime Entities: {entities.Count}");
            }
        }
    }
}
#endif