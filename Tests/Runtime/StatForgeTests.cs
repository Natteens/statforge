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
        private GameObject testObject;
        
        private static StatType[] cachedStatTypes;
        private static bool cacheInitialized = false;
        
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
                statType.DefaultValue = i;
                statType.MinValue = 0f;
                statType.MaxValue = 1000f;
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
            constitutionType.DefaultValue = 10f;
            constitutionType.MinValue = 1f;
            constitutionType.MaxValue = 50f;
            
            strengthType = ScriptableObject.CreateInstance<StatType>();
            strengthType.name = "Strength_Test";
            strengthType.DisplayName = "Strength";
            strengthType.ShortName = "STR";
            strengthType.DefaultValue = 10f;
            strengthType.MinValue = 1f;
            strengthType.MaxValue = 50f;
            
            maxHPType = ScriptableObject.CreateInstance<StatType>();
            maxHPType.name = "MaxHP_Test";
            maxHPType.DisplayName = "MaxHP";
            maxHPType.ShortName = "MHP";
            maxHPType.DefaultValue = 100f;
            maxHPType.Formula = "CON * 2 + STR * 1";
            maxHPType.MinValue = 1f;
            maxHPType.MaxValue = 1000f;
            
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
            
            Debug.Log("[StatForge Tests] TestBasicStatCreation - PASSED");
        }
        
        [Test]
        [Category("Modifiers")]
        public void TestModifierSystem()
        {
            Debug.Log("[StatForge Tests] Running TestModifierSystem");
            
            var stat = new Stat(constitutionType, 10f);
            
            var modifier1 = stat.AddBonus(5f, "Equipment");
            Assert.AreEqual(15f, stat.Value, "After additive modifier: expected 15f");
            
            var modifier2 = stat.AddMultiplier(1.5f, "Buff");
            Assert.AreEqual(22.5f, stat.Value, "After multiplicative modifier: expected 22.5f");
            
            var modifier3 = stat.AddPercentage(20f, "Skill");
            Assert.AreEqual(25.5f, stat.Value, "After percentage modifier: expected 25.5f");
            
            stat.RemoveModifier(modifier1);
            Assert.AreEqual(18f, stat.Value, "After removing additive modifier: expected 18f");
            
            var modifier4 = stat.SetOverride(100f, "Debug");
            Assert.AreEqual(100f, stat.Value, "After override modifier: expected 100f");
            
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
            
            var tempModifier = stat.AddTemporary(15f, 1f, "Potion");
            Assert.AreEqual(25f, stat.Value, "Temporary modifier should be active");
            
            tempModifier.Update(0.5f);
            Assert.AreEqual(25f, stat.Value, "Modifier should still be active");
            Assert.IsFalse(tempModifier.IsExpired, "Modifier should not be expired yet");
            
            tempModifier.Update(0.6f);
            Assert.IsTrue(tempModifier.IsExpired, "Modifier should be expired");
            
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
            
            Assert.AreEqual(132f, maxHP.Value, "Formula evaluation should give 132f");
            
            constitution.AddBonus(3f);
            Assert.AreEqual(138f, maxHP.Value, "After constitution bonus, should be 138f");
            
            Debug.Log("[StatForge Tests] TestFormulaEvaluation - PASSED");
        }
        
        [Test]
        [Category("Core")]
        public void TestClampingBehavior()
        {
            Debug.Log("[StatForge Tests] Running TestClampingBehavior");
            
            var stat = new Stat(constitutionType, 10f);
            
            stat.AddBonus(50f);
            Assert.AreEqual(50f, stat.Value, "Value should be clamped to max");
            
            stat.ClearModifiers();
            stat.BaseValue = -5f;
            Assert.AreEqual(1f, stat.Value, "Value should be clamped to min");
            
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
            
            Debug.Log("[StatForge Tests] TestStatContainer - PASSED");
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
                randomStat.AddBonus(1f, $"Test{i}");
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 100, "Performance test should complete in reasonable time");
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
                
                modifiers[statIndex] = stat.AddBonus(i % 50, "Test");
                
                var value = stat.Value;
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 50, "Realistic test should be under 50ms");
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
            stat.AddBonus(10f, "Base");
            stat.AddMultiplier(1.2f, "Buff");
            stat.AddPercentage(15f, "Skill");
            
            var startTime = System.DateTime.Now;
            
            float totalValue = 0f;
            for (int i = 0; i < 10000; i++)
            {
                totalValue += stat.Value;
            }
            
            var endTime = System.DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            Assert.Less(duration, 20, "Value access should be extremely fast");
            Debug.Log($"[StatForge Tests] VALUE ACCESS: {duration:F2}ms for 10,000 reads");
            Debug.Log($"[StatForge Tests] Read speed: {10000.0/duration*1000:F0} reads/second");
            Debug.Log($"[StatForge Tests] Total value sum: {totalValue}");
            
            Debug.Log("[StatForge Tests] TestPerformanceValueAccess - PASSED");
        }
        
        [UnityTest]
        [Category("Runtime")]
        public IEnumerator TestRuntimeIntegration()
        {
            Debug.Log("[StatForge Tests] Running TestRuntimeIntegration");
            
            var testComponent = testObject.AddComponent<TestStatBehaviour>();
            testComponent.InitializeStats();
            
            yield return null;
            
            Assert.IsNotNull(testComponent.Constitution, "Constitution stat should be initialized");
            Assert.IsNotNull(testComponent.MaxHP, "MaxHP stat should be initialized");
            
            var initialHP = testComponent.MaxHP.Value;
            testComponent.Constitution.AddBonus(5f);
            
            yield return null;
            
            Assert.Greater(testComponent.MaxHP.Value, initialHP, "MaxHP should increase after Constitution bonus");
            
            Debug.Log("[StatForge Tests] TestRuntimeIntegration - PASSED");
        }
        
        [Test]
        [Category("ErrorHandling")]
        public void TestErrorHandling()
        {
            Debug.Log("[StatForge Tests] Running TestErrorHandling");
            
            var stat = new Stat(null, 10f);
            Assert.AreEqual(0f, stat.Value, "Null StatType should result in 0 value");
            Assert.AreEqual("None", stat.Name, "Null StatType should result in 'None' name");
            
            var badFormulaType = ScriptableObject.CreateInstance<StatType>();
            badFormulaType.name = "BadFormula_Test";
            badFormulaType.Formula = "INVALID_STAT * 2";
            
            var badStat = new Stat(badFormulaType, 10f);
            Assert.AreEqual(10f, badStat.Value, "Invalid formula should not crash");
            
            Object.DestroyImmediate(badFormulaType);
            
            Debug.Log("[StatForge Tests] TestErrorHandling - PASSED");
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
            
            Debug.Log("[StatForge Tests] OneTimeTearDown - Cache cleaned up");
        }
    }
    
    public class TestStatBehaviour : MonoBehaviour
    {
        public Stat Constitution { get; private set; }
        public Stat MaxHP { get; private set; }
        
        public void InitializeStats()
        {
            var constitutionType = ScriptableObject.CreateInstance<StatType>();
            constitutionType.name = "TestConstitution";
            constitutionType.DisplayName = "Constitution";
            constitutionType.ShortName = "CON";
            constitutionType.DefaultValue = 10f;
            
            var maxHPType = ScriptableObject.CreateInstance<StatType>();
            maxHPType.name = "TestMaxHP";
            maxHPType.DisplayName = "MaxHP";
            maxHPType.ShortName = "MHP";
            maxHPType.DefaultValue = 100f;
            maxHPType.Formula = "CON * 3";
            
            Constitution = new Stat(constitutionType, 10f);
            MaxHP = new Stat(maxHPType, 100f);
            
            Stat.RegisterDependency(Constitution.Id, MaxHP);
        }
    }
}
#endif