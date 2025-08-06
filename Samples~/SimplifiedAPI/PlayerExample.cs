using UnityEngine;
using StatForge;

namespace StatForge.Samples
{
    /// <summary>
    /// Example showing the ultra-simplified API in action
    /// Just add [Stat] to fields and everything works naturally!
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("Core Stats")]
        [Stat(DisplayName = "Strength", ShortName = "STR", MinValue = 1, MaxValue = 100)]
        public int Strength = 10;
        
        [Stat(DisplayName = "Health Points", ShortName = "HP", MinValue = 0, MaxValue = 1000)]
        public float Health = 100f;
        
        [Stat(DisplayName = "Level", MinValue = 1, MaxValue = 99)]
        public int Level = 1;
        
        [Stat(DisplayName = "Mana", MinValue = 0, MaxValue = 500)]
        public float Mana = 50f;
        
        [Header("Derived Stats (Optional - can be calculated in code)")]
        [Stat(DisplayName = "Max Health", Formula = "STR * 10 + Level * 5", Category = StatCategory.Derived)]
        public float MaxHealth = 0f; // This will be auto-calculated
        
        [Stat(DisplayName = "Attack Power", Formula = "STR * 2 + Level", Category = StatCategory.Derived)]
        public int AttackPower = 0; // This will be auto-calculated
        
        private StatForgeComponent statForge;
        
        void Start()
        {
            // Get the StatForge component (added automatically or manually)
            statForge = GetComponent<StatForgeComponent>();
            if (statForge == null)
            {
                statForge = gameObject.AddComponent<StatForgeComponent>();
            }
            
            // Subscribe to stat changes
            statForge.OnAttributeChanged("Health", (oldVal, newVal) => 
            {
                Debug.Log($"Health changed from {oldVal} to {newVal}");
                CheckIfDead();
            });
        }
        
        void Update()
        {
            // Natural syntax - works exactly like regular variables!
            Health -= Time.deltaTime * 2f; // Lose health over time
            
            if (Input.GetKeyDown(KeyCode.Space)) 
            {
                Level++; // Level up!
                Debug.Log($"Level up! Now level {Level}");
            }
            
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Heal to max (using derived stat)
                Health = MaxHealth;
                Debug.Log($"Healed to max: {Health}");
            }
            
            if (Input.GetKeyDown(KeyCode.B))
            {
                // Add temporary strength buff
                statForge.AddModifier("Strength", 5, 10f); // +5 STR for 10 seconds
                Debug.Log("Strength buff applied!");
            }
        }
        
        private void CheckIfDead()
        {
            if (Health <= 0)
            {
                Debug.Log("Player died!");
                Health = 0; // Clamp to minimum
            }
        }
        
        // Example of query system usage
        void ShowAllStats()
        {
            var allStatNames = statForge.Query().Select();
            foreach (var name in allStatNames)
            {
                Debug.Log($"{name}: {statForge.Get<float>(name)}");
            }
        }
        
        // Example of getting total combat stats
        int GetTotalCombatPower()
        {
            return statForge.Query()
                .Where(name => name.Contains("Attack") || name.Contains("Strength"))
                .Sum<int>();
        }
    }
}