#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace StatForge.Tests
{
    [InitializeOnLoad]
    public static class TestInitializer
    {
        static TestInitializer()
        {
            Debug.Log("[StatForge] Test environment initialized");
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying)
                {
                    Debug.Log("[StatForge] Test environment ready for unit tests");
                }
            };
        }
    }
}
#endif