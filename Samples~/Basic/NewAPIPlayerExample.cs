using UnityEngine;
using StatForge;

namespace StatForge.Examples
{
    /// <summary>
    /// Example demonstrating the ultra-simplified StatForge v2 API.
    /// Shows how stats work completely transparently with zero setup required.
    /// </summary>
    public class NewAPIPlayerExample : MonoBehaviour
    {
        [Header("Ultra-Simplified Syntax - ANY of these work:")]
        public Stat health = new Stat("Health", 100f);                    // Auto-initializes
        [SerializeField] private Stat mana = new Stat("Mana", 50f);       // Private serialized 
        protected Stat stamina = new Stat("Stamina", 75f);                // Protected - also works
        internal Stat energy = new Stat("Energy", 25f);                   // Internal - still works
        
        [Header("Derived Stats with Formulas")]
        public Stat damage = new Stat("Damage", "health * 0.1 + strength * 2");
        public Stat maxHealth = new Stat("MaxHealth", "level * 10 + 50");
        public Stat critChance = new Stat("CritChance", "min(level * 0.02, 0.95)");
        
        [Header("More Stats for Testing")]
        public Stat strength = new Stat("Strength", 10f);
        public Stat level = new Stat("Level", 1f);
        
        void Start()
        {
            Debug.Log("=== StatForge v2: Ultra-Simplified API Demo ===");
            
            // Zero initialization needed - everything works immediately!
            health.Value = 80f;           // Works immediately
            mana += 25f;                  // Operator overloads (adds to base value)
            Debug.Log($"Total: {stamina}"); // ToString automático
            
            // Natural operator syntax
            Debug.Log($"Initial Health: {health.Value}");
            Debug.Log($"Health > 50? {health > 50f}");  // Comparison operators
            Debug.Log($"Health == 80? {health == 80f}"); // Equality
            
            // Ultra-natural modifier API
            health.Buff(25f, 5f);          // Buff for 5 seconds
            mana.Debuff(10f, 3f);          // Debuff for 3 seconds
            
            // Convenient methods
            stamina.Fill();                // Fill to max
            energy.Empty();                // Empty to min
            
            // Advanced operations
            Debug.Log($"Health percentage: {health.ToPercentageText()}");
            Debug.Log($"Can afford spell (20 mana)? {mana.CanAfford(20f)}");
            
            // Subscribe to events
            health.OnValueChanged += OnHealthChanged;
            
            // Level up affects derived stats automatically
            level.Value = 5f;
            Debug.Log($"After leveling to 5:");
            Debug.Log($"  Max Health: {maxHealth.Value}");
            Debug.Log($"  Crit Chance: {critChance.ToPercentageText()}");
            Debug.Log($"  Damage: {damage.Value}");
            
            // Works with any syntax the developer prefers!
            TestAllSyntaxes();
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Natural syntax - just works!
                health += 10f;  // Uses operator overload to add to base value
                Debug.Log($"Health boosted to: {health.Value}");
            }
            
            if (Input.GetKeyDown(KeyCode.L))
            {
                level.Value += 1f;
                Debug.Log($"Level up! New level: {level.Value}");
            }
            
            if (Input.GetKeyDown(KeyCode.H))
            {
                health.Heal(50f);  // Extension method
                Debug.Log($"Healed! Health: {health.FormatFull()}");
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (mana.Consume(20f))  // Try to consume mana
                {
                    Debug.Log("Spell cast! Mana consumed.");
                }
                else
                {
                    Debug.Log("Not enough mana!");
                }
            }
        }
        
        private void TestAllSyntaxes()
        {
            Debug.Log("\n=== Testing All Supported Syntaxes ===");
            
            // 1. Direct value assignment
            health.Value = 100f;
            Debug.Log($"Direct assignment: {health.Value}");
            
            // 2. Arithmetic operators (returns float)
            float result = health + 25f;
            Debug.Log($"health + 25f = {result}");
            
            // 3. Comparison operators
            bool isHealthy = health > 75f;
            Debug.Log($"health > 75f = {isHealthy}");
            
            // 4. Implicit conversion to float
            float healthFloat = health;  // No cast needed!
            Debug.Log($"Implicit float: {healthFloat}");
            
            // 5. Extension methods for natural usage
            health.TakeDamage(15f);
            Debug.Log($"After damage: {health.Value}");
            
            // 6. Modifier methods
            var bonus = health.AddTemp(50f, 2f);  // +50 for 2 seconds
            Debug.Log($"With temporary bonus: {health.Value}");
            
            // 7. Utility properties
            Debug.Log($"Health percentage: {health.Percentage:P0}");
            Debug.Log($"Is at max? {health.IsAtMax}");
            Debug.Log($"Is empty? {health.IsEmpty}");
            
            // 8. Conversion utilities
            Debug.Log($"As percentage text: {health.ToPercentageText()}");
            Debug.Log($"As fraction: {health.ToFractionText()}");
            Debug.Log($"As int: {health.ToInt()}");
            Debug.Log($"As bool: {health.ToBool()}");
            
            Debug.Log("=== All syntaxes work perfectly! ===\n");
        }
        
        private void OnHealthChanged(float oldValue, float newValue)
        {
            Debug.Log($"💓 Health changed: {oldValue:F1} → {newValue:F1}");
            
            // Update UI color based on health
            Color healthColor = health.ToColor();  // Auto red-to-green gradient
            Debug.Log($"Health color: {healthColor}");
        }
        
        [Header("Runtime Testing UI")]
        [SerializeField] private bool showDebugInfo = true;
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 350, 500));
            GUILayout.Label("StatForge v2: Ultra-Simplified API", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            
            GUILayout.Space(10);
            
            // Show stats with various display formats
            GUILayout.Label($"Health: {health.FormatFull()} ({health.ToPercentageText()})");
            GUILayout.Label($"Mana: {mana.FormatFull()} (Can afford 20? {mana.CanAfford(20f)})");
            GUILayout.Label($"Stamina: {stamina.Value:F1}");
            GUILayout.Label($"Energy: {energy.Value:F1}");
            GUILayout.Label($"Strength: {strength.Value:F0}");
            GUILayout.Label($"Level: {level.ToInt()}");
            
            GUILayout.Space(5);
            GUILayout.Label("Derived Stats:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"Max Health: {maxHealth.Value:F0}");
            GUILayout.Label($"Damage: {damage.Value:F1}");
            GUILayout.Label($"Crit Chance: {critChance.ToPercentageText()}");
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label("Space: +10 Health");
            GUILayout.Label("L: Level Up");
            GUILayout.Label("H: Heal 50");
            GUILayout.Label("M: Cast Spell (20 mana)");
            
            GUILayout.Space(10);
            GUILayout.Label("Stat States:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"Health is empty? {health.IsEmpty}");
            GUILayout.Label($"Health is at max? {health.IsAtMax}");
            GUILayout.Label($"Health modifiers: {health.Modifiers.Count}");
            
            GUILayout.EndArea();
        }
        
        void OnDestroy()
        {
            // Events are automatically cleaned up, but good practice
            if (health != null)
                health.OnValueChanged -= OnHealthChanged;
        }
    }
}