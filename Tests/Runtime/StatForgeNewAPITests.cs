using NUnit.Framework;
using UnityEngine;
using StatForge;

namespace StatForge.Tests
{
    public class StatForgeNewAPITests
    {
        private GameObject testObject;
        private TestComponent testComponent;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestObject");
            testComponent = testObject.AddComponent<TestComponent>();
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
    }
    
    // Test component for testing [Stat] attributes
    public class TestComponent : MonoBehaviour
    {
        [Stat] public float health = 100f;
        [Stat] public int level = 1;
        [DerivedStat("level * 10")] public float damage;
    }
}