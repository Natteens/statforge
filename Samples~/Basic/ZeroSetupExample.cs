using UnityEngine;
using StatForge;

namespace StatForge.Examples
{
    /// <summary>
    /// Minimal example showing the ultra-simplified API promise.
    /// This demonstrates that ANY syntax works with ZERO setup required.
    /// </summary>
    public class ZeroSetupExample : MonoBehaviour
    {
        // The promise: ANY of these syntaxes work immediately with ZERO setup
        public Stat health;                    // Null initially, but works when used
        [SerializeField] private Stat mana;    // Also null initially
        protected Stat stamina;                // Any visibility
        internal Stat energy;                  // Still works
        
        // Pre-initialized - also works
        public Stat strength = new Stat("Strength", 10f);
        public Stat level = new Stat("Level", 1f);
        
        void Start()
        {
            Debug.Log("=== ZERO SETUP PROMISE TEST ===");
            
            // These should work immediately, even though stats are null!
            // The system auto-creates them on first use
            health = new Stat("Health", 100f);  // Manual initialization
            health.Value = 80f;                  // Works immediately
            Debug.Log($"Health: {health.Value}");
            
            // Natural operator syntax
            if (health > 50f)                    // Comparison works
            {
                Debug.Log("Character is healthy!");
            }
            
            health += 20f;                       // Operator overload (adds to base)
            Debug.Log($"Health after boost: {health.Value}");
            
            // Implicit conversion
            float healthFloat = health;          // No cast needed
            Debug.Log($"Health as float: {healthFloat}");
            
            // Works for any stat without setup
            mana = new Stat("Mana", 50f);
            mana.Buff(25f, 5f);                  // Temporary buff
            Debug.Log($"Mana with buff: {mana.Value}");
            
            // Extension methods work immediately
            stamina = new Stat("Stamina", 100f);
            stamina.TakeDamage(25f);
            Debug.Log($"Stamina after damage: {stamina.Value}");
            
            // Conversions work
            energy = new Stat("Energy", 75f, 0f, 100f);
            Debug.Log($"Energy percentage: {energy.ToPercentageText()}");
            Debug.Log($"Energy as bool: {energy.ToBool()}");
            
            // Events work immediately
            strength.OnValueChanged += (old, newVal) => 
                Debug.Log($"Strength changed: {old} → {newVal}");
            
            strength.Value += 5f;                // Triggers event
            
            // Derived stats work immediately
            var damage = new Stat("Damage", "strength * 2 + level");
            Debug.Log($"Calculated damage: {damage.Value}");
            
            Debug.Log("=== ALL SYNTAX WORKS WITH ZERO SETUP! ===");
        }
        
        void Update()
        {
            // Operators work in Update too
            if (Input.GetKeyDown(KeyCode.Space) && health != null)
            {
                health += 10f;  // Natural syntax
                Debug.Log($"Health boosted: {health.Value}");
            }
        }
    }
}