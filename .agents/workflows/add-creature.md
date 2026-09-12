# Workflow: Adding a New Creature or Enemy

This playbook outlines the procedure for adding new wildlife, beasts, or hostile human NPCs into the game world.

---

## 📋 Checklist

- [ ] **Step 1**: Create Entity Script in `Scripts/Entities/<CreatureName>.cs`
- [ ] **Step 2**: Implement `ICombatant` & `ISelectableTarget` Contracts
- [ ] **Step 3**: Configure Sprite Frames, Collision & Y-Sort Footprint
- [ ] **Step 4**: Implement AI Behavior (Wander, Aggro, Attack, Death)
- [ ] **Step 5**: Wire Selection Brackets & Contextual Cursors
- [ ] **Step 6**: Add Dropped Loot on Death
- [ ] **Step 7**: Register in `WorldManager.cs` Spawning Loop
- [ ] **Step 8**: Create Packed Scene (Optional) & Build Verification

---

## Detailed Steps

### Step 1 & 2: Entity Script & Interface Implementation
Create `Scripts/Entities/Boar.cs` (example):
```csharp
using System;
using Godot;
using Werewolves.Core;
using Werewolves.Effects;

namespace Werewolves.Entities;

public partial class Boar : CharacterBody2D, ICombatant, ISelectableTarget
{
    public string TargetName => "Boar";
    public float BaseDamage { get; set; } = 18f;
    public float Accuracy { get; set; } = 0.75f;
    public float BaseDefense { get; set; } = 8f;
    public float Evasion { get; set; } = 0.1f;
    public float Health { get; set; } = 120f;
    public float MaxHealth => 120f;
    public bool IsDead => Health <= 0f;

    public Werewolf? TargetWerewolf { get; set; }
    private bool _isSelected = false;
    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    ...
```

### Step 3: Dual-Mode Setup & Offsets
In `_Ready()`:
```csharp
_sprite = GetNodeOrNull<Sprite2D>("Sprite2D") ?? new Sprite2D { Name = "Sprite2D" };
_sprite.Texture = GD.Load<Texture2D>("res://assets/boar.png");
_sprite.Scale = new Vector2(0.5f, 0.5f);
// Offset vertically so character base is aligned with (0, 0)
_sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.4f);

_collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D") ?? new CollisionShape2D { Name = "CollisionShape2D" };
_collision.Shape = new CircleShape2D { Radius = 20f };
_collision.Position = new Vector2(0, -5);

InputPickable = true;
MouseEntered += OnMouseEntered;
MouseExited += OnMouseExited;
```

### Step 4: AI State Machine in `_PhysicsProcess`
- **Passive Wandering**: Random directional vector picked every 2–5s.
- **Aggro Pursuit**: If attacked (`_isAggro == true`), navigate toward `TargetWerewolf.GlobalPosition`.
- **Melee Attack**: When within attack range, roll `Formulas.IsHitSuccessful(this, TargetWerewolf)` and apply damage.

### Step 5: Selection Brackets & Hover Cursors
```csharp
private void OnMouseEntered()
{
    if (!IsDead)
    {
        var cursor = GD.Load<Resource>("res://assets/cursors/interaction_o.png");
        Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow);
    }
}

public override void _Draw()
{
    if (_isSelected && !IsDead)
    {
        // Draw 4 corner yellow brackets around creature bounds
    }
}
```

### Step 6: Loot Drop on Death
When `Health <= 0`:
```csharp
var loot = DroppedLoot.Instantiate("Meat", GlobalPosition);
GetParent()?.AddChild(loot);
if (GameState.Instance.SelectedTarget == this)
{
    GameState.Instance.SelectedTarget = null;
}
QueueFree();
```

### Step 7: Spawn in `WorldManager.cs`
In `WorldManager.cs` (e.g. inside `BuildHuntingGrounds()` or a dedicated landmark/spawn helper called from `GenerateOpenWorld()`):
```csharp
// Example: Spawning creatures around a landmark or wilderness clearing
for (int i = 0; i < count; i++)
{
    Vector2 offset = new Vector2((float)GD.RandRange(-250, 250), (float)GD.RandRange(-250, 250));
    var boar = new Boar { GlobalPosition = landmarkCenter + offset, TargetWerewolf = Player };
    _entitiesContainer.AddChild(boar);
}
```

### Step 8: Build Verification
```bash
dotnet build Werewolves.sln
```
