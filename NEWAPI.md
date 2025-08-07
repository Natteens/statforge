# StatForge - Complete Refactoring Documentation

## Overview

StatForge has been completely refactored to provide a modular, powerful, and extremely simple-to-use stat system for Unity. The new API focuses on individual `Stat` objects that work independently with their own modifiers, formulas, and events.

## New Core API

### Basic Usage

```csharp
using UnityEngine;
using StatForge;

public class Player : MonoBehaviour
{
    [Stat] public Stat health = new Stat("Health", 100f);
    [Stat] public Stat mana = new Stat("Mana", 50f);
    [Stat] public Stat damage = new Stat("Damage", "health * 0.1 + strength * 2");
    
    void Start()
    {
        // Ultra-simple API usage
        health.Value = 80f;
        mana.AddModifier(new StatModifier(ModifierType.Additive, 25f, 5f));
        Debug.Log($"Damage: {damage.Value}"); // Auto-calculated via formula
    }
}
```

### Key Features

#### Individual Stat Objects
- Each `Stat` is completely independent
- No dependency on global ScriptableObjects
- Self-contained with modifiers, formulas, and events
- Implicit conversion to float for easy usage

#### Formula System
```csharp
// Simple formulas
var damage = new Stat("Damage", "strength + dexterity * 0.5");

// Complex formulas with functions
var critChance = new Stat("CritChance", "min(luck * 0.02, 0.95)");

// Percentage references
var healthRegen = new Stat("HealthRegen", "5% maxHealth + constitution");
```

#### Modifier System
```csharp
// Temporary modifiers
health.AddTemporaryBonus(25f, 5f); // +25 for 5 seconds
damage.AddTemporaryMultiplier(1.5f, 3f); // x1.5 for 3 seconds

// Permanent modifiers
var permanentBonus = StatModifier.Additive(10f);
health.AddModifier(permanentBonus);

// Remove modifiers
health.RemoveModifier(permanentBonus);
```

#### Event System
```csharp
// Individual stat events
health.OnValueChanged += (oldValue, newValue) => 
{
    Debug.Log($"Health: {oldValue} -> {newValue}");
    UpdateHealthBar(newValue / maxHealth.Value);
};

// Global events
StatEvents.OnStatChanged += (owner, statName, oldValue, newValue) => 
{
    Debug.Log($"{owner.name}.{statName}: {oldValue} -> {newValue}");
};
```

## Architecture

### Core Classes

#### `Stat` Class
The centerpiece of the new API. Represents an individual stat with:
- Base value and calculated value
- Optional formula for derived stats
- Individual modifier collection
- Min/max value constraints
- Event system
- Implicit float conversion

#### `StatModifier` Interface and Implementation
Enhanced modifier system with:
- Additive, Multiplicative, and Override types
- Temporary modifiers with duration
- Priority-based application order
- Auto-disposal when expired

#### `IndividualStatFormulaEvaluator`
Advanced formula evaluation engine supporting:
- Mathematical operations (+, -, *, /, parentheses)
- Stat references by name
- Percentage calculations (e.g., "50% health")
- Math functions (min, max, abs, floor, ceil, round, sqrt, sin, cos, tan)
- Circular reference detection

#### `StatDefinition` (Optional)
ScriptableObject for defining stat templates:
- Standardized configurations
- Preset formulas and values
- Display formatting
- Dependency tracking

### Extension Methods

The `StatExtensions` class provides backward compatibility and integration:

```csharp
// GameObject integration
gameObject.SetStat("health", 100f);
float health = gameObject.GetStat("health");

// Modifier management
gameObject.AddStatModifier("health", modifier);
gameObject.RemoveStatModifier("health", modifierId);

// Stat object access
Stat healthStat = gameObject.GetStatObject("health");
var allStats = gameObject.GetAllStatObjects();
```

## Editor Tools

### StatForge Window
Advanced editor window accessible via `Tools > StatForge > StatForge Window`:

#### Stat Editor Tab
- Create and configure individual stats
- Formula validation
- Real-time value preview
- Modifier testing

#### Formula Tester Tab
- Test formulas with custom values
- Real-time evaluation
- Syntax help and examples
- Stat reference extraction

#### Debug Console Tab
- Real-time stat change monitoring
- Event tracking during gameplay
- Performance metrics

#### Settings Tab
- Configure debugging options
- Performance settings
- Editor behavior customization

### Property Drawer
Custom property drawer for `Stat` fields:
- Clean inspector display
- Formula editing
- Min/max value configuration
- Runtime value monitoring

## Performance Optimizations

### Caching System
- Intelligent value caching with dirty flagging
- Formula result caching
- Automatic cache invalidation

### Lazy Evaluation
- Values calculated only when requested
- Formula evaluation on-demand
- Minimal CPU overhead for unused stats

### Memory Management
- Object pooling for modifiers
- Automatic cleanup on GameObject destruction
- Efficient event subscription management

## Backward Compatibility

The new system maintains 100% backward compatibility with the existing API:

```csharp
// Legacy API still works
[Stat] public float health = 100f;
[DerivedStat("level * 10")] public float damage;

// Can be mixed with new API
[Stat] public Stat mana = new Stat("Mana", 50f);
```

## Migration Guide

### From Legacy to New API

1. **Replace primitive stat fields:**
   ```csharp
   // Old
   [Stat] public float health = 100f;
   
   // New
   [Stat] public Stat health = new Stat("Health", 100f);
   ```

2. **Convert derived stats:**
   ```csharp
   // Old
   [DerivedStat("level * 10")] public float maxHealth;
   
   // New
   [Stat] public Stat maxHealth = new Stat("MaxHealth", "level * 10");
   ```

3. **Update modifier usage:**
   ```csharp
   // Old
   this.AddStatModifier("health", StatModifier.Additive(25f));
   
   // New (both work, but direct is simpler)
   health.AddTemporaryBonus(25f);
   ```

## Advanced Features

### Custom Operators
Formulas support advanced mathematical operations:
```csharp
var complexStat = new Stat("Complex", "min(max(strength * 2, 10), 100) + floor(level / 5)");
```

### Stat Templates
Use StatDefinition assets for standardized configurations:
```csharp
var healthDef = StatDefinition.FindByName("StandardHealth");
var health = healthDef.CreateStat();
```

### Performance Profiling
Monitor stat system performance:
```csharp
// Enable in StatForge Window > Settings
showPerformanceMetrics = true;
```

### Circular Reference Detection
Automatic detection and prevention of circular formula dependencies:
```csharp
// This will be detected and handled gracefully
var stat1 = new Stat("A", "B + 10");
var stat2 = new Stat("B", "A + 5"); // Circular reference
```

## Examples

### RPG Character Stats
```csharp
public class RPGCharacter : MonoBehaviour
{
    [Header("Primary Stats")]
    [Stat] public Stat strength = new Stat("Strength", 10f);
    [Stat] public Stat agility = new Stat("Agility", 10f);
    [Stat] public Stat intelligence = new Stat("Intelligence", 10f);
    [Stat] public Stat level = new Stat("Level", 1f);
    
    [Header("Derived Stats")]
    [Stat] public Stat health = new Stat("Health", "strength * 10 + level * 5");
    [Stat] public Stat mana = new Stat("Mana", "intelligence * 8 + level * 3");
    [Stat] public Stat damage = new Stat("Damage", "strength * 2 + level");
    [Stat] public Stat critChance = new Stat("CritChance", "min(agility * 0.01, 0.5)");
    
    void Start()
    {
        this.InitializeStats();
        
        // Level up mechanics
        level.OnValueChanged += OnLevelUp;
    }
    
    private void OnLevelUp(float oldLevel, float newLevel)
    {
        Debug.Log($"Level up! {oldLevel} -> {newLevel}");
        // All derived stats update automatically
    }
}
```

### Equipment System
```csharp
public class Equipment : MonoBehaviour
{
    [Stat] public Stat attackPower = new Stat("AttackPower", 0f);
    [Stat] public Stat defense = new Stat("Defense", 0f);
    
    public void EquipItem(Item item)
    {
        // Add equipment bonuses as permanent modifiers
        if (item.attackBonus > 0)
        {
            attackPower.AddModifier(StatModifier.Additive(item.attackBonus, -1f, 0, item.name));
        }
        
        if (item.defenseBonus > 0)
        {
            defense.AddModifier(StatModifier.Additive(item.defenseBonus, -1f, 0, item.name));
        }
    }
    
    public void UnequipItem(Item item)
    {
        // Remove equipment bonuses
        attackPower.RemoveModifier(item.name);
        defense.RemoveModifier(item.name);
    }
}
```

### Buff/Debuff System
```csharp
public class BuffSystem : MonoBehaviour
{
    public void ApplyBuff(string statName, float amount, float duration)
    {
        var stat = this.GetStatObject(statName);
        if (stat != null)
        {
            stat.AddTemporaryBonus(amount, duration);
        }
    }
    
    public void ApplyDebuff(string statName, float multiplier, float duration)
    {
        var stat = this.GetStatObject(statName);
        if (stat != null)
        {
            stat.AddTemporaryMultiplier(multiplier, duration);
        }
    }
}
```

## Best Practices

1. **Use meaningful stat names** that match your formulas
2. **Initialize stats early** with `this.InitializeStats()`
3. **Subscribe to events** for UI updates and game logic
4. **Use temporary modifiers** for time-limited effects
5. **Validate formulas** in the StatForge Window before use
6. **Monitor performance** with the debug console during development
7. **Create StatDefinitions** for commonly used stat configurations

## Troubleshooting

### Common Issues

**Formula not evaluating correctly:**
- Check stat name references match exactly
- Validate formula syntax in StatForge Window
- Ensure referenced stats are initialized

**Events not firing:**
- Call `InitializeStats()` on GameObject
- Check event subscription timing
- Verify stat ownership assignment

**Performance issues:**
- Enable performance monitoring in StatForge Window
- Check for excessive formula complexity
- Consider caching for frequently accessed stats

**Circular references:**
- Review formula dependencies
- Use the Debug Console to track evaluation chains
- Consider splitting complex formulas

## Support

For additional support and examples:
- Check the Samples~ folder for complete examples
- Use the StatForge Window for formula testing
- Review the unit tests in Tests/Runtime
- Visit the GitHub repository for updates and community support