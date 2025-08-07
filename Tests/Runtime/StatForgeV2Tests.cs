using NUnit.Framework;
using UnityEngine;
using StatForge;

namespace StatForge.Tests
{
    public class StatForgeV2Tests
    {
        private GameObject testObject;
        private Stat testStat;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestObject");
            testStat = new Stat("TestStat", 100f);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        #region Auto-Initialization Tests
        
        [Test]
        public void Stat_AutoInitialization_Works()
        {
            // Create a stat without explicit initialization
            var stat = new Stat();
            
            // Access Value should trigger auto-initialization
            var value = stat.Value;
            
            Assert.IsNotNull(stat.Name);
            Assert.IsTrue(stat.Name.StartsWith("Stat_"));
        }
        
        [Test]
        public void Stat_ZeroSetup_Usage_Works()
        {
            // This simulates the "zero setup" experience
            var health = new Stat();
            
            // Should work immediately without any setup
            health.Value = 80f;
            Assert.AreEqual(80f, health.Value, 0.01f);
            
            health += 25f; // Should add temporary modifier
            Assert.AreEqual(105f, health.Value, 0.01f);
        }
        
        #endregion
        
        #region Operator Overload Tests
        
        [Test]
        public void Stat_AdditionOperator_AppliesTemporaryModifier()
        {
            testStat.Value = 100f;
            testStat += 25f;
            
            Assert.AreEqual(125f, testStat.Value, 0.01f);
            Assert.AreEqual(1, testStat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_SubtractionOperator_AppliesNegativeModifier()
        {
            testStat.Value = 100f;
            testStat -= 25f;
            
            Assert.AreEqual(75f, testStat.Value, 0.01f);
            Assert.AreEqual(1, testStat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_MultiplicationOperator_AppliesMultiplier()
        {
            testStat.Value = 100f;
            testStat *= 1.5f;
            
            Assert.AreEqual(150f, testStat.Value, 0.01f);
            Assert.AreEqual(1, testStat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_DivisionOperator_AppliesDivisor()
        {
            testStat.Value = 100f;
            testStat /= 2f;
            
            Assert.AreEqual(50f, testStat.Value, 0.01f);
            Assert.AreEqual(1, testStat.Modifiers.Count);
        }
        
        [Test]
        public void Stat_ComparisonOperators_Work()
        {
            testStat.Value = 100f;
            
            Assert.IsTrue(testStat > 50f);
            Assert.IsTrue(testStat >= 100f);
            Assert.IsTrue(testStat < 150f);
            Assert.IsTrue(testStat <= 100f);
            Assert.IsTrue(testStat == 100f);
            Assert.IsTrue(testStat != 50f);
        }
        
        #endregion
        
        #region Convenience Method Tests
        
        [Test]
        public void Stat_BuffMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.Buff(25f, 5f);
            
            Assert.AreEqual(125f, testStat.Value, 0.01f);
            Assert.IsNotNull(modifier);
            Assert.IsTrue(modifier.HasDuration);
        }
        
        [Test]
        public void Stat_DebuffMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.Debuff(25f, 5f);
            
            Assert.AreEqual(75f, testStat.Value, 0.01f);
            Assert.IsNotNull(modifier);
            Assert.IsTrue(modifier.HasDuration);
        }
        
        [Test]
        public void Stat_AddBonusMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.AddBonus(25f);
            
            Assert.AreEqual(125f, testStat.Value, 0.01f);
            Assert.IsNotNull(modifier);
            Assert.IsFalse(modifier.HasDuration); // Permanent
        }
        
        [Test]
        public void Stat_OverrideMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.Override(200f, 3f);
            
            Assert.AreEqual(200f, testStat.Value, 0.01f);
            Assert.IsNotNull(modifier);
            Assert.IsTrue(modifier.HasDuration);
        }
        
        #endregion
        
        #region Extension Method Tests
        
        [Test]
        public void StatExtensions_PercentMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.Percent(50f); // +50% of current value
            
            Assert.AreEqual(150f, testStat.Value, 0.01f); // 100 + (100 * 0.5)
        }
        
        [Test]
        public void StatExtensions_MultiplyMethod_Works()
        {
            testStat.Value = 100f;
            var modifier = testStat.Multiply(150f); // 150% = 1.5x multiplier
            
            Assert.AreEqual(150f, testStat.Value, 0.01f);
        }
        
        [Test]
        public void StatExtensions_AsPercentage_Works()
        {
            testStat.Value = 75f;
            testStat.MaxValue = 100f;
            
            var percentage = testStat.AsPercentage();
            Assert.AreEqual(75f, percentage, 0.01f);
        }
        
        [Test]
        public void StatExtensions_IsFull_IsEmpty_Work()
        {
            testStat.MinValue = 0f;
            testStat.MaxValue = 100f;
            
            testStat.FillToMax();
            Assert.IsTrue(testStat.IsFull());
            Assert.IsFalse(testStat.IsEmpty());
            
            testStat.EmptyToMin();
            Assert.IsFalse(testStat.IsFull());
            Assert.IsTrue(testStat.IsEmpty());
        }
        
        [Test]
        public void StatExtensions_InRange_Works()
        {
            testStat.Value = 75f;
            
            Assert.IsTrue(testStat.InRange(50f, 100f));
            Assert.IsFalse(testStat.InRange(80f, 100f));
        }
        
        #endregion
        
        #region Conversion Tests
        
        [Test]
        public void StatConversions_ToInt_Works()
        {
            testStat.Value = 42.7f;
            int intValue = testStat.ToInt();
            
            Assert.AreEqual(42, intValue);
        }
        
        [Test]
        public void StatConversions_ToBool_Works()
        {
            testStat.Value = 0f;
            Assert.IsFalse(testStat.ToBool());
            
            testStat.Value = 1f;
            Assert.IsTrue(testStat.ToBool());
        }
        
        [Test]
        public void StatConversions_ToString_Works()
        {
            testStat.Value = 42.123f;
            
            string defaultFormat = testStat.ToString();
            Assert.AreEqual("42.12", defaultFormat);
            
            string customFormat = testStat.ToString("F1");
            Assert.AreEqual("42.1", customFormat);
        }
        
        [Test]
        public void StatConversions_GetValueOrDefault_Works()
        {
            Assert.AreEqual(100f, testStat.GetValueOrDefault(), 0.01f);
            
            Stat nullStat = null;
            Assert.AreEqual(42f, nullStat.GetValueOrDefault(42f), 0.01f);
        }
        
        #endregion
        
        #region GameObject Extension Tests
        
        [Test]
        public void GameObjectExtensions_GetOrCreateStat_Works()
        {
            var stat = testObject.GetOrCreateStat("Health", 100f);
            
            Assert.IsNotNull(stat);
            Assert.AreEqual("Health", stat.Name);
            Assert.AreEqual(100f, stat.Value, 0.01f);
            Assert.AreEqual(testObject, stat.Owner);
            
            // Second call should return same stat
            var sameStat = testObject.GetOrCreateStat("Health", 50f);
            Assert.AreSame(stat, sameStat);
            Assert.AreEqual(100f, sameStat.Value, 0.01f); // Should keep original value
        }
        
        [Test]
        public void GameObjectExtensions_BuffStats_Works()
        {
            testObject.GetOrCreateStat("Health", 100f);
            testObject.GetOrCreateStat("Mana", 50f);
            
            testObject.BuffStats(25f, 5f, "Health", "Mana");
            
            Assert.AreEqual(125f, testObject.GetStat("Health"), 0.01f);
            Assert.AreEqual(75f, testObject.GetStat("Mana"), 0.01f);
        }
        
        [Test]
        public void GameObjectExtensions_GetTotalStatValue_Works()
        {
            testObject.GetOrCreateStat("Health", 100f);
            testObject.GetOrCreateStat("Mana", 50f);
            testObject.GetOrCreateStat("Stamina", 75f);
            
            var total = testObject.GetTotalStatValue();
            Assert.AreEqual(225f, total, 0.01f);
        }
        
        [Test]
        public void GameObjectExtensions_CreateStatSnapshot_Works()
        {
            testObject.GetOrCreateStat("Health", 100f);
            testObject.GetOrCreateStat("Mana", 50f);
            
            var snapshot = testObject.CreateStatSnapshot();
            
            Assert.AreEqual(2, snapshot.Count);
            Assert.AreEqual(100f, snapshot["Health"], 0.01f);
            Assert.AreEqual(50f, snapshot["Mana"], 0.01f);
            
            // Modify values
            testObject.SetStat("Health", 150f);
            testObject.SetStat("Mana", 75f);
            
            // Restore from snapshot
            testObject.RestoreFromSnapshot(snapshot);
            
            Assert.AreEqual(100f, testObject.GetStat("Health"), 0.01f);
            Assert.AreEqual(50f, testObject.GetStat("Mana"), 0.01f);
        }
        
        #endregion
        
        #region Serialization Tests
        
        [Test]
        public void Stat_Serialization_PreservesState()
        {
            // Set up a stat with various properties
            testStat.Name = "TestStat";
            testStat.BaseValue = 100f;
            testStat.MinValue = 0f;
            testStat.MaxValue = 200f;
            testStat.Formula = "50 + 25";
            
            // Simulate serialization/deserialization by calling the interface methods
            testStat.OnBeforeSerialize();
            testStat.OnAfterDeserialize();
            
            // Verify state is preserved after deserialization
            Assert.AreEqual("TestStat", testStat.Name);
            Assert.AreEqual(100f, testStat.BaseValue, 0.01f);
            Assert.AreEqual(0f, testStat.MinValue, 0.01f);
            Assert.AreEqual(200f, testStat.MaxValue, 0.01f);
            Assert.AreEqual("50 + 25", testStat.Formula);
            
            // Value should be recalculated (base + formula)
            Assert.AreEqual(175f, testStat.Value, 0.01f); // 100 + 75
        }
        
        #endregion
    }
}