# Migration Guide: StatForge Legacy to Simplified API

This guide helps you migrate from the legacy AttributeSystem-based API to the new simplified StatForge API.

## Overview

The new API maintains **full backward compatibility** while providing a much simpler alternative. You can:
- Use both APIs simultaneously
- Migrate gradually 
- Keep existing code unchanged

## API Comparison

### Old API (Still Supported)
```csharp
public class OldPlayer : MonoBehaviour
{
    public List<StatContainer> baseContainers;
    private AttributeSystem attributeSystem;
    
    void Start()
    {
        attributeSystem = GetComponent<AttributeSystem>();
        
        // Complex API:
        float health = attributeSystem.GetStatValue(healthStatType);
        attributeSystem.AddTemporaryBonus(healthStatType, 25f);
    }
}
```

### New API (Recommended)
```csharp
public class NewPlayer : MonoBehaviour
{
    [Stat] public float health = 100f;
    [Stat] public int level = 1;
    
    void Start()
    {
        // Simple API:
        float health = this.GetStat("health");
        this.AddStatModifier("health", StatModifier.Additive(25f, 5f));
    }
}
```

## Migration Strategies

### Strategy 1: Full Migration (Recommended for New Projects)
1. Replace AttributeSystem components with [Stat] attributes
2. Update all GetStatValue() calls to GetStat()
3. Replace AddTemporaryBonus() with AddStatModifier()
4. Use StatEvents instead of custom event handling

### Strategy 2: Gradual Migration (Recommended for Existing Projects)
1. Keep existing AttributeSystem code
2. Add new [Stat] fields for new features
3. Migrate components one by one
4. Both systems work side by side

### Strategy 3: Hybrid Approach
1. Keep complex stats in StatContainers
2. Use [Stat] attributes for simple stats
3. Leverage both systems' strengths

## Step-by-Step Migration

### Step 1: Add [Stat] Attributes
```csharp
// Old way
public StatContainer playerStats;

// New way (add this)
[Stat] public float health = 100f;
[Stat] public int level = 1;
[DerivedStat("level * 10")] public float maxHealth;
```

### Step 2: Update Stat Access
```csharp
// Old way
float health = attributeSystem.GetStatValue(healthStatType);

// New way
float health = this.GetStat("health");
```

### Step 3: Update Modifiers
```csharp
// Old way
attributeSystem.AddTemporaryBonus(statType, 25f);

// New way
this.AddStatModifier("health", StatModifier.Additive(25f, 5f));
```

### Step 4: Update Events
```csharp
// Old way - manual event handling

// New way
StatEvents.OnStatChanged += (owner, statName, oldValue, newValue) => {
    if (owner == gameObject && statName == "health")
    {
        UpdateHealthBar(newValue);
    }
};
```

## Benefits of Migration

### Performance Improvements
- Automatic caching and optimization
- Reduced memory allocations
- Better event handling

### Code Simplification
- 90% less boilerplate code
- No required components
- Intuitive API

### Enhanced Features
- Auto-dispose modifiers
- Global event system
- Runtime debugging
- Modern editor interface

## Common Patterns

### Health/Mana System
```csharp
// Old way
public class OldHealthMana : MonoBehaviour
{
    public AttributeSystem attributes;
    public StatType healthType, manaType;
    
    void Heal(float amount)
    {
        var current = attributes.GetStatValue(healthType);
        // Complex modification logic...
    }
}

// New way
public class NewHealthMana : MonoBehaviour
{
    [Stat] public float health = 100f;
    [Stat] public float mana = 50f;
    
    void Heal(float amount)
    {
        this.SetStat("health", Mathf.Min(this.GetStat("health") + amount, maxHealth));
    }
}
```

### Buffs/Debuffs
```csharp
// Old way - manual tracking needed
public class OldBuffs : MonoBehaviour
{
    private List<TempBonus> activeBonuses = new List<TempBonus>();
    
    void ApplyBuff(StatType type, float value, float duration)
    {
        // Complex manual implementation...
    }
}

// New way - automatic handling
public class NewBuffs : MonoBehaviour
{
    void ApplyBuff(string statName, float value, float duration)
    {
        this.ApplyTemporaryModifier(statName, ModifierType.Additive, value, duration);
        // Auto-dispose after duration!
    }
}
```

## Editor Differences

### Old Editor
- Emoji-heavy interface
- Basic functionality
- Limited debugging

### New Editor
- Professional, clean interface
- Live stats debugging
- Enhanced workflow
- Color-coded categories

## Troubleshooting

### Issue: "GetStat() returns 0"
**Solution**: Call `this.InitializeStats()` in Start() or ensure [Stat] attributes are properly set.

### Issue: "Events not firing"
**Solution**: Make sure to initialize the stat system and check event subscription timing.

### Issue: "Modifiers not applying"
**Solution**: Verify modifier priorities and types. Use the Live Stats editor view to debug.

## Best Practices

1. **Initialize Early**: Call `InitializeStats()` in Start()
2. **Use Events**: Subscribe to StatEvents for UI updates
3. **Leverage Auto-Dispose**: Use duration-based modifiers when possible
4. **Debug with Editor**: Use the Live Stats view for runtime debugging
5. **Mix Approaches**: Use both APIs where it makes sense

## Support

The legacy API will continue to be supported. Migration is optional but recommended for:
- New projects
- Code that needs simplification
- Projects requiring advanced modifier features

Both APIs can coexist indefinitely in the same project.