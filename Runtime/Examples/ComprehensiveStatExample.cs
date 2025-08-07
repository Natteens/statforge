using UnityEngine;
using System.Collections;

namespace StatForge.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating all StatForge features including:
    /// - Simple Stat usage with StatDefinitions
    /// - Event system integration
    /// - Modifier system with durations
    /// - Extension methods usage
    /// - Performance considerations
    /// </summary>
    public class ComprehensiveStatExample : MonoBehaviour
    {
        [Header("Basic Stats (Using StatDefinition)")]
        [SerializeField] private Stat health;
        [SerializeField] private Stat maxHealth;
        [SerializeField] private Stat mana;
        [SerializeField] private Stat maxMana;
        
        [Header("Combat Stats")]
        [SerializeField] private Stat attack;
        [SerializeField] private Stat defense;
        [SerializeField] private Stat speed;
        
        [Header("Derived Stats (Calculated)")]
        [SerializeField] private Stat criticalChance;
        [SerializeField] private Stat damageReduction;
        
        [Header("Runtime Information")]
        [SerializeField] private float healthPercentage;
        [SerializeField] private float manaPercentage;
        [SerializeField] private int activeModifiers;
        
        private void Start()
        {
            InitializeStats();
            SubscribeToEvents();
            DemonstrateBasicUsage();
            StartCoroutine(DemonstrateTimedEffects());
        }
        
        private void InitializeStats()
        {
            // Initialize current stats to max values
            if (health.IsValid && maxHealth.IsValid)
            {
                health.Value = maxHealth.Value;
            }
            
            if (mana.IsValid && maxMana.IsValid)
            {
                mana.Value = maxMana.Value;
            }
            
            Debug.Log("=== Initial Stats ===");
            ShowAllStats();
        }
        
        private void SubscribeToEvents()
        {
            // Local event subscriptions
            if (health.IsValid)
            {
                health.Events.OnValueChanged += OnHealthChanged;
                health.Events.OnModifierAdded += modifier => Debug.Log($"Health modifier added: {modifier}");
            }
            
            if (attack.IsValid)
            {
                attack.Events.OnValueChanged += OnAttackChanged;
            }
            
            // Global event subscription
            StatEvents.Global.OnAnyStatChanged += OnAnyStatChanged;
            StatEvents.Global.OnModifierAdded += OnGlobalModifierAdded;
        }
        
        private void Update()
        {
            // Update display values
            UpdateDisplayValues();
            
            // Count active modifiers
            activeModifiers = GetTotalActiveModifiers();
        }
        
        private void UpdateDisplayValues()
        {
            healthPercentage = health.IsValid ? health.GetPercentage() : 0f;
            manaPercentage = mana.IsValid ? mana.GetPercentage() : 0f;
        }
        
        private int GetTotalActiveModifiers()
        {
            int count = 0;
            var allStats = this.GetAllStats(); // Using extension method
            foreach (var stat in allStats)
            {
                count += stat.GetActiveModifiers().Count;
            }
            return count;
        }
        
        private void DemonstrateBasicUsage()
        {
            Debug.Log("=== Demonstrating Basic Usage ===");
            
            // Basic value manipulation
            health.Value -= 25f;
            Debug.Log($"Took 25 damage. Health: {health.Value}/{maxHealth.Value}");
            
            // Using extension methods
            if (health.IsMaxValue())
            {
                Debug.Log("Health is at maximum!");
            }
            else
            {
                Debug.Log($"Need {health.GetRemainingToMax():F1} health to reach maximum");
            }
            
            // Percentage manipulation
            mana.SetPercentage(0.5f); // Set to 50%
            Debug.Log($"Set mana to 50%: {mana.Value}/{maxMana.Value}");
        }
        
        private IEnumerator DemonstrateTimedEffects()
        {
            yield return new WaitForSeconds(2f);
            
            Debug.Log("=== Demonstrating Modifiers ===");
            
            // Add a strength boost for 10 seconds
            var strengthBoost = new StatModifier(20f, StatModifier.ModifierType.Flat, 10f, "Strength Potion");
            attack.AddModifier(strengthBoost);
            Debug.Log($"Added strength boost. Attack: {attack.Value} (+{strengthBoost.Value} for {strengthBoost.Duration}s)");
            
            yield return new WaitForSeconds(3f);
            
            // Add a percentage defense boost
            var defenseBoost = new StatModifier(25f, StatModifier.ModifierType.Percentage, 8f, "Defense Shield");
            defense.AddModifier(defenseBoost);
            Debug.Log($"Added defense percentage boost. Defense: {defense.Value} (+{defenseBoost.Value}% for {defenseBoost.Duration}s)");
            
            yield return new WaitForSeconds(2f);
            
            // Add temporary speed boost without modifier system
            speed.AddTemporaryBonus(15f, 5f);
            Debug.Log($"Added temporary speed boost. Speed: {speed.Value}");
            
            yield return new WaitForSeconds(5f);
            
            // Demonstrate healing
            Debug.Log("=== Demonstrating Healing ===");
            health.FillToMax(); // Using extension method
            Debug.Log($"Fully healed! Health: {health.Value}");
            
            yield return new WaitForSeconds(5f);
            
            // Show final stats
            Debug.Log("=== Final Stats ===");
            ShowAllStats();
        }
        
        private void OnHealthChanged(float oldValue, float newValue)
        {
            var difference = newValue - oldValue;
            var changeType = difference > 0 ? "gained" : "lost";
            Debug.Log($"Health {changeType}: {Mathf.Abs(difference):F1} (was {oldValue:F1}, now {newValue:F1})");
            
            // Check for critical health
            if (health.GetPercentage() <= 0.2f)
            {
                Debug.LogWarning("⚠️ Critical health! Health below 20%");
            }
        }
        
        private void OnAttackChanged(float oldValue, float newValue)
        {
            Debug.Log($"Attack power changed: {oldValue:F1} -> {newValue:F1}");
        }
        
        private void OnAnyStatChanged(Stat stat)
        {
            // This gets called for any stat change globally
            // Useful for UI updates, achievement tracking, etc.
        }
        
        private void OnGlobalModifierAdded(Stat stat, StatModifier modifier)
        {
            Debug.Log($"Global: Modifier '{modifier.Source}' added to {stat.Name}");
        }
        
        #region Context Menu Methods (For Testing)
        
        [ContextMenu("Take Damage")]
        public void TakeDamage()
        {
            TakeDamage(Random.Range(10f, 30f));
        }
        
        public void TakeDamage(float damage)
        {
            if (health.IsValid)
            {
                health.Value -= damage;
            }
        }
        
        [ContextMenu("Heal")]
        public void Heal()
        {
            Heal(Random.Range(15f, 35f));
        }
        
        public void Heal(float amount)
        {
            if (health.IsValid)
            {
                health.Value += amount;
            }
        }
        
        [ContextMenu("Use Mana")]
        public void UseMana()
        {
            UseMana(Random.Range(20f, 40f));
        }
        
        public void UseMana(float amount)
        {
            if (mana.IsValid)
            {
                mana.Value -= amount;
            }
        }
        
        [ContextMenu("Restore Mana")]
        public void RestoreMana()
        {
            if (mana.IsValid)
            {
                mana.Value += Random.Range(25f, 50f);
            }
        }
        
        [ContextMenu("Add Random Buff")]
        public void AddRandomBuff()
        {
            var allStats = this.GetAllStats();
            if (allStats.Count > 0)
            {
                var randomStat = allStats[Random.Range(0, allStats.Count)];
                var modifier = new StatModifier(
                    Random.Range(5f, 20f), 
                    (StatModifier.ModifierType)Random.Range(0, 2), // Flat or Percentage
                    Random.Range(5f, 15f), 
                    "Random Buff"
                );
                randomStat.AddModifier(modifier);
                Debug.Log($"Added random buff to {randomStat.Name}: {modifier}");
            }
        }
        
        [ContextMenu("Clear All Modifiers")]
        public void ClearAllModifiers()
        {
            var allStats = this.GetAllStats();
            allStats.ClearAllModifiers(); // Using extension method
            Debug.Log("Cleared all modifiers from all stats");
        }
        
        [ContextMenu("Show All Stats")]
        public void ShowAllStats()
        {
            Debug.Log("=== Current Stats ===");
            var allStats = this.GetAllStats();
            foreach (var stat in allStats)
            {
                var modifierCount = stat.GetActiveModifiers().Count;
                var modifierText = modifierCount > 0 ? $" ({modifierCount} modifiers)" : "";
                Debug.Log($"{stat.Name}: {stat.Value:F1}{modifierText}");
            }
        }
        
        [ContextMenu("Show Stats by Category")]
        public void ShowStatsByCategory()
        {
            var allStats = this.GetAllStats();
            
            Debug.Log("=== Primary Stats ===");
            var primaryStats = allStats.FilterByCategory(StatCategory.Primary);
            foreach (var stat in primaryStats)
            {
                Debug.Log($"  {stat.Name}: {stat.Value:F1}");
            }
            
            Debug.Log("=== Derived Stats ===");
            var derivedStats = allStats.FilterByCategory(StatCategory.Derived);
            foreach (var stat in derivedStats)
            {
                Debug.Log($"  {stat.Name}: {stat.Value:F1}");
            }
        }
        
        [ContextMenu("Reset All Stats")]
        public void ResetAllStats()
        {
            var allStats = this.GetAllStats();
            foreach (var stat in allStats)
            {
                stat.ResetToDefault(); // Using extension method
                stat.ClearAllModifiers();
            }
            Debug.Log("Reset all stats to default values");
        }
        
        [ContextMenu("Performance Test")]
        public void PerformanceTest()
        {
            StartCoroutine(RunPerformanceTest());
        }
        
        private IEnumerator RunPerformanceTest()
        {
            Debug.Log("Starting performance test...");
            var startTime = Time.realtimeSinceStartup;
            
            // Create many temporary modifiers
            for (int i = 0; i < 1000; i++)
            {
                if (attack.IsValid)
                {
                    var modifier = new StatModifier(1f, StatModifier.ModifierType.Flat, 0.1f, $"Test_{i}");
                    attack.AddModifier(modifier);
                }
                
                if (i % 100 == 0)
                {
                    yield return null; // Spread across frames
                }
            }
            
            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"Performance test completed in {(endTime - startTime) * 1000:F2}ms");
            Debug.Log($"Active modifiers on attack: {attack.GetActiveModifiers().Count}");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (health.IsValid)
            {
                health.Events.OnValueChanged -= OnHealthChanged;
            }
            
            if (attack.IsValid)
            {
                attack.Events.OnValueChanged -= OnAttackChanged;
            }
            
            StatEvents.Global.OnAnyStatChanged -= OnAnyStatChanged;
            StatEvents.Global.OnModifierAdded -= OnGlobalModifierAdded;
        }
    }
}