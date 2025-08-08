#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace StatForge.Editor
{
    public class StatForgeEditorCache
    {
        private string[] cachedCategories;
        private List<StatContainerAsset> cachedContainers;
        private List<StatType> cachedStatTypes;
        private Dictionary<string, List<StatType>> filteredStatsCache;
        private bool isDirty = true;

        public List<StatType> StatTypes
        {
            get
            {
                if (isDirty || cachedStatTypes == null)
                    RefreshStatTypes();
                return cachedStatTypes;
            }
        }

        public List<StatContainerAsset> Containers
        {
            get
            {
                if (isDirty || cachedContainers == null)
                    RefreshContainers();
                return cachedContainers;
            }
        }

        public string[] Categories
        {
            get
            {
                if (isDirty || cachedCategories == null)
                    RefreshCategories();
                return cachedCategories;
            }
        }

        public void MarkDirty()
        {
            isDirty = true;
            filteredStatsCache?.Clear();
        }

        public List<StatType> GetFilteredStats(string searchFilter, string selectedCategory)
        {
            var key = $"{searchFilter}|{selectedCategory}";

            if (filteredStatsCache == null)
                filteredStatsCache = new Dictionary<string, List<StatType>>();

            if (filteredStatsCache.TryGetValue(key, out var cached) && !isDirty)
                return cached;

            var stats = StatTypes;
            var result = stats;

            if (!string.IsNullOrEmpty(searchFilter))
            {
                var filter = searchFilter.ToLower();
                result = stats.Where(s =>
                    s.DisplayName.ToLower().Contains(filter) ||
                    s.ShortName.ToLower().Contains(filter) ||
                    s.Category.ToLower().Contains(filter)).ToList();
            }

            if (selectedCategory != "All") result = result.Where(s => s.Category == selectedCategory).ToList();

            filteredStatsCache[key] = result;
            return result;
        }

        private void RefreshStatTypes()
        {
            cachedStatTypes = new List<StatType>();
            var guids = AssetDatabase.FindAssets("t:StatType");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var stat = AssetDatabase.LoadAssetAtPath<StatType>(path);
                if (stat != null) cachedStatTypes.Add(stat);
            }
        }

        private void RefreshContainers()
        {
            cachedContainers = new List<StatContainerAsset>();
            var guids = AssetDatabase.FindAssets("t:StatContainerAsset");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var container = AssetDatabase.LoadAssetAtPath<StatContainerAsset>(path);
                if (container != null) cachedContainers.Add(container);
            }
        }

        private void RefreshCategories()
        {
            var categories = StatTypes.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, "All");
            cachedCategories = categories.ToArray();
        }

        public void Clear()
        {
            cachedStatTypes?.Clear();
            cachedContainers?.Clear();
            cachedCategories = null;
            filteredStatsCache?.Clear();
            isDirty = true;
        }
    }
}
#endif