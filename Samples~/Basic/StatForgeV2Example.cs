using UnityEngine;
using StatForge;

namespace StatForge.Examples
{
    /// <summary>
    /// Example demonstrating the ultra-simplified StatForge v2 API.
    /// This component shows how stats work with zero setup required.
    /// </summary>
    public class StatForgeV2Example : MonoBehaviour
    {
        [Header("Core Stats - Zero Setup Required!")]
        public Stat health;
        public Stat mana;
        public Stat stamina;
        
        [Header("Derived Stats")]
        public Stat maxDamage;  // Will be configured with formula
        public Stat critChance; // Will be percentage-based
        
        [Header("Character Attributes")]
        [SerializeField] private Stat strength;
        [SerializeField] private Stat intelligence;
        [SerializeField] private Stat agility;
        
        void Start()
        {
            DemonstrateZeroSetupUsage();
            DemonstrateOperatorOverloads();
            DemonstrateConvenienceMethods();
            DemonstrateTypeConversions();
        }
        
        /// <summary>
        /// Shows how stats work immediately without any setup.
        /// </summary>
        void DemonstrateZeroSetupUsage()
        {
            Debug.Log("=== Zero Setup Demo ===");
            
            // These work immediately - no initialization needed!
            health.Value = 100f;
            mana.Value = 50f;
            stamina.Value = 75f;
            
            Debug.Log($"Health: {health}");  // ToString() works automatically
            Debug.Log($"Mana: {mana}");
            Debug.Log($"Stamina: {stamina}");
            
            // Derived stats with formulas
            strength.Value = 15f;
            intelligence.Value = 12f;
            agility.Value = 18f;
            
            // These could be set up with formulas via StatDefinitions
            maxDamage.Value = strength.Value * 2f + 10f;
            critChance.Value = agility.Value * 0.5f;
            
            Debug.Log($"Max Damage: {maxDamage}");
            Debug.Log($"Crit Chance: {critChance}%");
        }
        
        /// <summary>
        /// Demonstrates the natural operator overloads.
        /// </summary>
        void DemonstrateOperatorOverloads()
        {
            Debug.Log("=== Operator Overloads Demo ===");
            
            var initialHealth = health.Value;
            
            // Addition adds temporary modifiers
            health += 25f;
            Debug.Log($"After +25 buff: {health} (was {initialHealth})");
            
            // Multiplication adds multipliers
            health *= 1.5f;
            Debug.Log($"After x1.5 multiplier: {health}");
            
            // Subtraction adds negative modifiers
            health -= 10f;
            Debug.Log($"After -10 debuff: {health}");
            
            // Comparisons work naturally
            if (health > 100f)
            {
                Debug.Log("Health is above 100!");
            }
            
            if (mana < stamina)
            {
                Debug.Log("Mana is lower than stamina");
            }
            
            // Division
            health /= 2f;
            Debug.Log($"After /2 divisor: {health}");
            
            Debug.Log($"Total modifiers on health: {health.Modifiers.Count}");
        }
        
        /// <summary>
        /// Shows the convenience methods for common operations.
        /// </summary>
        void DemonstrateConvenienceMethods()
        {
            Debug.Log("=== Convenience Methods Demo ===");
            
            // Buff/Debuff with duration
            var buffModifier = mana.Buff(20f, 5f); // +20 for 5 seconds
            Debug.Log($"Buffed mana: {mana} (modifier: {buffModifier.Id})");
            
            var debuffModifier = stamina.Debuff(15f, 3f); // -15 for 3 seconds
            Debug.Log($"Debuffed stamina: {stamina}");
            
            // Permanent modifiers
            var permBonus = strength.AddBonus(5f); // Permanent +5
            Debug.Log($"Permanent strength bonus: {strength}");
            
            // Override temporarily
            var override = agility.Override(30f, 2f); // Set to 30 for 2 seconds
            Debug.Log($"Overridden agility: {agility}");
            
            // Percentage operations
            var percentBonus = critChance.Percent(25f); // +25% of current value
            Debug.Log($"Crit chance with 25% boost: {critChance}");
            
            // Utility checks
            Debug.Log($"Health percentage: {health.AsPercentage():F1}%");
            Debug.Log($"Is health full? {health.IsFull()}");
            Debug.Log($"Is mana empty? {mana.IsEmpty()}");
        }
        
        /// <summary>
        /// Demonstrates automatic type conversions.
        /// </summary>
        void DemonstrateTypeConversions()
        {
            Debug.Log("=== Type Conversions Demo ===");
            
            // Implicit float conversion
            float healthFloat = health; // Automatic conversion
            Debug.Log($"Health as float: {healthFloat}");
            
            // Integer conversion
            int healthInt = health.ToInt();
            Debug.Log($"Health as int: {healthInt}");
            
            // Boolean conversion
            bool hasHealth = health.ToBool(); // true if > 0
            Debug.Log($"Has health: {hasHealth}");
            
            // String formatting
            string formattedHealth = health.ToString("F1");
            Debug.Log($"Formatted health: {formattedHealth}");
            
            // Null safety
            Stat nullStat = null;
            float safeValue = nullStat.GetValueOrDefault(42f);
            Debug.Log($"Safe value from null stat: {safeValue}");
        }
        
        void Update()
        {
            // In a real game, you might update modifier durations
            // The StatForge system handles this automatically
        }
        
        [ContextMenu("Heal to Full")]
        void HealToFull()
        {
            health.FillToMax();
            mana.FillToMax();
            stamina.FillToMax();
            Debug.Log("Healed to full!");
        }
        
        [ContextMenu("Reset All Stats")]
        void ResetAllStats()
        {
            health.ClearModifiers();
            mana.ClearModifiers();
            stamina.ClearModifiers();
            strength.ClearModifiers();
            intelligence.ClearModifiers();
            agility.ClearModifiers();
            maxDamage.ClearModifiers();
            critChance.ClearModifiers();
            Debug.Log("All stat modifiers cleared!");
        }
        
        [ContextMenu("Apply Random Buffs")]
        void ApplyRandomBuffs()
        {
            health.Buff(Random.Range(10f, 30f), Random.Range(3f, 8f));
            mana.Buff(Random.Range(5f, 15f), Random.Range(2f, 6f));
            stamina.Buff(Random.Range(8f, 20f), Random.Range(4f, 10f));
            Debug.Log("Applied random buffs!");
        }
    }
}