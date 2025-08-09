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
            data.SelectedStat = null; 
        }

        public void StartEditStat(StatType stat)
        {
            data.CurrentEdit = EditMode.EditStat;
            data.IsCreatingNew = false;
            data.EditingStatType = stat;
            data.SelectedStat = null; 
            LoadStatToEditor(stat);
        }

        public void DuplicateStat(StatType originalStat)
        {
            data.CurrentEdit = EditMode.CreateStat;
            data.IsCreatingNew = true;
            data.SelectedStat = null;
            
            data.NewStatName = $"{originalStat.DisplayName} Copy";
            data.NewStatShortName = $"{originalStat.ShortName}C";
            data.NewStatCategory = originalStat.Category;
            data.NewStatValueType = originalStat.ValueType;
            data.NewStatFormula = originalStat.Formula;
            data.NewStatDescription = originalStat.Description;
            data.NewStatDefault = originalStat.DefaultValue;
            data.NewStatMin = originalStat.MinValue;
            data.NewStatMax = originalStat.MaxValue;
        }

        private void LoadStatToEditor(StatType stat)
        {
            data.NewStatName = stat.DisplayName;
            data.NewStatShortName = stat.ShortName;
            data.NewStatCategory = stat.Category;
            data.NewStatValueType = stat.ValueType;
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
            stat.ValueType = data.NewStatValueType;
            stat.Formula = data.NewStatFormula;
            stat.Description = data.NewStatDescription;
            stat.DefaultValue = data.NewStatDefault;
            
            if (data.NewStatValueType == StatValueType.Percentage)
            {
                stat.MinValue = data.NewStatMin;
                stat.MaxValue = data.NewStatMax;
            }

            stat.AutoAdjustRangeForType();

            EditorUtility.SetDirty(stat);
            AssetDatabase.SaveAssets();

            CancelEdit();
            RefreshAll();
        }

        public void DeleteStat(StatType stat)
        {
            var path = AssetDatabase.GetAssetPath(stat);
            AssetDatabase.DeleteAsset(path);

            if (data.SelectedStat == stat)
                data.SelectedStat = null;

            RefreshAll();
        }

        public void StartCreateContainer()
        {
            data.CurrentEdit = EditMode.CreateContainer;
            data.IsCreatingNew = true;
            data.ResetContainerEditor();
            data.SelectedContainer = null;
        }

        public void StartEditContainer(StatContainerAsset container)
        {
            data.CurrentEdit = EditMode.EditContainer;
            data.IsCreatingNew = false;
            data.EditingContainer = container;
            data.SelectedContainer = null; 
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
        }

        public void CancelEdit()
        {
            data.Reset();
        }

        public void ValidateAllStats()
        {
            var issues = new List<string>();
            var stats = data.AllStatTypes;
            
            foreach (var stat in stats)
            {
                if (string.IsNullOrEmpty(stat.DisplayName))
                    issues.Add($"Stat '{stat.name}' has no display name");
                    
                if (string.IsNullOrEmpty(stat.ShortName))
                    issues.Add($"Stat '{stat.DisplayName}' has no short name");
                    
                if (stat.HasFormula && string.IsNullOrEmpty(stat.Formula))
                    issues.Add($"Stat '{stat.DisplayName}' has empty formula");
                    
                if (stat.ValueType == StatValueType.Percentage)
                {
                    if (stat.MinValue > stat.MaxValue)
                        issues.Add($"Stat '{stat.DisplayName}' has min > max");
                        
                    if (stat.DefaultValue < stat.MinValue || stat.DefaultValue > stat.MaxValue)
                        issues.Add($"Stat '{stat.DisplayName}' default value outside range");
                }
            }
            
            var duplicateNames = stats.GroupBy(s => s.DisplayName).Where(g => g.Count() > 1);
            foreach (var group in duplicateNames)
            {
                issues.Add($"Duplicate display name: '{group.Key}'");
            }
            
            var duplicateShorts = stats.GroupBy(s => s.ShortName).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key));
            foreach (var group in duplicateShorts)
            {
                issues.Add($"Duplicate short name: '{group.Key}'");
            }
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "All stats are valid! No issues found.", "OK");
            }
            else
            {
                var message = "Found validation issues:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }

        public string GenerateShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";

            var words = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var result = "";

            foreach (var word in words)
                if (word.Length > 0)
                    result += word[0].ToString().ToUpper();

            return result.Length > 4 ? result.Substring(0, 4) : result;
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
                return;
            }

            var entities = FindEntitiesWithStats();

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
                        }
                }
            }
        }
    }
}
#endif