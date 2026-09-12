# Role: Gameplay Engineer

**Domain**: Player mechanics, combat formulas, entity behaviors, animations, and game state.

---

## 🎯 Objectives & Responsibilities

1. **Character Controller (`Werewolf.cs`)**:
   - Maintain 8-directional or 4-directional WASD movement with smooth `Velocity` interpolation and `MoveAndSlide()`.
   - Control sprint speed multiplier (default 1.6×) and frame timer adjustments.
   - Synchronize spritesheet rows/frames for idle variants, run cycles, attack strikes, and magic invocations.
   - Handle screen edge boundaries and fire `OnExitedScreenEdge`.

2. **Combat Systems & Math (`Formulas.cs` & `GameState.cs`)**:
   - Maintain `ICombatant` contract: `BaseDamage`, `Accuracy`, `BaseDefense`, `Evasion`, `Health`, `MaxHealth`, `IsDead`, `TakeDamage()`.
   - Ensure all damage calculations go through `Formulas.cs`:
     - Regular attacks: $\max(0, \text{Damage} - \frac{\text{Defense}}{2})$.
     - Bonus damage per skill (+12 Scratch, +20 Charge, +15 Bite).
     - Hit probability: $\text{GD.Randf}() < (\text{Accuracy} - \text{Evasion})$.
   - Auto-attack warmode: Melee range check (110px) on 1.0s interval.

3. **Entity AI (`Deer.cs` & Future Creatures)**:
   - State machine: Passive Wander $\leftrightarrow$ Pause $\leftrightarrow$ Retaliatory Aggro $\leftrightarrow$ Attack $\leftrightarrow$ Death.
   - Retaliation trigger: Enter aggro state when `TakeDamage()` is invoked, targeting the attacker.
   - Boundary collisions: Bounce or steer away from screen edges.
   - Death sequence: Stop velocity, modulate alpha to 0 over ~1 second, instantiate `DroppedLoot`, clear target from `GameState`, and call `QueueFree()`.

4. **Combat Feedback**:
   - Dispatch floating combat text via `GameState.Instance.TriggerDamageNumber(...)`.
   - Color standards:
     - Yellow `Color(1f, 1f, 0.4f)` for physical melee hits.
     - Red `Color(1f, 0.2f, 0.2f)` for skill hits.
     - Green `Color(0.2f, 1f, 0.4f)` for healing and loot.
     - Grey `Color(0.8f, 0.8f, 0.8f)` for misses or distance warnings.
     - Violet `Color(0.8f, 0.2f, 1f)` for buffs.

---

## 🛠️ Code Conventions & Patterns

### Entity Template
```csharp
namespace Werewolves.Entities;

public partial class MyCreature : CharacterBody2D, ICombatant, ISelectableTarget
{
    public string TargetName => "CreatureName";
    public float BaseDamage { get; set; } = 20f;
    public float Accuracy { get; set; } = 0.8f;
    public float BaseDefense { get; set; } = 8f;
    public float Evasion { get; set; } = 0.1f;
    public float Health { get; set; } = 120f;
    public float MaxHealth => 120f;
    public bool IsDead => Health <= 0f;

    public override void _Ready()
    {
        // Dual-mode initialization: check if sprite exists, else create
        ...
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        // AI state processing & MoveAndSlide()
    }

    public void TakeDamage(float amount, bool isSkill = false)
    {
        if (IsDead) return;
        Health -= amount;
        if (Health <= 0f) Die();
    }
}
```

---

## ⚠️ Anti-Patterns to Avoid
- **Never modify `PlayerHealth` directly**: Always use `GameState.Instance.ModifyHealth(delta)` to notify listeners and update HUD orbs.
- **Never execute combat without range checks**: Always verify `DistanceTo(target.GlobalPosition) <= MeleeRange` before applying damage.
- **Never forget Y-Sort Offset**: When setting sprite offset, compensate vertically so root position is at the creature's feet.
