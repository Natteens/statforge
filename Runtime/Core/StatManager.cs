using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Global manager for updating stat modifiers with duration.
    /// Automatically created and managed, no manual setup required.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    internal class StatManager : MonoBehaviour
    {
        private static StatManager _instance;
        private static readonly List<GameObject> _registeredObjects = new List<GameObject>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("[StatManager]");
                go.hideFlags = HideFlags.HideInHierarchy;
                _instance = go.AddComponent<StatManager>();
                DontDestroyOnLoad(go);
            }
        }
        
        private void Update()
        {
            var deltaTime = Time.deltaTime;
            
            // Update all registered objects
            for (int i = _registeredObjects.Count - 1; i >= 0; i--)
            {
                var obj = _registeredObjects[i];
                if (obj == null)
                {
                    _registeredObjects.RemoveAt(i);
                    continue;
                }
                
                obj.UpdateStats(deltaTime);
            }
        }
        
        /// <summary>
        /// Register a GameObject for automatic stat updates.
        /// </summary>
        internal static void Register(GameObject gameObject)
        {
            if (gameObject != null && !_registeredObjects.Contains(gameObject))
            {
                _registeredObjects.Add(gameObject);
            }
        }
        
        /// <summary>
        /// Unregister a GameObject from automatic stat updates.
        /// </summary>
        internal static void Unregister(GameObject gameObject)
        {
            _registeredObjects.Remove(gameObject);
        }
    }
}