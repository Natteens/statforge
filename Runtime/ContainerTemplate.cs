#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    [CreateAssetMenu(fileName = "New Container Template", menuName = "Scriptable Objects/StatForge/Container Template")]
    public class ContainerTemplate : ScriptableObject
    {
        public string templateName = "";
        public string description = "";
        public List<StatType> statTypes;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(templateName))
                templateName = name;
        }
    }
}
#endif