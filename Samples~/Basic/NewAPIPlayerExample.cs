using UnityEngine;
using StatForge;

namespace StatForge.Examples
{
    /// <summary>
    /// Example demonstrating the new ultra-simplified StatForge API with individual Stat objects.
    /// This is the target API described in the problem statement.
    /// </summary>
    public class NewAPIPlayerExample : MonoBehaviour
    {
        [Header("Individual Stats with New API")]
        [Stat] public Stat health = new Stat("Health", 100f);
        [Stat] public Stat mana = new Stat("Mana", 50f);
        [Stat] public Stat strength = new Stat("Strength", 10f);
        [Stat] public Stat level = new Stat("Level", 1f);
        
        [Header("Derived Stats with Formulas")]
        [Stat] public Stat damage = new Stat("Damage", "health * 0.1 + strength * 2");
        [Stat] public Stat maxHealth = new Stat("MaxHealth", "level * 10 + 50");
        [Stat] public Stat critChance = new Stat("CritChance", "min(level * 0.02, 0.95)");
        
        void Start()
        {
            // Initialize the stats system
            this.InitializeStats();
            
            Debug.Log("=== New StatForge API Demo ===");
            
            // Ultra-simple API usage
            Debug.Log($"Initial Health: {health.Value}");
            Debug.Log($"Initial Damage: {damage.Value}");
            Debug.Log($"Initial Max Health: {maxHealth.Value}");
            
            // Setting values is extremely simple
            health.Value = 80f;
            Debug.Log($"Health after setting to 80: {health.Value}");
            
            // Adding modifiers is straightforward
            var healthBonus = health.AddTemporaryBonus(25f, 5f); // +25 for 5 seconds
            Debug.Log($"Health with temporary bonus: {health.Value}");
            
            var damageMultiplier = damage.AddTemporaryMultiplier(1.5f, 3f); // x1.5 for 3 seconds
            Debug.Log($"Damage with multiplier: {damage.Value}");
            
            // Level up affects derived stats automatically
            level.Value = 5f;
            Debug.Log($"After leveling to 5:");
            Debug.Log($"  Max Health: {maxHealth.Value}");
            Debug.Log($"  Crit Chance: {critChance.Value}");
            Debug.Log($"  Damage: {damage.Value}");
            
            // Subscribe to individual stat events
            health.OnValueChanged += (oldValue, newValue) => 
            {
                Debug.Log($"Health changed: {oldValue:F1} -> {newValue:F1}");
                UpdateHealthBar(newValue / maxHealth.Value);
            };
            
            strength.OnValueChanged += (oldValue, newValue) => 
            {
                Debug.Log($"Strength changed: {oldValue:F1} -> {newValue:F1} (affects damage)");
            };
            
            // Subscribe to global events
            StatEvents.OnStatChanged += OnAnyStatChanged;
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Test direct value modification
                health.Value += 10f;
                Debug.Log($"Health increased to: {health.Value}");
            }
            
            if (Input.GetKeyDown(KeyCode.L))
            {
                // Level up
                level.Value += 1f;
                Debug.Log($"Level up! New level: {level.Value}");
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                // Increase strength
                strength.Value += 2f;
                Debug.Log($"Strength training! New strength: {strength.Value}");
            }
            
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Heal to full
                health.Value = maxHealth.Value;
                Debug.Log($"Full heal! Health: {health.Value}");
            }
        }
        
        private void OnAnyStatChanged(GameObject owner, string statName, float oldValue, float newValue)
        {
            if (owner == gameObject)
            {
                Debug.Log($"[Global Event] {statName}: {oldValue:F1} -> {newValue:F1}");
            }
        }
        
        private void UpdateHealthBar(float percentage)
        {
            // Simulate updating a health bar
            Debug.Log($"Health bar updated: {percentage * 100:F0}%");
        }
        
        void OnDestroy()
        {
            StatEvents.OnStatChanged -= OnAnyStatChanged;
        }
        
        [Header("Runtime Testing")]
        [SerializeField] private bool showDebugInfo = true;
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("StatForge New API Demo", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            
            GUILayout.Space(10);
            GUILayout.Label($"Health: {health.Value:F1} / {maxHealth.Value:F1}");
            GUILayout.Label($"Mana: {mana.Value:F1}");
            GUILayout.Label($"Strength: {strength.Value:F1}");
            GUILayout.Label($"Level: {level.Value:F0}");
            GUILayout.Label($"Damage: {damage.Value:F1}");
            GUILayout.Label($"Crit Chance: {(critChance.Value * 100):F1}%");
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label("Space: +10 Health");
            GUILayout.Label("L: Level Up");
            GUILayout.Label("S: +2 Strength");
            GUILayout.Label("H: Full Heal");
            
            GUILayout.EndArea();
        }
    }
}