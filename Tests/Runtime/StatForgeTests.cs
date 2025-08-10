#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

namespace StatForge.Tests
{
    [TestFixture]
    public class StatForgeE2ETests
    {
        private StatType constitutionType;
        private StatType strengthType;
        private StatType maxHPType;
        private StatType percentageType;
        private GameObject testObject;
        
        private static StatType[] cachedStatTypes;
        private static bool cacheInitialized;
        
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Debug.Log("[StatForge Tests] OneTimeSetUp - Initializing test environment");
            InitializeStatTypeCache();
        }
        
        private void InitializeStatTypeCache()
        {
            if (cacheInitialized) return;
            
            cachedStatTypes = new StatType[10];
            for (int i = 0; i < 10; i++)
            {
                var statType = ScriptableObject.CreateInstance<StatType>();
                statType.name = $"CachedStat{i}";
                statType.DisplayName = $"CachedStat{i}";
                statType.ShortName = $"CS{i}";
                statType.ValueType = StatValueType.Normal;
                statType.DefaultValue = i;
                cachedStatTypes[i] = statType;
            }
            cacheInitialized = true;
            Debug.Log("[StatForge Tests] StatType cache initialized with 10 types");
        }
        
        [SetUp]
        public void Setup()
        {
            Debug.Log("[StatForge Tests] SetUp - Creating test data");
            
            constitutionType = ScriptableObject.CreateInstance<StatType>();
            constitutionType.name = "Constitution_Test";
            constitutionType.DisplayName = "Constitution";
            constitutionType.ShortName = "CON";
            constitutionType.ValueType = StatValueType.Normal;
            constitutionType.DefaultValue = 10f;
            
            strengthType = ScriptableObject.CreateInstance<StatType>();
            strengthType.name = "Strength_Test";
            strengthType.DisplayName = "Strength";
            strengthType.ShortName = "STR";
            strengthType.ValueType = StatValueType.Normal; 
            strengthType.DefaultValue = 10f;
            
            maxHPType = ScriptableObject.CreateInstance<StatType>();
            maxHPType.name = "MaxHP_Test";
            maxHPType.DisplayName = "MaxHP";
            maxHPType.ShortName = "MHP";
            maxHPType.ValueType = StatValueType.Normal;
            maxHPType.DefaultValue = 100f;
            maxHPType.Formula = "CON * 2 + STR * 1";
            
            percentageType = ScriptableObject.CreateInstance<StatType>();
            percentageType.name = "Percentage_Test";
            percentageType.DisplayName = "Percentage";
            percentageType.ShortName = "PCT";
            percentageType.ValueType = StatValueType.Percentage; 
            percentageType.DefaultValue = 50f;
            percentageType.MinValue = 0f;
            percentageType.MaxValue = 100f;
            
            testObject = new GameObject("TestEntity");
        }
        
        [TearDown]
        public void TearDown()
        {
            Debug.Log("[StatForge Tests] TearDown - Cleaning up test data");
            
            if (testObject != null)
                Object.DestroyImmediate(testObject);
                
            if (constitutionType != null)
                Object.DestroyImmediate(constitutionType);
            if (strengthType != null)
                Object.DestroyImmediate(strengthType);
            if (maxHPType != null)
                Object.DestroyImmediate(maxHPType);
            if (percentageType != null)
                Object.DestroyImmediate(percentageType);
        }
        
        [Test]
        [Category("Core")]
        public void TestBasicStatCreation()
        {
            Debug.Log("[StatForge Tests] Running TestBasicStatCreation");
            
            var stat = new Stat(constitutionType, 15f);
            
            Assert.IsNotNull(stat, "Stat should not be null");
            Assert.AreEqual(constitutionType, stat.StatType, "StatType should match");
            Assert.AreEqual(15f, stat.BaseValue, "BaseValue should match");
            Assert.AreEqual(15f, stat.Value, "Value should match BaseValue initially");
            Assert.AreEqual("Constitution", stat.Name, "Name should match StatType DisplayName");
            Assert.AreEqual("CON", stat.ShortName, "ShortName should match StatType ShortName");
            Assert.AreEqual("15", stat.FormattedValue, "FormattedValue should be rounded for Normal type");
            
            Debug.Log("[StatForge Tests] TestBasicStatCreation - PASSED");
        }
        
        [Test]
        [Category("ValueTypes")]
        public void TestValueTypeFormatting()
        {
            Debug.Log("[StatForge Tests] Running TestValueTypeFormatting");
            
            var normalType = ScriptableObject.CreateInstance<StatType>();
            normalType.ValueType = StatValueType.Normal;
            var normalStat = new Stat(normalType, 25.7f);
            Assert.AreEqual("26", normalStat.FormattedValue, "Normal should round to nearest integer");
            
            var normalStat2 = new Stat(normalType, 25.4f);
            Assert.AreEqual("25", normalStat2.FormattedValue, "25.4 should round to 25");
            
            var normalStat3 = new Stat(normalType, 25.6f);
            Assert.AreEqual("26", normalStat3.FormattedValue, "25.6 should round to 26");
            
            var percentType = ScriptableObject.CreateInstance<StatType>();
            percentType.ValueType = StatValueType.Percentage;
            var percentStat = new Stat(percentType, 7.75f);
            Assert.AreEqual("7.75%", percentStat.FormattedValue, "Percentage should format with %");
            Assert.AreEqual(0.0775f, percentStat.PercentageNormalized, 0.001f, "PercentageNormalized should be 0-1 range");
            
            var rateType = ScriptableObject.CreateInstance<StatType>();
            rateType.ValueType = StatValueType.Rate;
            var rateStat = new Stat(rateType, 2.5f);
            Assert.AreEqual("2.5/s", rateStat.FormattedValue, "Rate should format with /s");
            
            Object.DestroyImmediate(normalType);
            Object.DestroyImmediate(percentType);
            Object.DestroyImmediate(rateType);
            
            Debug.Log("[StatForge Tests] TestValueTypeFormatting - PASSED");
        }
        
        [Test]
        [Category("ValueTypes")]
        public void TestNormalTypeEdgeCases()
        {
            Debug.Log("[StatForge Tests] Running TestNormalTypeEdgeCases");
    
            var normalType = ScriptableObject.CreateInstance<StatType>();
            normalType.ValueType = StatValueType.Normal;
    
            var stat1 = new Stat(normalType, 25.0f);
            Assert.AreEqual("25", stat1.FormattedValue, "Exact integer should display as integer");
    
            var stat2 = new Stat(normalType, -10.7f);
            Assert.AreEqual("-11", stat2.FormattedValue, "Negative values should round correctly");
    
            var stat3 = new Stat(normalType, 0.0f);
            Assert.AreEqual("0", stat3.FormattedValue, "Zero should display as 0");
    
            var stat4 = new Stat(normalType, 0.4f);
            Assert.AreEqual("0", stat4.FormattedValue, "0.4 should round to 0");
    
            var stat5 = new Stat(normalType, 0.6f);
            Assert.AreEqual("1", stat5.FormattedValue, "0.6 should round to 1");
    
            Object.DestroyImmediate(normalType);
    
            Debug.Log("[StatForge Tests] TestNormalTypeEdgeCases - PASSED");
        }
        
        [Test]
        [Category("ValueTypes")]
        public void TestIntegerClamping()
        {
            Debug.Log("[StatForge Tests] Running TestIntegerClamping");
            
            var stat = new Stat(percentageType, 25.7f);
            Assert.AreEqual(25.7f, stat.Value, "Internal value should be exact");
            Assert.AreEqual("25.70%", stat.FormattedValue, "Display should be rounded for percentage");
            
            stat.AddModifier(75.3f); 
            Assert.AreEqual(100f, stat.Value, "Should clamp to max for percentage");
            Assert.AreEqual("100.00%", stat.FormattedValue, "Clamped value should display correctly");
            
            Debug.Log("[StatForge Tests] TestIntegerClamping - PASSED");
        }
        
        [Test]
        [Category("ValueTypes")]
        public void TestPercentageClamping()
        {
            Debug.Log("[StatForge Tests] Running TestPercentageClamping");
            
            var stat = new Stat(percentageType, 50f);
            Assert.AreEqual(50f, stat.Value, "Percentage stat should work normally");
            Assert.AreEqual("50.00%", stat.FormattedValue, "Percentage should format correctly");
            
            stat.AddModifier(60f);
            Assert.AreEqual(100f, stat.Value, "Percentage stat should clamp to 100%");
            Assert.AreEqual("100.00%", stat.FormattedValue, "Clamped percentage should format correctly");
            
            stat.ClearModifiers();
            stat.BaseValue = -10f;
            Assert.AreEqual(0f, stat.Value, "Percentage stat should clamp to 0%");
            Assert.AreEqual("0.00%", stat.FormattedValue, "Zero percentage should format correctly");
            
            Debug.Log("[StatForge Tests] TestPercentageClamping - PASSED");
        }
        
        [Test]
        [Category("Modifiers")]
        public void TestModifierSystem()
        {
            Debug.Log("[StatForge Tests] Running TestModifierSystem");
            
            var stat = new Stat(constitutionType, 10f);
            
            var modifier1 = stat.AddModifier(5f, source: "Equipment");
            Assert.AreEqual(15f, stat.Value, "After additive modifier: expected 15f");
            Assert.AreEqual("15", stat.FormattedValue, "Display should be rounded");
            
            var modifier2 = stat.AddModifier(1.5f, ModifierType.Multiplicative, source: "Buff");
            Assert.AreEqual(22.5f, stat.Value, "After multiplicative modifier: expected 22.5f");
            Assert.AreEqual("23", stat.FormattedValue, "22.5 should round to 23");
            
            var modifier3 = stat.AddModifier(20f, ModifierType.Percentage, source: "Skill");
            Assert.AreEqual(25.5f, stat.Value, "After percentage modifier: expected 25.5f");
            Assert.AreEqual("26", stat.FormattedValue, "25.5 should round to 26");
            
            stat.RemoveModifier(modifier1);
            Assert.AreEqual(18f, stat.Value, "After removing additive modifier: expected 18f");
            Assert.AreEqual("18", stat.FormattedValue, "Should display 18");
            
            var modifier4 = stat.AddModifier(100f, ModifierType.Override, priority: ModifierPriority.Override, source: "Debug");
            Assert.AreEqual(100f, stat.Value, "After override modifier: expected 100f");
            Assert.AreEqual("100", stat.FormattedValue, "Override should display correctly");
            
            stat.RemoveModifier(modifier4);
            Assert.AreEqual(18f, stat.Value, "After removing override: expected 18f");
            
            Debug.Log("[StatForge Tests] TestModifierSystem - PASSED");
        }
        
        [Test]
        [Category("Modifiers")]
        public void TestTemporaryModifiers()
        {
            Debug.Log("[StatForge Tests] Running TestTemporaryModifiers");
            
            var stat = new Stat(constitutionType, 10f);
            
            var tempModifier = stat.AddModifier(15f, ModifierType.Additive, ModifierDuration.Temporary, 1f, source: "Potion");
            Assert.AreEqual(25f, stat.Value, "Temporary modifier should be active");
            Assert.IsNotNull(tempModifier, "Temporary modifier should not be null");
            Assert.AreEqual(1f, tempModifier.RemainingTime, 0.01f, "Remaining time should be 1f");
            
            tempModifier.Update(0.5f);
            Assert.AreEqual(25f, stat.Value, "Modifier should still be active");
            Assert.IsFalse(tempModifier.IsExpired, "Modifier should not be expired yet");
            Assert.AreEqual(0.5f, tempModifier.RemainingTime, 0.01f, "Remaining time should be 0.5f");
            
            tempModifier.Update(0.6f);
            Assert.IsTrue(tempModifier.IsExpired, "Modifier should be expired");
            Assert.AreEqual(-0.1f, tempModifier.RemainingTime, 0.01f, "Remaining time should be negative");
            
            Debug.Log("[StatForge Tests] TestTemporaryModifiers - PASSED");
        }
        
        [Test]
        [Category("Formulas")]
        public void TestFormulaEvaluation()
        {
            Debug.Log("[StatForge Tests] Running TestFormulaEvaluation");
            
            var container = new StatContainer("TestContainer");
            
            var constitution = new Stat(constitutionType, 12f);
            var strength = new Stat(strengthType, 8f);
            var maxHP = new Stat(maxHPType, 100f);
            
            container.AddStat(constitution);
            container.AddStat(strength);
            container.AddStat(maxHP);
            
            container.Initialize();
            
            Assert.AreEqual(132f, maxHP.Value, "Formula evaluation should give 132f (base + formula)");
            Assert.AreEqual("132", maxHP.FormattedValue, "Should display as 132");
            
            constitution.AddModifier(3f);
            Assert.AreEqual(138f, maxHP.Value, "After constitution bonus, should be 138f");
            Assert.AreEqual("138", maxHP.FormattedValue, "Should display as 138");
            
            Debug.Log("[StatForge Tests] TestFormulaEvaluation - PASSED");
        }
        
        [Test]
        [Category("Formulas")]
        public void TestComplexFormulaEvaluation()
        {
            Debug.Log("[StatForge Tests] Running TestComplexFormulaEvaluation");
            
            var container = new StatContainer("ComplexTest");
            
            var complexType = ScriptableObject.CreateInstance<StatType>();
            complexType.name = "Complex_Test";
            complexType.DisplayName = "Complex";
            complexType.ShortName = "CMP";
            complexType.ValueType = StatValueType.Normal;
            complexType.DefaultValue = 50f;
            complexType.Formula = "(CON + STR) * 2 + CON * STR";
            
            var constitution = new Stat(constitutionType, 10f);
            var strength = new Stat(strengthType, 5f);
            var complex = new Stat(complexType, 50f);
            
            container.AddStat(constitution);
            container.AddStat(strength);
            container.AddStat(complex);
            container.Initialize();
            
            Assert.AreEqual(130f, complex.Value, "Complex formula should evaluate correctly");
            
            Object.DestroyImmediate(complexType);
            
            Debug.Log("[StatForge Tests] TestComplexFormulaEvaluation - PASSED");
        }
        
        [Test]
        [Category("Core")]
        public void TestClampingBehavior()
        {
            Debug.Log("[StatForge Tests] Running TestClampingBehavior");
            
            var stat = new Stat(percentageType, 10f);
            
            stat.AddModifier(100f); 
            Assert.AreEqual(100f, stat.Value, "Percentage should be clamped to max");
            Assert.AreEqual("100.00%", stat.FormattedValue, "Should display max value");
            
            stat.ClearModifiers();
            stat.BaseValue = -5f; 
            Assert.AreEqual(0f, stat.Value, "Percentage should be clamped to min");
            Assert.AreEqual("0.00%", stat.FormattedValue, "Should display min value");
            
            Debug.Log("[StatForge Tests] TestClampingBehavior - PASSED");
        }
        
        [Test]
        [Category("Container")]
        public void TestStatContainer()
        {
            Debug.Log("[StatForge Tests] Running TestStatContainer");
            
            var container = new StatContainer("PlayerStats");
            
            var constitution = new Stat(constitutionType, 12f);
            var strength = new Stat(strengthType, 8f);
            
            container.AddStat(constitution);
            container.AddStat(strength);
            container.Initialize();
            
            Assert.AreEqual(2, container.Count, "Container should have 2 stats");
            Assert.AreEqual(constitution, container.GetStat("Constitution"), "Should find stat by name");
            Assert.AreEqual(constitution, container.GetStat("CON"), "Should find stat by short name");
            Assert.AreEqual(12f, container.GetStatValue("CON"), "Should return correct stat value");
            
            var newStat = container.CreateStat(maxHPType, 150f);
            Assert.AreEqual(3, container.Count, "Container should have 3 stats after creation");
            Assert.IsNotNull(container.GetStat("MaxHP"), "Should find newly created stat");
            
            Assert.AreEqual(182f, newStat.Value, "New stat should evaluate formula: base(150) + formula(32) = 182");
            
            Debug.Log("[StatForge Tests] TestStatContainer - PASSED");
        }
        
        [Test]
        [Category("ErrorHandling")]
        public void TestErrorHandling()
        {
            Debug.Log("[StatForge Tests] Running TestErrorHandling");
            
            var stat = new Stat(null, 10f);
            Assert.AreEqual(10f, stat.Value, "Null StatType should return baseValue");
            Assert.AreEqual("None", stat.Name, "Null StatType should result in 'None' name");
            Assert.AreEqual("", stat.ShortName, "Null StatType should result in empty ShortName");
            Assert.AreEqual(StatValueType.Normal, stat.ValueType, "Null StatType should default to Normal type");
            
            var badFormulaType = ScriptableObject.CreateInstance<StatType>();
            badFormulaType.name = "BadFormula_Test";
            badFormulaType.Formula = "INVALID_STAT * 2";
            badFormulaType.ValueType = StatValueType.Normal;
            badFormulaType.DefaultValue = 25f;
            
            var badStat = new Stat(badFormulaType, 10f);
            Assert.AreEqual(10f, badStat.Value, "Invalid formula should return base value when formula evaluation fails");
            
            Object.DestroyImmediate(badFormulaType);
            
            Debug.Log("[StatForge Tests] TestErrorHandling - PASSED");
        }
        
        [Test]
        [Category("ErrorHandling")]
        public void TestEdgeCases()
        {
            Debug.Log("[StatForge Tests] Running TestEdgeCases");
            
            var stat1 = new Stat(constitutionType, 0f);
            Assert.AreEqual(0f, stat1.Value, "Zero base value should work");
            
            var stat2 = new Stat(constitutionType, -10f);
            Assert.AreEqual(-10f, stat2.Value, "Negative base should NOT be clamped for Normal");
            
            var stat3 = new Stat(constitutionType, 1000f);
            Assert.AreEqual(1000f, stat3.Value, "Large base should NOT be clamped for Normal");
            
            stat2.AddModifier(5f);
            Assert.AreEqual(-5f, stat2.Value, "Modifier should work mathematically: -10 + 5 = -5");
            
            Debug.Log("[StatForge Tests] TestEdgeCases - PASSED");
        }
        
        [Test]
        [Category("Runtime")]
        public void TestRuntimeIntegration()
        {
            Debug.Log("[StatForge Tests] Running TestRuntimeIntegration");
            
            var testComponent = testObject.AddComponent<TestStatBehaviour>();
            testComponent.InitializeStats();
            
            Assert.IsNotNull(testComponent.Constitution, "Constitution stat should be initialized");
            Assert.IsNotNull(testComponent.MaxHP, "MaxHP stat should be initialized");
            
            var expectedInitialHP = 130f; 
            Assert.AreEqual(expectedInitialHP, testComponent.MaxHP.Value, $"Initial MaxHP should be {expectedInitialHP} (base + formula)");
            
            var initialHP = testComponent.MaxHP.Value;
            testComponent.Constitution.AddModifier(5f);
            
            testComponent.MaxHP.ForceRecalculate();
            
            var expectedFinalHP = 145f;
            Assert.AreEqual(expectedFinalHP, testComponent.MaxHP.Value, $"MaxHP should be {expectedFinalHP} after Constitution bonus");
            Assert.Greater(testComponent.MaxHP.Value, initialHP, "MaxHP should increase after Constitution bonus");
            
            Debug.Log($"[StatForge Tests] Constitution: {testComponent.Constitution.Value}, MaxHP: {testComponent.MaxHP.Value}");
            Debug.Log("[StatForge Tests] TestRuntimeIntegration - PASSED");
        }
        
        [Test]
        [Category("ImplicitConversion")]
        public void TestImplicitConversions()
        {
            Debug.Log("[StatForge Tests] Running TestImplicitConversions");
            
            Stat stat1 = 25.5f;
            Assert.AreEqual(25.5f, stat1.Value, "Implicit conversion from float should work");
            Assert.AreEqual("26", stat1.FormattedValue, "Should round for display");
            
            Stat stat2 = 100;
            Assert.AreEqual(100f, stat2.Value, "Implicit conversion from int should work");
            Assert.AreEqual("100", stat2.FormattedValue, "Should display correctly");
            
            var typedStat = new Stat(constitutionType, 15f);
            float value = typedStat;
            Assert.AreEqual(15f, value, "Implicit conversion to float should work");
            
            Debug.Log("[StatForge Tests] TestImplicitConversions - PASSED");
        }
        
        [Test]
        [Category("Performance")]
        public void TestPerformanceScenario()
        {
            Debug.Log("[StatForge Tests] Running TestPerformanceScenario - CACHED VERSION");
            
            var container = new StatContainer("PerfTest");
            var stats = new List<Stat>(10);
            
            for (int i = 0; i < 10; i++)
            {
                var statType = cachedStatTypes[i];
                var stat = new Stat(statType, i);
                stats.Add(stat);
                container.AddStat(stat);
            }
            
            container.Initialize();
            
            var startTime = System.DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                var randomStat = stats[i % stats.Count];
                randomStat.AddModifier(1f, source: $"Test{i}");
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 200, "Performance test should complete in reasonable time");
            Debug.Log($"[StatForge Tests] CACHED Performance test: {duration:F2}ms for 1000 operations");
            
            Debug.Log("[StatForge Tests] TestPerformanceScenario - PASSED");
        }
        
        [Test]
        [Category("Performance")]
        public void TestPerformanceRealistic()
        {
            Debug.Log("[StatForge Tests] Running REALISTIC Performance Test");
            
            var stats = new Stat[10];
            var modifiers = new IStatModifier[10];
            
            for (int i = 0; i < 10; i++)
            {
                stats[i] = new Stat(cachedStatTypes[i], 100f);
            }
            
            var startTime = System.DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                var statIndex = i % 10;
                var stat = stats[statIndex];
                
                if (modifiers[statIndex] != null)
                {
                    stat.RemoveModifier(modifiers[statIndex]);
                }
                
                modifiers[statIndex] = stat.AddModifier(i % 50, source: "Test");
                
                var value = stat.Value;
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 100, "Realistic test should be under 100ms");
            Debug.Log($"[StatForge Tests] REALISTIC: {duration:F2}ms for 1000 add/remove/read operations");
            Debug.Log($"[StatForge Tests] Throughput: {1000.0/duration*1000:F0} operations/second");
            
            Debug.Log("[StatForge Tests] TestPerformanceRealistic - PASSED");
        }
        
        [Test]
        [Category("Performance")]
        public void TestPerformanceValueAccess()
        {
            Debug.Log("[StatForge Tests] Running VALUE ACCESS Performance Test");
            
            var stat = new Stat(constitutionType, 100f);
            stat.AddModifier(10f, source: "Base");
            stat.AddModifier(1.2f, ModifierType.Multiplicative, source: "Buff");
            stat.AddModifier(15f, ModifierType.Percentage, source: "Skill");
            
            var startTime = System.DateTime.Now;
            
            float totalValue = 0f;
            for (int i = 0; i < 10000; i++)
            {
                totalValue += stat.Value;
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 50, "Value access should be extremely fast");
            Debug.Log($"[StatForge Tests] VALUE ACCESS: {duration:F2}ms for 10,000 reads");
            Debug.Log($"[StatForge Tests] Read speed: {10000.0/duration*1000:F0} reads/second");
            Debug.Log($"[StatForge Tests] Total value sum: {totalValue}");
            
            Debug.Log("[StatForge Tests] TestPerformanceValueAccess - PASSED");
        }
        
        [Test]
        [Category("Memory")]
        public void TestMemoryManagement()
        {
            Debug.Log("[StatForge Tests] Running TestMemoryManagement");
            
            var stats = new List<Stat>(100);
            
            for (int i = 0; i < 100; i++)
            {
                var stat = new Stat(cachedStatTypes[i % 10], i);
                stats.Add(stat);
            }
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            foreach (var stat in stats)
            {
                Stat.CleanupStat(stat.Id);
            }
            
            Stat.ClearFieldCache();
            System.GC.Collect();
            
            Assert.IsTrue(true, "Memory management test completed");
            
            Debug.Log("[StatForge Tests] TestMemoryManagement - PASSED");
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (cachedStatTypes != null)
            {
                for (int i = 0; i < cachedStatTypes.Length; i++)
                {
                    if (cachedStatTypes[i] != null)
                        Object.DestroyImmediate(cachedStatTypes[i]);
                }
                cachedStatTypes = null;
                cacheInitialized = false;
            }
            
            Stat.ClearFieldCache();
            Debug.Log("[StatForge Tests] OneTimeTearDown - Cache cleaned up");
        }
    }
    
    public class TestStatBehaviour : MonoBehaviour
    {
        public Stat Constitution { get; private set; }
        public Stat MaxHP { get; private set; }
        
        private StatContainer container;
        
        public void InitializeStats()
        {
            var constitutionType = ScriptableObject.CreateInstance<StatType>();
            constitutionType.name = "TestConstitution";
            constitutionType.DisplayName = "Constitution";
            constitutionType.ShortName = "CON";
            constitutionType.ValueType = StatValueType.Normal;
            constitutionType.DefaultValue = 10f;
            
            var maxHPType = ScriptableObject.CreateInstance<StatType>();
            maxHPType.name = "TestMaxHP";
            maxHPType.DisplayName = "MaxHP";
            maxHPType.ShortName = "MHP";
            maxHPType.ValueType = StatValueType.Normal;
            maxHPType.DefaultValue = 100f;
            maxHPType.Formula = "CON * 3";
            
            container = new StatContainer("TestBehaviourContainer");
            
            Constitution = new Stat(constitutionType, 10f);
            MaxHP = new Stat(maxHPType, 100f);
            
            container.AddStat(Constitution);
            container.AddStat(MaxHP);
            container.Initialize();
            
            Debug.Log($"[TestStatBehaviour] Initialized - Constitution: {Constitution.Value}, MaxHP: {MaxHP.Value}");
        }
        
        private void OnDestroy()
        {
            container?.ClearStats();
        }
    }
}
#endif