using UnityEngine;
using StatForge;
using StatForge.Core;

namespace StatForge.Samples
{
    /// <summary>
    /// Advanced example showing enterprise features: validation, batching, events, performance monitoring
    /// </summary>
    public class AdvancedPlayerExample : MonoBehaviour
    {
        [Header("Core Stats")]
        [Stat(DisplayName = "Strength", ShortName = "STR", MinValue = 1, MaxValue = 100)]
        public int Strength = 10;
        
        [Stat(DisplayName = "Health Points", ShortName = "HP", MinValue = 0, MaxValue = 1000)]
        public float Health = 100f;
        
        [Stat(DisplayName = "Level", MinValue = 1, MaxValue = 99)]
        public int Level = 1;
        
        [Stat(DisplayName = "Experience", MinValue = 0, MaxValue = 999999)]
        public int Experience = 0;
        
        [Header("Derived Stats")]
        [Stat(DisplayName = "Max Health", Formula = "STR * 10 + Level * 5", Category = StatCategory.Derived)]
        public float MaxHealth = 0f;
        
        [Stat(DisplayName = "Attack Power", Formula = "STR * 2 + Level", Category = StatCategory.Derived)]
        public int AttackPower = 0;
        
        private StatForgeComponent statForge;
        private float lastHealthUpdate;
        
        void Start()
        {
            statForge = GetComponent<StatForgeComponent>();
            if (statForge == null)
            {
                statForge = gameObject.AddComponent<StatForgeComponent>();
            }
            
            SetupAdvancedFeatures();
        }
        
        private void SetupAdvancedFeatures()
        {
            // 1. Add validation rules
            statForge.AddValidationRule<float>("Health", health => health >= 0 && health <= MaxHealth);
            statForge.AddValidationRule<int>("Experience", exp => exp >= 0);
            
            // 2. Subscribe to events through the modern event bus
            statForge.SubscribeToEvents<AttributeChangedEvent>(OnAttributeChanged);
            statForge.SubscribeToEvents<ModifierAddedEvent>(OnModifierAdded);
            
            // 3. Subscribe to specific attribute changes (legacy style still works)
            statForge.OnAttributeChanged("Health", (oldVal, newVal) => CheckHealthStatus(oldVal, newVal));
            statForge.OnAttributeChanged("Level", (oldVal, newVal) => OnLevelUp(oldVal, newVal));
            
            Debug.Log("Advanced StatForge features initialized!");
        }
        
        void Update()
        {
            // Natural syntax still works
            if (Time.time - lastHealthUpdate > 1f)
            {
                Health -= 1f; // Lose 1 HP per second
                lastHealthUpdate = Time.time;
            }
            
            HandleInput();
            
            // Performance monitoring
            if (Input.GetKeyDown(KeyCode.P))
            {
                ShowPerformanceStats();
            }
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Level up with validation
                Level++;
            }
            
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Heal to max (clamped by validation)
                Health = MaxHealth;
            }
            
            if (Input.GetKeyDown(KeyCode.B))
            {
                // Apply strength buff
                statForge.AddModifier("Strength", 5, 10f);
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Add experience (demonstrates batch operations)
                AddExperienceBatch(100);
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                // Demonstrate multiple stat changes in a batch
                ApplyMagicPotion();
            }
        }
        
        private void AddExperienceBatch(int amount)
        {
            using (var batch = statForge.CreateBatch())
            {
                batch.AddAction(() => Experience += amount);
                batch.AddAction(() => Debug.Log($"Experience gained: {amount}"));
                
                // Check for level up
                if (Experience >= GetExperienceForNextLevel())
                {
                    batch.AddAction(() => {
                        Level++;
                        Experience = 0;
                    });
                    
                    batch.AddEvent(new AttributeChangedEvent
                    {
                        AttributeName = "Level",
                        OldValue = Level - 1,
                        NewValue = Level,
                        Source = this
                    });
                }
                
                // Batch automatically commits on dispose
            }
        }
        
        private void ApplyMagicPotion()
        {
            using (var batch = statForge.CreateBatch())
            {
                batch.AddAction(() => Health = MaxHealth);
                batch.AddAction(() => statForge.AddModifier("Strength", 3, 15f));
                batch.AddAction(() => Debug.Log("Magic potion consumed!"));
                
                batch.AddEvent(new ModifierAddedEvent
                {
                    AttributeName = "Strength",
                    ModifierValue = 3,
                    Duration = 15f,
                    Source = this
                });
            }
        }
        
        private int GetExperienceForNextLevel()
        {
            return Level * 100; // Simple progression formula
        }
        
        private void OnAttributeChanged(AttributeChangedEvent eventData)
        {
            if (eventData.Source == statForge)
            {
                Debug.Log($"[Event Bus] {eventData.AttributeName}: {eventData.OldValue} → {eventData.NewValue}");
            }
        }
        
        private void OnModifierAdded(ModifierAddedEvent eventData)
        {
            if (eventData.Source == this)
            {
                Debug.Log($"[Event Bus] Modifier added to {eventData.AttributeName}: +{eventData.ModifierValue} for {eventData.Duration}s");
            }
        }
        
        private void CheckHealthStatus(object oldVal, object newVal)
        {
            if (newVal is float health)
            {
                if (health <= 0)
                {
                    Debug.Log("Player died!");
                    OnPlayerDeath();
                }
                else if (health < 20f)
                {
                    Debug.Log("Player health critical!");
                }
            }
        }
        
        private void OnLevelUp(object oldVal, object newVal)
        {
            if (oldVal is int oldLevel && newVal is int newLevel && newLevel > oldLevel)
            {
                Debug.Log($"LEVEL UP! {oldLevel} → {newLevel}");
                
                // Restore health on level up
                Health = MaxHealth;
                
                // Add permanent stat bonus
                statForge.AddModifier("Strength", 1, 0f); // Permanent modifier (duration = 0)
            }
        }
        
        private void OnPlayerDeath()
        {
            // Reset stats
            Health = MaxHealth;
            Level = Mathf.Max(1, Level - 1);
            Experience = 0;
            
            Debug.Log("Player respawned with reduced level!");
        }
        
        private void ShowPerformanceStats()
        {
            var stats = statForge.GetCacheStats();
            Debug.Log($"Cache Stats: {stats}");
            
            // Show all stats using query system
            var allStats = statForge.Query().Select();
            Debug.Log($"Total Attributes: {allStats.Count()}");
            
            // Show combat-related stats
            var combatPower = statForge.Query()
                .Where(name => name.Contains("Strength") || name.Contains("Attack"))
                .Sum<int>();
            Debug.Log($"Total Combat Power: {combatPower}");
        }
        
        private void OnDestroy()
        {
            // Cleanup subscriptions
            if (statForge != null)
            {
                statForge.UnsubscribeFromEvents<AttributeChangedEvent>(OnAttributeChanged);
                statForge.UnsubscribeFromEvents<ModifierAddedEvent>(OnModifierAdded);
            }
        }
        
        // Example of custom validation that can be added dynamically
        private bool ValidateHealthValue(float health)
        {
            // Custom business logic
            return health <= MaxHealth && health >= 0;
        }
    }
}