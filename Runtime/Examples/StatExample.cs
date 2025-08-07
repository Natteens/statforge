using UnityEngine;

namespace StatForge.Examples
{
    public class StatExample : MonoBehaviour
    {
        [Header("Simple Stat Usage")]
        [SerializeField] private Stat maxHealth;
        [SerializeField] private Stat currentHealth;
        [SerializeField] private Stat strength;
        [SerializeField] private Stat defense;
        
        [Header("Runtime Info")]
        [SerializeField] private float healthPercentage;
        
        private void Start()
        {
            // Initialize current health to max health
            if (currentHealth.IsValid && maxHealth.IsValid)
            {
                currentHealth.Value = maxHealth.Value;
            }
            
            // Subscribe to value changes
            if (currentHealth.IsValid)
            {
                currentHealth.OnValueChanged += OnHealthChanged;
            }
            
            // Example of modifier usage
            if (strength.IsValid)
            {
                strength.AddTemporaryBonus(10f, 5f); // +10 strength for 5 seconds
            }
        }
        
        private void Update()
        {
            // Update health percentage for display
            if (currentHealth.IsValid)
            {
                healthPercentage = currentHealth.GetPercentage();
            }
        }
        
        private void OnHealthChanged(Stat stat)
        {
            Debug.Log($"Health changed to: {stat.Value}");
        }
        
        [ContextMenu("Take Damage")]
        public void TakeDamage()
        {
            TakeDamage(10f);
        }
        
        public void TakeDamage(float damage)
        {
            if (currentHealth.IsValid)
            {
                currentHealth.Value -= damage;
                Debug.Log($"Took {damage} damage. Health: {currentHealth.Value}/{maxHealth.Value}");
            }
        }
        
        [ContextMenu("Heal")]
        public void Heal()
        {
            Heal(20f);
        }
        
        public void Heal(float amount)
        {
            if (currentHealth.IsValid)
            {
                currentHealth.Value += amount;
                Debug.Log($"Healed {amount}. Health: {currentHealth.Value}/{maxHealth.Value}");
            }
        }
        
        [ContextMenu("Add Strength Buff")]
        public void AddStrengthBuff()
        {
            if (strength.IsValid)
            {
                var modifier = new StatModifier(15f, StatModifier.ModifierType.Flat, 10f, "Strength Potion");
                strength.AddModifier(modifier);
                Debug.Log($"Added strength buff. New strength: {strength.Value}");
            }
        }
        
        [ContextMenu("Add Defense Percentage Buff")]
        public void AddDefensePercentageBuff()
        {
            if (defense.IsValid)
            {
                var modifier = new StatModifier(25f, StatModifier.ModifierType.Percentage, 8f, "Shield Spell");
                defense.AddModifier(modifier);
                Debug.Log($"Added defense percentage buff. New defense: {defense.Value}");
            }
        }
        
        [ContextMenu("Show All Stats")]
        public void ShowAllStats()
        {
            Debug.Log("=== Current Stats ===");
            if (maxHealth.IsValid) Debug.Log($"Max Health: {maxHealth.Value}");
            if (currentHealth.IsValid) Debug.Log($"Current Health: {currentHealth.Value} ({currentHealth.GetPercentage() * 100:F1}%)");
            if (strength.IsValid) Debug.Log($"Strength: {strength.Value} (Active modifiers: {strength.GetActiveModifiers().Count})");
            if (defense.IsValid) Debug.Log($"Defense: {defense.Value} (Active modifiers: {defense.GetActiveModifiers().Count})");
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (currentHealth.IsValid)
            {
                currentHealth.OnValueChanged -= OnHealthChanged;
            }
        }
    }
}