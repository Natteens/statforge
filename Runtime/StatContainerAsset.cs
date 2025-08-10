using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    public class StatContainerAsset : ScriptableObject
    {
        [Header("Container Configuration")]
        [SerializeField] private string containerName = "New Container";
        [SerializeField] private List<StatType> statTypes = new List<StatType>();
        
        [Header("Description")]
        [TextArea(3, 5)]
        [SerializeField] private string description = "";
        
        public string ContainerName 
        { 
            get => string.IsNullOrEmpty(containerName) ? name : containerName;
            set => containerName = value;
        }
        
        public List<StatType> StatTypes => statTypes;
        public string Description { get => description; set => description = value; }
        
        public StatContainer CreateRuntimeContainer()
        {
            var container = new StatContainer(ContainerName);
            
            foreach (var statType in statTypes)
            {
                if (statType != null)
                {
                    var stat = new Stat(statType, statType.DefaultValue);
                    container.AddStat(stat);
                }
            }
            
            container.Initialize();
            return container;
        }
        
        public void PopulateContainer(StatContainer container)
        {
            container.ClearStats();
            
            foreach (var statType in statTypes)
            {
                if (statType != null)
                {
                    var stat = new Stat(statType, statType.DefaultValue);
                    container.AddStat(stat);
                }
            }
            
            container.Initialize();
        }
        
        private void OnValidate()
        {
            for (int i = statTypes.Count - 1; i >= 0; i--)
            {
                if (statTypes[i] == null)
                {
                    statTypes.RemoveAt(i);
                }
            }
        }
    }
}