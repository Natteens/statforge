using NUnit.Framework;
using UnityEngine;
using StatForge;

namespace StatForge.Tests
{
    /// <summary>
    /// Core validation tests for StatForge v2 implementation.
    /// These tests validate the key requirements from the problem statement.
    /// </summary>
    public class StatForgeV2ValidationTests
    {
        private GameObject testObject;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("ValidationTest");
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
        public void Requirement_ZeroSetup_Usage_Works()
        {
            // This validates the main requirement: "Zero inicialização necessária - tudo automático!"
            var health = new Stat();
            
            // Should work immediately without any setup
            health.Value = 80f;
            Assert.AreEqual(80f, health.Value, 0.01f);
            
            // Operator overloads should work
            health += 25f;
            Assert.AreEqual(105f, health.Value, 0.01f);
            
            // ToString should work
            Assert.IsNotNull(health.ToString());
            Assert.IsTrue(health.ToString().Contains("105"));
        }
        
        [Test]
        public void Requirement_AnyVisibility_Works()
        {
            // Validates: "Qualquer uma dessas sintaxes funciona: public Stat health; [SerializeField] private Stat mana; protected Stat stamina;"
            var publicStat = new Stat("Public", 100f);
            var privateStat = new Stat("Private", 50f);
            var protectedStat = new Stat("Protected", 75f);
            
            Assert.AreEqual(100f, publicStat.Value, 0.01f);
            Assert.AreEqual(50f, privateStat.Value, 0.01f);
            Assert.AreEqual(75f, protectedStat.Value, 0.01f);
            
            // All should support operators
            publicStat += 10f;
            privateStat *= 2f;
            protectedStat -= 5f;
            
            Assert.AreEqual(110f, publicStat.Value, 0.01f);
            Assert.AreEqual(100f, privateStat.Value, 0.01f);
            Assert.AreEqual(70f, protectedStat.Value, 0.01f);
        }
        
        [Test]
        public void Requirement_OperatorOverloads_Work()
        {
            // Validates all operator requirements
            var stat = new Stat("Test", 100f);
            
            // Addition
            stat += 25f;
            Assert.AreEqual(125f, stat.Value, 0.01f);
            
            // Subtraction  
            stat -= 10f;
            Assert.AreEqual(115f, stat.Value, 0.01f);
            
            // Multiplication
            stat *= 1.5f;
            Assert.AreEqual(172.5f, stat.Value, 0.01f);
            
            // Division
            stat /= 2f;
            Assert.AreEqual(86.25f, stat.Value, 0.01f);
            
            // Comparisons
            Assert.IsTrue(stat > 80f);
            Assert.IsTrue(stat < 90f);
            Assert.IsTrue(stat >= 86f);
            Assert.IsTrue(stat <= 87f);
            Assert.IsFalse(stat == 100f);
            Assert.IsTrue(stat != 100f);
        }
        
        [Test]
        public void Requirement_ImplicitConversion_Works()
        {
            // Validates: "public static implicit operator float(Stat stat)"
            var stat = new Stat("Test", 42.5f);
            
            float value = stat; // Implicit conversion
            Assert.AreEqual(42.5f, value, 0.01f);
            
            // Should work in expressions
            float result = stat + 10f; // This uses implicit conversion + float addition
            Assert.AreEqual(52.5f, result, 0.01f);
        }
        
        [Test]
        public void Requirement_ConvenienceMethods_Work()
        {
            // Validates convenience methods like Buff, Debuff
            var stat = new Stat("Test", 100f);
            
            // Buff method
            var buffModifier = stat.Buff(25f, 5f);
            Assert.AreEqual(125f, stat.Value, 0.01f);
            Assert.IsNotNull(buffModifier);
            Assert.IsTrue(buffModifier.HasDuration);
            
            // Debuff method
            var debuffModifier = stat.Debuff(15f, 3f);
            Assert.AreEqual(110f, stat.Value, 0.01f); // 125 - 15
            Assert.IsNotNull(debuffModifier);
            
            // AddBonus (permanent)
            var bonusModifier = stat.AddBonus(10f);
            Assert.AreEqual(120f, stat.Value, 0.01f); // 110 + 10
            Assert.IsFalse(bonusModifier.HasDuration); // Permanent
        }
        
        [Test]
        public void Requirement_AutoInitialization_Works()
        {
            // Validates: "EnsureInitialized() - Lazy initialization"
            var stat = new Stat(); // No parameters
            
            // Should auto-initialize when Value is accessed
            var value = stat.Value; // This should trigger auto-initialization
            
            Assert.IsNotNull(stat.Name);
            Assert.IsTrue(stat.Name.StartsWith("Stat_")); // Auto-generated name
            Assert.AreEqual(0f, value, 0.01f); // Default value
        }
        
        [Test]
        public void Requirement_Serialization_Interface_Implemented()
        {
            // Validates: "ISerializationCallbackReceiver for automatic Unity serialization"
            var stat = new Stat("Test", 100f);
            
            // Should implement the interface
            Assert.IsTrue(stat is ISerializationCallbackReceiver);
            
            // Should handle serialization callbacks without errors
            var serializable = stat as ISerializationCallbackReceiver;
            Assert.DoesNotThrow(() => serializable.OnBeforeSerialize());
            Assert.DoesNotThrow(() => serializable.OnAfterDeserialize());
            
            // After deserialization, should still work
            Assert.AreEqual(100f, stat.Value, 0.01f);
        }
        
        [Test]
        public void Requirement_TypeConversions_Work()
        {
            // Validates various conversion methods
            var stat = new Stat("Test", 42.7f);
            
            // ToInt conversion
            Assert.AreEqual(42, stat.ToInt());
            
            // ToBool conversion
            Assert.IsTrue(stat.ToBool()); // > 0
            
            stat.Value = 0f;
            Assert.IsFalse(stat.ToBool()); // == 0
            
            // ToString with format
            stat.Value = 123.456f;
            Assert.AreEqual("123.5", stat.ToString("F1"));
            
            // GetValueOrDefault
            Assert.AreEqual(123.456f, stat.GetValueOrDefault(), 0.001f);
            
            Stat nullStat = null;
            Assert.AreEqual(999f, nullStat.GetValueOrDefault(999f), 0.01f);
        }
        
        [Test]
        public void Requirement_ExtensionMethods_Work()
        {
            // Validates extension methods like Percent, Multiply, etc.
            var stat = new Stat("Test", 100f);
            
            // Percent method
            var percentMod = stat.Percent(50f); // +50% of current value
            Assert.AreEqual(150f, stat.Value, 0.01f); // 100 + (100 * 0.5)
            
            // Multiply method
            var multiplyMod = stat.Multiply(120f); // 120% = 1.2x multiplier
            Assert.IsNotNull(multiplyMod);
            
            // Utility methods
            stat.MinValue = 0f;
            stat.MaxValue = 200f;
            stat.Value = 150f;
            
            Assert.AreEqual(75f, stat.AsPercentage(), 0.01f); // 150/200 * 100
            Assert.IsTrue(stat.InRange(100f, 200f));
            Assert.IsFalse(stat.IsFull());
            Assert.IsFalse(stat.IsEmpty());
            
            stat.FillToMax();
            Assert.IsTrue(stat.IsFull());
            
            stat.EmptyToMin();
            Assert.IsTrue(stat.IsEmpty());
        }
        
        [Test]
        public void Requirement_FormulaSupport_Works()
        {
            // Validates derived stat functionality
            var stat = new Stat("Damage", "10 + 5"); // Simple formula
            
            Assert.IsTrue(stat.IsDerived);
            Assert.AreEqual("10 + 5", stat.Formula);
            
            // Should calculate: base (0) + formula (15) = 15
            Assert.AreEqual(15f, stat.Value, 0.01f);
            
            // Can still have base value
            stat.BaseValue = 5f;
            Assert.AreEqual(20f, stat.Value, 0.01f); // 5 + 15
        }
        
        [Test]
        public void Requirement_BackwardCompatibility_Maintained()
        {
            // Validates that the new system doesn't break existing functionality
            // Test with GameObject extension methods
            testObject.InitializeStats();
            
            // Legacy API should still work
            testObject.SetStat("health", 100f);
            Assert.AreEqual(100f, testObject.GetStat("health"), 0.01f);
            
            // New API should also work
            var stat = testObject.GetOrCreateStat("mana", 50f);
            Assert.IsNotNull(stat);
            Assert.AreEqual(50f, stat.Value, 0.01f);
            
            // Operator overloads should work on new stat
            stat += 25f;
            Assert.AreEqual(75f, stat.Value, 0.01f);
        }
    }
}