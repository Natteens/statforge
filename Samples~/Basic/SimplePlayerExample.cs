using UnityEngine;
using StatForge;

namespace StatForge.Examples
{
    /// <summary>
    /// Example demonstrating the new simplified StatForge API.
    /// </summary>
    public class SimplePlayerExample : MonoBehaviour
    {
        [Header("Stats using new [Stat] attribute")]
        [Stat] public float health = 100f;
        [Stat] public int level = 1;
        [Stat] public float mana = 50f;
        
        [Header("Derived Stats")]
        [DerivedStat("level * 10")] public float maxHealth;
        [DerivedStat("level * 5")] public float maxMana;
        
        void Start()
        {
            // Initialize stats system
            this.InitializeStats();
            
            // Demonstrate the ultra-simple API
            Debug.Log($"Initial Health: {this.GetStat("health")}");
            
            // Set stats using extension methods
            this.SetStat("health", 150f);
            Debug.Log($"Health after setting: {this.GetStat("health")}");
            
            // Apply temporary modifiers
            var healthBonus = this.ApplyTemporaryModifier("health", ModifierType.Additive, 25f, 5f);
            Debug.Log($"Health with temporary bonus: {this.GetStat("health")}");
            
            // Subscribe to stat change events
            StatEvents.OnStatChanged += OnStatChanged;
        }
        
        void Update()
        {
            // Test changing the field directly (should work with new system)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                health += 10f;
                Debug.Log($"Health after direct field change: {health}");
            }
            
            // Test level up
            if (Input.GetKeyDown(KeyCode.L))
            {
                level++;
                this.SetStat("level", level);
                Debug.Log($"Level up! New level: {level}");
            }
        }
        
        private void OnStatChanged(GameObject owner, string statName, float oldValue, float newValue)
        {
            if (owner == gameObject)
            {
                Debug.Log($"Stat changed: {statName} {oldValue} → {newValue}");
            }
        }
        
        void OnDestroy()
        {
            StatEvents.OnStatChanged -= OnStatChanged;
        }
    }
}