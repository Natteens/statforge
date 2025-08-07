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
        
        #region Ultra-Simplified API Tests
        
        [Test]
        public void Stat_OperatorOverloads_ArithmeticWork()
        {
            var stat = new Stat("Test", 100f);
            
            // Test arithmetic operators (these return float values)
            float addResult = stat + 25f;
            Assert.AreEqual(125f, addResult, 0.01f);
            
            float subtractResult = stat - 10f;
            Assert.AreEqual(90f, subtractResult, 0.01f);
            
            float multiplyResult = stat * 2f;
            Assert.AreEqual(200f, multiplyResult, 0.01f);
            
            float divideResult = stat / 4f;
            Assert.AreEqual(25f, divideResult, 0.01f);
        }
        
        [Test]
        public void Stat_OperatorOverloads_ComparisonWork()
        {
            var stat = new Stat("Test", 100f);
            
            // Test comparison operators
            Assert.IsTrue(stat > 50f);
            Assert.IsFalse(stat < 50f);
            Assert.IsTrue(stat >= 100f);
            Assert.IsTrue(stat <= 100f);
            Assert.IsTrue(stat == 100f);
            Assert.IsFalse(stat != 100f);
        }
        
        [Test]
        public void Stat_OperatorOverloads_StatComparison()
        {
            var stat1 = new Stat("Test1", 100f);
            var stat2 = new Stat("Test2", 50f);
            var stat3 = new Stat("Test3", 100f);
            
            Assert.IsTrue(stat1 > stat2);
            Assert.IsFalse(stat1 < stat2);
            Assert.IsTrue(stat1 == stat3);
            Assert.IsFalse(stat1 != stat3);
        }
        
        [Test]
        public void Stat_ImplicitConversions_Work()
        {
            var stat = new Stat("Test", 42.5f);
            
            // Test implicit conversion to float
            float floatValue = stat;
            Assert.AreEqual(42.5f, floatValue, 0.01f);
            
            // Test null stat conversion
            Stat nullStat = null;
            float nullValue = nullStat;
            Assert.AreEqual(0f, nullValue, 0.01f);
        }
        
        [Test]
        public void Stat_EnhancedMethods_Work()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            
            // Test convenience methods
            stat.Add(25f);
            Assert.AreEqual(75f, stat.Value, 0.01f);
            
            stat.Subtract(15f);
            Assert.AreEqual(60f, stat.Value, 0.01f);
            
            stat.Multiply(2f);
            Assert.AreEqual(100f, stat.Value, 0.01f); // Clamped to max
            
            // Test utility properties
            Assert.IsTrue(stat.IsAtMax);
            Assert.IsFalse(stat.IsEmpty);
            Assert.AreEqual(1f, stat.Percentage, 0.01f);
        }
        
        [Test]
        public void Stat_BuffDebuff_Methods_Work()
        {
            var stat = new Stat("Test", 100f);
            
            var buff = stat.Buff(25f, 1f);
            Assert.AreEqual(125f, stat.Value, 0.01f);
            Assert.IsNotNull(buff);
            
            var debuff = stat.Debuff(15f, 1f);
            Assert.AreEqual(110f, stat.Value, 0.01f); // 100 + 25 - 15
            Assert.IsNotNull(debuff);
        }
        
        [Test]
        public void Stat_SerializationCallbacks_Work()
        {
            var stat = new Stat("Test", 100f);
            
            // Test serialization callbacks don't crash
            stat.OnBeforeSerialize();
            stat.OnAfterDeserialize();
            
            // Value should still be accessible
            Assert.AreEqual(100f, stat.Value, 0.01f);
        }
        
        [Test]
        public void Stat_AutoInitialization_Works()
        {
            // Create a stat without explicit initialization
            var stat = new Stat("AutoInit", 50f);
            
            // Accessing Value should trigger auto-initialization
            float value = stat.Value;
            Assert.AreEqual(50f, value, 0.01f);
            
            // Should be able to add modifiers
            stat.AddTemporaryBonus(10f);
            Assert.AreEqual(60f, stat.Value, 0.01f);
        }
        
        [Test]
        public void StatOperators_ExtensionMethods_Work()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            
            // Test extension methods from StatOperators
            stat.Fill();
            Assert.AreEqual(100f, stat.Value, 0.01f);
            
            stat.Empty();
            Assert.AreEqual(0f, stat.Value, 0.01f);
            
            stat.Value = 75f;
            stat.TakeDamage(25f);
            Assert.AreEqual(50f, stat.Value, 0.01f);
            
            stat.Heal(30f);
            Assert.AreEqual(80f, stat.Value, 0.01f);
            
            Assert.IsTrue(stat.CanAfford(50f));
            Assert.IsFalse(stat.CanAfford(100f));
            
            bool consumed = stat.Consume(30f);
            Assert.IsTrue(consumed);
            Assert.AreEqual(50f, stat.Value, 0.01f);
        }
        
        [Test]
        public void StatConversions_Work()
        {
            var stat = new Stat("Test", 42.7f, 0f, 100f);
            
            // Test conversion methods
            Assert.AreEqual(42, stat.ToInt());
            Assert.AreEqual(43, stat.ToIntRounded());
            Assert.IsTrue(stat.ToBool());
            
            string percentage = stat.ToPercentageText();
            Assert.AreEqual("43%", percentage);
            
            string fraction = stat.ToFractionText();
            Assert.AreEqual("43/100", fraction);
            
            float normalized = stat.Normalize();
            Assert.AreEqual(0.427f, normalized, 0.01f);
        }
        
        [Test]
        public void Stat_ZeroSetupUsage_Works()
        {
            // Test the "zero setup" promise - stats should work immediately
            var health = new Stat("Health", 100f);
            var mana = new Stat("Mana", 50f);
            
            // Should work immediately without any initialization
            health.Value = 80f;
            Assert.AreEqual(80f, health.Value, 0.01f);
            
            // Operators should work
            bool isHealthy = health > 50f;
            Assert.IsTrue(isHealthy);
            
            // Modifiers should work
            mana.AddTemporaryBonus(25f);
            Assert.AreEqual(75f, mana.Value, 0.01f);
            
            // Events should work
            bool eventFired = false;
            health.OnValueChanged += (old, newVal) => eventFired = true;
            health.Value = 90f;
            Assert.IsTrue(eventFired);
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