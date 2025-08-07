using NUnit.Framework;
using UnityEngine;
using StatForge;

namespace StatForge.Tests
{
    /// <summary>
    /// Integration tests for the complete new StatForge API.
    /// Tests end-to-end functionality including formulas, modifiers, and events.
    /// </summary>
    public class StatForgeIntegrationTests
    {
        private GameObject testObject;
        private IntegrationTestComponent component;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("IntegrationTest");
            component = testObject.AddComponent<IntegrationTestComponent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void CompleteWorkflow_NewAPI_WorksEndToEnd()
        {
            // Initialize the stats system
            testObject.InitializeStats();
            
            // Verify initial values
            Assert.AreEqual(100f, component.health.Value, 0.01f);
            Assert.AreEqual(10f, component.strength.Value, 0.01f);
            Assert.AreEqual(1f, component.level.Value, 0.01f);
            
            // Test derived stat calculation
            // damage = health * 0.1 + strength * 2 = 100 * 0.1 + 10 * 2 = 10 + 20 = 30
            Assert.AreEqual(30f, component.damage.Value, 0.1f);
            
            // Test level-based derived stat
            // maxHealth = level * 10 + 50 = 1 * 10 + 50 = 60
            Assert.AreEqual(60f, component.maxHealth.Value, 0.01f);
        }
        
        [Test]
        public void FormulaUpdates_WhenDependentStatsChange()
        {
            testObject.InitializeStats();
            
            // Change strength and verify damage updates
            component.strength.Value = 20f;
            
            // damage = health * 0.1 + strength * 2 = 100 * 0.1 + 20 * 2 = 10 + 40 = 50
            Assert.AreEqual(50f, component.damage.Value, 0.1f);
            
            // Change level and verify maxHealth updates
            component.level.Value = 5f;
            
            // maxHealth = level * 10 + 50 = 5 * 10 + 50 = 100
            Assert.AreEqual(100f, component.maxHealth.Value, 0.01f);
        }
        
        [Test]
        public void Events_FireCorrectly_ForStatChanges()
        {
            testObject.InitializeStats();
            
            bool healthEventFired = false;
            float oldHealthValue = 0f;
            float newHealthValue = 0f;
            
            bool damageEventFired = false;
            
            // Subscribe to individual stat event
            component.health.OnValueChanged += (oldVal, newVal) =>
            {
                healthEventFired = true;
                oldHealthValue = oldVal;
                newHealthValue = newVal;
            };
            
            // Subscribe to derived stat event
            component.damage.OnValueChanged += (oldVal, newVal) =>
            {
                damageEventFired = true;
            };
            
            // Change health value
            component.health.Value = 150f;
            
            // Verify events fired
            Assert.IsTrue(healthEventFired);
            Assert.AreEqual(100f, oldHealthValue, 0.01f);
            Assert.AreEqual(150f, newHealthValue, 0.01f);
            Assert.IsTrue(damageEventFired); // Should fire because damage depends on health
        }
        
        [Test]
        public void Modifiers_ApplyCorrectly_ToStatsAndFormulas()
        {
            testObject.InitializeStats();
            
            // Add a temporary health bonus
            var healthBonus = component.health.AddTemporaryBonus(50f);
            
            // Health should be 100 + 50 = 150
            Assert.AreEqual(150f, component.health.Value, 0.01f);
            
            // Damage should update because it depends on health
            // damage = health * 0.1 + strength * 2 = 150 * 0.1 + 10 * 2 = 15 + 20 = 35
            Assert.AreEqual(35f, component.damage.Value, 0.1f);
            
            // Remove the modifier
            component.health.RemoveModifier(healthBonus);
            
            // Values should return to normal
            Assert.AreEqual(100f, component.health.Value, 0.01f);
            Assert.AreEqual(30f, component.damage.Value, 0.1f);
        }
        
        [Test]
        public void StatExtensions_WorkWithNewStatObjects()
        {
            testObject.InitializeStats();
            
            // Test GetStatObject
            var healthStat = testObject.GetStatObject("Health");
            Assert.IsNotNull(healthStat);
            Assert.AreEqual("Health", healthStat.Name);
            Assert.AreEqual(100f, healthStat.Value, 0.01f);
            
            // Test GetAllStatObjects
            var allStats = System.Linq.Enumerable.ToList(testObject.GetAllStatObjects());
            Assert.IsTrue(allStats.Count >= 4); // health, strength, level, damage, maxHealth
            
            // Verify specific stats are present
            Assert.IsTrue(allStats.Any(s => s.Name == "Health"));
            Assert.IsTrue(allStats.Any(s => s.Name == "Damage" && s.IsDerived));
        }
        
        [Test]
        public void MinMaxValues_AreRespected()
        {
            testObject.InitializeStats();
            
            // Set health min/max values
            component.health.MinValue = 0f;
            component.health.MaxValue = 200f;
            
            // Try to set below minimum
            component.health.Value = -50f;
            Assert.AreEqual(0f, component.health.Value, 0.01f);
            
            // Try to set above maximum
            component.health.Value = 300f;
            Assert.AreEqual(200f, component.health.Value, 0.01f);
        }
        
        [Test]
        public void ImplicitConversion_Works()
        {
            testObject.InitializeStats();
            
            // Test implicit conversion to float
            float healthValue = component.health;
            Assert.AreEqual(100f, healthValue, 0.01f);
            
            // Test in mathematical operations
            float halfHealth = component.health / 2f;
            Assert.AreEqual(50f, halfHealth, 0.01f);
            
            // Test in comparisons
            Assert.IsTrue(component.health > 50f);
            Assert.IsTrue(component.health <= 100f);
        }
        
        [Test]
        public void BackwardCompatibility_Works()
        {
            testObject.InitializeStats();
            
            // Test legacy API still works
            testObject.SetStat("testStat", 42f);
            float value = testObject.GetStat("testStat");
            Assert.AreEqual(42f, value, 0.01f);
            
            // Test modifier through legacy API
            var modifier = StatModifier.Additive(8f);
            testObject.AddStatModifier("testStat", modifier);
            Assert.AreEqual(50f, testObject.GetStat("testStat"), 0.01f);
            
            testObject.RemoveStatModifier("testStat", modifier);
            Assert.AreEqual(42f, testObject.GetStat("testStat"), 0.01f);
        }
    }
    
    /// <summary>
    /// Test component using the new Stat API.
    /// </summary>
    public class IntegrationTestComponent : MonoBehaviour
    {
        [Header("Primary Stats")]
        [Stat] public Stat health = new Stat("Health", 100f);
        [Stat] public Stat strength = new Stat("Strength", 10f);
        [Stat] public Stat level = new Stat("Level", 1f);
        
        [Header("Derived Stats")]
        [Stat] public Stat damage = new Stat("Damage", "health * 0.1 + strength * 2");
        [Stat] public Stat maxHealth = new Stat("MaxHealth", "level * 10 + 50");
    }
}