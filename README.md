<div align="center">
  <h1>StatForge</h1>
  <p><strong>A powerful, flexible, and editor-driven stat system for Unity.</strong></p>
  <p>
    <a href="https://github.com/Natteens/StatForge/releases"><img src="https://img.shields.io/badge/version-0.3.2-blue.svg" alt="Version"></a>
    <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.0.54f1-lightgrey.svg" alt="Unity"></a>
  </p>
</div>

---

**StatForge** is engineered to transform stat management from a tedious coding task into an intuitive and creative process. At its core, StatForge is built around a powerful editor window that allows developers and designers to create, manage, and test complex stat interactions without ever leaving Unity.

Whether you're building a deep RPG, a fast-paced action game, or a complex simulation, StatForge provides the tools you need to build a robust, performant, and easily maintainable stat system.

## ✨ Key Features

-   🎨 **Centralized Editor Hub**: A dedicated editor window (**Tools > StatForge**) to manage your entire stat ecosystem. Say goodbye to scattered ScriptableObjects.
-   🧮 **Dynamic Formula Engine**: Define stats that are dynamically calculated from others (e.g., `MaxHealth = CON * 10 + LVL * 5`). The system automatically tracks dependencies and recalculates values when a base stat changes.
-   🔄 **Comprehensive Modifier System**: Implement buffs, debuffs, equipment bonuses, and status effects with ease. Modifiers can be temporary, permanent, or conditional and support various calculation types.
-   📦 **Reusable Stat Templates**: Use `StatContainerAsset` to create templates for character classes, enemy types, or equipment sets. This promotes a clean, data-driven architecture.
-   ⚡ **Performance-First Architecture**: Designed with high performance in mind, featuring lazy evaluation, intelligent caching, and minimal runtime allocations.
-   🔍 **Integrated Testing & Debugging**: A dedicated "Testing" tab in the editor window allows you to test formulas and inspect runtime stat values on active GameObjects in real-time.
-   🔗 **Automatic Inspector Integration**: A custom property drawer for the `Stat` class provides a clean and informative view directly in the Inspector, showing runtime values and modifier counts.

## ⚙️ Core Concepts

Understanding these four components is key to mastering StatForge.

1.  **`StatType` (The Blueprint)**
    -   A `ScriptableObject` that defines the fundamental properties of a stat: its name, short name (for formulas), default value, and any formulas it uses.
    -   *Example*: A `StatType` for "Strength" with the short name "STR" and a default value of 5.

2.  **`Stat` (The Instance)**
    -   A plain C# class that represents an actual stat on a character or object. It holds the `baseValue` and a list of active `IStatModifier`s.
    -   *Example*: A player's `strengthStat`, which is an instance of the "Strength" `StatType`, with a `baseValue` that can be increased on level up.

3.  **`IStatModifier` (The Change)**
    -   An interface for anything that temporarily or permanently alters a `Stat`'s value.
    -   *Example*: A "Potion of Strength" adds a temporary `Additive` modifier of `+10` to the player's `strengthStat`.

4.  **`StatContainer` & `StatContainerAsset` (The Collection)**
    -   `StatContainerAsset` is a `ScriptableObject` template that defines a collection of `StatType`s (e.g., "Player Stats", "Goblin Stats").
    -   `StatContainer` is the runtime instance of that template, managing all the `Stat` instances for a specific character.

## 🚀 The Editor-First Workflow: A Step-by-Step Guide

This guide will walk you through creating a simple RPG character stat system, emphasizing the intended editor-driven workflow.

### Step 1: The StatForge Hub

First, open the main editor window from **Tools > StatForge**. This window is your command center for the entire system.

<div align="center">
  <a href="https://files.catbox.moe/ff2inu.png" target="_blank">
    <img src="https://files.catbox.moe/ff2inu.png" alt="StatForge Editor Window" width="900"/>
  </a>
  <br>
  <em>The main StatForge editor window.</em>
</div>

### Step 2: Creating `StatType` Blueprints

`StatType`s are the templates for every stat in your game. Let's create a few primary stats.

1.  In the **StatForge** window, ensure you are in the "Stats" view, and click the **"New Stat"** button.
2.  A creation panel will appear on the right. Fill it out for our `Constitution` stat:
    -   **Display Name**: `Constitution`
    -   **Short Name**: `CON` (**Important:** This is used in formulas!)
    -   **Category**: `Primary Stats`
    -   **Default Value**: `5`
3.  Click **"Create Stat"**. The asset will be saved to your project.
4.  Repeat this process for `Strength` (STR) and `Level` (LVL).

<div align="center">
  <a href="https://files.catbox.moe/4trk4v.png" target="_blank">
    <img src="https://files.catbox.moe/4trk4v.png" alt="StatType Inspector" width="500"/>
  </a>
  <br>
  <em>The Inspector for the `Constitution` StatType.</em>
</div>

### Step 3: Harnessing the Power of Formulas

Now, let's create a derived stat, `Max Health`, that uses a formula.

1.  Create another `StatType` named `Max Health` with the short name `MHP`.
2.  In the **Formula** field, enter the logic for its calculation. Use the short names of the stats you created earlier.
    -   **Formula**: `CON * 10 + LVL * 5`
3.  Click **"Create Stat"**. StatForge now understands that `MHP` depends on `CON` and `LVL` and will update it automatically whenever they change.

> **💡 Best Practice:** Use derived stats like `MaxHealth`, `CritChance`, or `AttackDamage` for values that are calculated. Keep a separate, simple `float` variable like `currentHealth` in your scripts for mutable values.

### Step 4: Building `StatContainerAsset` Templates

A `StatContainerAsset` is a reusable template that bundles `StatType`s. This is perfect for defining the stat blocks for different kinds of characters or items.

1.  In the StatForge window, switch to the **"Containers"** view.
2.  Click **"New Container"**.
3.  Name it `Player Character Template`.
4.  From the list, select all the stats a player character should have: `Constitution`, `Strength`, `Level`, and `Max Health`.
5.  Click **"Create Container"**. You now have a reusable asset for any player character.

### Step 5: Bringing Stats to Life in Code

With our assets defined, using them in a script is straightforward.

1.  Create a `Player.cs` script.
2.  Add a field to hold the `StatContainerAsset` we just created.
3.  In `Start()`, create a runtime instance of the container and get references to the stats you'll need to access frequently.

```csharp
// Player.cs
using UnityEngine;
using StatForge;

public class Player : MonoBehaviour
{
    [Header("Stat Configuration")]
    [SerializeField] private StatContainerAsset playerStatsTemplate;

    // The runtime container holding all our stat instances.
    private StatContainer stats;

    // A simple float for the character's current health.
    private float currentHealth;

    // --- Cached Stat References for easy access ---
    private Stat maxHealthStat;
    private Stat constitutionStat;
    private Stat levelStat;

    void Start()
    {
        // Create a runtime instance of the stat container from our template.
        stats = playerStatsTemplate.CreateRuntimeContainer();
        
        // Get references to stats we'll use often (using Short Names is recommended).
        maxHealthStat = stats.GetStat("MHP");
        constitutionStat = stats.GetStat("CON");
        levelStat = stats.GetStat("LVL");

        // Set the initial health based on the calculated Max Health.
        currentHealth = maxHealthStat.Value;
        Debug.Log($"Player initialized. Max Health: {maxHealthStat.Value}");

        // Subscribe to events for reactive game logic.
        maxHealthStat.OnValueChanged += OnMaxHealthChanged;
    }

    public void LevelUp()
    {
        // To level up, we just modify the BaseValue of the Level stat.
        levelStat.BaseValue++;
        
        // The MaxHealth stat will automatically update because its formula depends on LVL.
        // The OnMaxHealthChanged event will handle healing the player.
        Debug.Log($"Leveled up to {levelStat.Value}! New Max Health is: {maxHealthStat.Value}");
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"Took {amount} damage. Current health: {currentHealth} / {maxHealthStat.Value}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void OnMaxHealthChanged(Stat stat, float oldValue, float newValue)
    {
        // When max health changes (e.g., from a buff or level up),
        // heal the player for the amount increased.
        float healthIncrease = newValue - oldValue;
        if (healthIncrease > 0)
        {
            currentHealth += healthIncrease;
        }
        // Ensure current health doesn't exceed the new max.
        currentHealth = Mathf.Min(currentHealth, newValue);
        
        Debug.Log($"Max health changed to {newValue}. Current health is now {currentHealth}.");
        // Here you would update the player's health bar UI.
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        // Handle death logic here...
    }
}
```

When you attach this script to a GameObject and assign the `Player Character Template`, the custom property drawer will give you a clean, informative view in the Inspector.

<div align="center">
  <a href="https://files.catbox.moe/9hsx9o.png" target="_blank">
    <img src="https://files.catbox.moe/9hsx9o.png" alt="Stat Drawer in Inspector" width="500"/>
  </a>
  <br>
  <em>The custom Stat drawer in the Inspector, showing runtime info.</em>
</div>

## 🛠️ Advanced Topics & Techniques

### In-Depth with Modifiers

Modifiers are the heart of dynamic gameplay changes. They can come from buffs, equipment, or environmental effects.

```csharp
public class EquipmentManager
{
    private StatContainer targetStats;
    private const string BERSERKER_HELM_SOURCE = "BerserkerHelm";

    public EquipmentManager(StatContainer stats)
    {
        this.targetStats = stats;
    }

    public void EquipBerserkerHelm()
    {
        // This helmet increases Strength but lowers Defense.
        // We use a source tag to easily remove it later.
        
        targetStats.GetStat("STR").AddModifier(
            value: 15f,
            type: ModifierType.Additive,
            source: BERSERKER_HELM_SOURCE
        );
        
        targetStats.GetStat("DEF").AddModifier(
            value: -10f,
            type: ModifierType.Subtractive,
            source: BERSERKER_HELM_SOURCE
        );
    }
    
    public void UnequipBerserkerHelm()
    {
        // Remove all modifiers from all stats that came from this specific item.
        foreach (var stat in targetStats.Stats)
        {
            stat.RemoveModifiersBySource(BERSERKER_HELM_SOURCE);
        }
    }
}
```

### Runtime Debugging & Testing Tools

StatForge includes a powerful **Testing** tab to help you debug and balance your game.

1.  **Formula Tester**: Test complex formulas on the fly without needing to enter Play Mode. It automatically loads the default values from all your `StatType`s.
2.  **Runtime Entities**: While in Play Mode, this section lists all GameObjects in your scene that have `Stat` fields. You can inspect their live values, which update in real-time.

<div align="center">
  <a href="https://files.catbox.moe/45cf8g.png" target="_blank">
    <img src="https://files.catbox.moe/45cf8g.png" alt="Testing Tools Tab" width="900"/>
  </a>
  <br>
  <em>The Testing tab, showing the Formula Tester and Runtime Entities inspector.</em>
</div>

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.