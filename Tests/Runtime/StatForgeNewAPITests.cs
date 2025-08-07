using NUnit.Framework;
using UnityEngine;
using StatForge;

namespace StatForge.Tests
{
    public class StatForgeNewAPITests
    {
        private GameObject testObject;
        private TestComponent testComponent;
        private NewAPITestComponent newAPITestComponent;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestObject");
            testComponent = testObject.AddComponent<TestComponent>();
            newAPITestComponent = testObject.AddComponent<NewAPITestComponent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        #region Legacy API Tests
        
        [Test]
        public void StatAttribute_CanBeAppliedToFields()
        {
            // Test that [Stat] attribute can be applied to fields
            var field = typeof(TestComponent).GetField("health");
            var statAttr = System.Attribute.GetCustomAttribute(field, typeof(StatAttribute)) as StatAttribute;
            
            Assert.IsNotNull(statAttr);
        }
        
        [Test]
        public void StatExtensions_GetSetStat_WorksCorrectly()
        {
            // Test basic get/set functionality
            testObject.SetStat("testStat", 100f);
            float value = testObject.GetStat("testStat");
            
            Assert.AreEqual(100f, value, 0.01f);
        }
        
        [Test]
        public void StatExtensions_InitializeStats_CreatesStatCollection()
        {
            // Test that InitializeStats works
            testObject.InitializeStats();
            var collection = testObject.GetStatCollection();
            
            Assert.IsNotNull(collection);
        }
        
        [Test]
        public void StatModifier_AddRemove_WorksCorrectly()
        {
            // Test modifier functionality
            testObject.SetStat("testStat", 100f);
            
            var modifier = StatModifier.Additive(25f);
            testObject.AddStatModifier("testStat", modifier);
            
            float modifiedValue = testObject.GetStat("testStat");
            Assert.AreEqual(125f, modifiedValue, 0.01f);
            
            testObject.RemoveStatModifier("testStat", modifier);
            float afterRemoval = testObject.GetStat("testStat");
            Assert.AreEqual(100f, afterRemoval, 0.01f);
        }
        
        [Test]
        public void StatEvents_Fire_WhenStatsChange()
        {
            // Test that events fire correctly
            bool eventFired = false;
            string eventStatName = "";
            float eventOldValue = 0f;
            float eventNewValue = 0f;
            
            StatEvents.OnStatChanged += (owner, statName, oldValue, newValue) =>
            {
                if (owner == testObject)
                {
                    eventFired = true;
                    eventStatName = statName;
                    eventOldValue = oldValue;
                    eventNewValue = newValue;
                }
            };
            
            testObject.SetStat("testStat", 50f);
            testObject.SetStat("testStat", 75f);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual("testStat", eventStatName);
            Assert.AreEqual(50f, eventOldValue, 0.01f);
            Assert.AreEqual(75f, eventNewValue, 0.01f);
        }
        
        [Test]
        public void StatCollection_IndependentUsage_Works()
        {
            // Test StatCollection can work independently
            var collection = new StatCollection();
            collection.Initialize();
            
            collection.Set("strength", 10f);
            collection.Set("health", 100f);
            
            Assert.AreEqual(10f, collection.Get("strength"), 0.01f);
            Assert.AreEqual(100f, collection.Get("health"), 0.01f);
        }
        
        [Test]
        public void ModifierTypes_ApplyCorrectly()
        {
            // Test different modifier types
            testObject.SetStat("testStat", 100f);
            
            // Additive
            var additiveModifier = StatModifier.Additive(25f);
            testObject.AddStatModifier("testStat", additiveModifier);
            Assert.AreEqual(125f, testObject.GetStat("testStat"), 0.01f);
            
            // Multiplicative  
            var multiplicativeModifier = StatModifier.Multiplicative(2f);
            testObject.AddStatModifier("testStat", multiplicativeModifier);
            Assert.AreEqual(250f, testObject.GetStat("testStat"), 0.01f); // (100 + 25) * 2
            
            testObject.RemoveStatModifier("testStat", additiveModifier);
            testObject.RemoveStatModifier("testStat", multiplicativeModifier);
            
            // Override
            var overrideModifier = StatModifier.Override(500f);
            testObject.AddStatModifier("testStat", overrideModifier);
            Assert.AreEqual(500f, testObject.GetStat("testStat"), 0.01f);
        }
        
        #endregion
        
        #region New Stat Class Tests
        
        [Test]
        public void Stat_Constructor_WithValue_Works()
        {
            var stat = new Stat("Health", 100f);
            
            Assert.AreEqual("Health", stat.Name);
            Assert.AreEqual(100f, stat.Value, 0.01f);
            Assert.AreEqual(100f, stat.BaseValue, 0.01f);
            Assert.IsFalse(stat.IsDerived);
        }
        
        [Test]
        public void Stat_Constructor_WithFormula_Works()
        {
            var stat = new Stat("Damage", "10 + 5");
            
            Assert.AreEqual("Damage", stat.Name);
            Assert.AreEqual("10 + 5", stat.Formula);
            Assert.IsTrue(stat.IsDerived);
            Assert.AreEqual(15f, stat.Value, 0.01f); // 0 (base) + 15 (formula)
        }
        
        [Test]
        public void Stat_SetValue_TriggersEvents()
        {
            var stat = new Stat("Test", 50f);
            bool eventFired = false;
            float oldValue = 0f;
            float newValue = 0f;
            
            stat.OnValueChanged += (old, newVal) =>
            {
                eventFired = true;
                oldValue = old;
                newValue = newVal;
            };
            
            stat.Value = 75f;
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(50f, oldValue, 0.01f);
            Assert.AreEqual(75f, newValue, 0.01f);
        }
        
        [Test]
        public void Stat_AddModifier_Works()
        {
            var stat = new Stat("Test", 100f);
            var modifier = StatModifier.Additive(25f);
            
            stat.AddModifier(modifier);
            
            Assert.AreEqual(125f, stat.Value, 0.01f);
            Assert.AreEqual(1, stat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_RemoveModifier_Works()
        {
            var stat = new Stat("Test", 100f);
            var modifier = StatModifier.Additive(25f);
            
            stat.AddModifier(modifier);
            Assert.AreEqual(125f, stat.Value, 0.01f);
            
            bool removed = stat.RemoveModifier(modifier);
            
            Assert.IsTrue(removed);
            Assert.AreEqual(100f, stat.Value, 0.01f);
            Assert.AreEqual(0, stat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_MinMaxValues_Work()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            
            Assert.AreEqual(0f, stat.MinValue);
            Assert.AreEqual(100f, stat.MaxValue);
            
            stat.Value = 150f; // Should be clamped to max
            Assert.AreEqual(100f, stat.Value, 0.01f);
            
            stat.Value = -10f; // Should be clamped to min
            Assert.AreEqual(0f, stat.Value, 0.01f);
        }
        
        [Test]
        public void Stat_ImplicitConversion_Works()
        {
            var stat = new Stat("Test", 42f);
            
            float value = stat; // Implicit conversion
            
            Assert.AreEqual(42f, value, 0.01f);
        }
        
        [Test]
        public void Stat_TemporaryModifiers_Work()
        {
            var stat = new Stat("Test", 100f);
            
            var bonus = stat.AddTemporaryBonus(25f, 1f); // +25 for 1 second
            Assert.AreEqual(125f, stat.Value, 0.01f);
            
            var multiplier = stat.AddTemporaryMultiplier(2f, 1f); // x2 for 1 second
            Assert.AreEqual(250f, stat.Value, 0.01f); // (100 + 25) * 2
            
            Assert.AreEqual(2, stat.Modifiers.Count);
        }
        
        [Test]
        public void NewAPITestComponent_StatObjects_Work()
        {
            testObject.InitializeStats();
            
            // Test that Stat objects are properly initialized
            Assert.IsNotNull(newAPITestComponent.health);
            Assert.AreEqual("Health", newAPITestComponent.health.Name);
            Assert.AreEqual(100f, newAPITestComponent.health.Value, 0.01f);
            
            Assert.IsNotNull(newAPITestComponent.damage);
            Assert.IsTrue(newAPITestComponent.damage.IsDerived);
            
            // Test setting values
            newAPITestComponent.health.Value = 150f;
            Assert.AreEqual(150f, newAPITestComponent.health.Value, 0.01f);
            
            // Test that derived stats work
            newAPITestComponent.strength.Value = 20f;
            // damage = health * 0.1 + strength * 2 = 150 * 0.1 + 20 * 2 = 15 + 40 = 55
            Assert.AreEqual(55f, newAPITestComponent.damage.Value, 0.1f);
        }
        
        [Test]
        public void StatExtensions_GetStatObject_Works()
        {
            testObject.InitializeStats();
            
            var healthStat = testObject.GetStatObject("Health");
            Assert.IsNotNull(healthStat);
            Assert.AreEqual("Health", healthStat.Name);
            Assert.AreEqual(100f, healthStat.Value, 0.01f);
        }
        
        [Test]
        public void StatExtensions_GetAllStatObjects_Works()
        {
            testObject.InitializeStats();
            
            var allStats = testObject.GetAllStatObjects();
            var statList = System.Linq.Enumerable.ToList(allStats);
            
            Assert.IsTrue(statList.Count >= 3); // At least health, strength, damage
            Assert.IsTrue(statList.Any(s => s.Name == "Health"));
            Assert.IsTrue(statList.Any(s => s.Name == "Strength"));
            Assert.IsTrue(statList.Any(s => s.Name == "Damage"));
        }
        
        #endregion
    }
    
    // Test component for testing [Stat] attributes with legacy API
    public class TestComponent : MonoBehaviour
    {
        [Stat] public float health = 100f;
        [Stat] public int level = 1;
        [DerivedStat("level * 10")] public float damage;
    }
    
    // Test component for testing new Stat objects
    public class NewAPITestComponent : MonoBehaviour
    {
        [Stat] public Stat health = new Stat("Health", 100f);
        [Stat] public Stat strength = new Stat("Strength", 10f);
        [Stat] public Stat damage = new Stat("Damage", "health * 0.1 + strength * 2");
    }
}