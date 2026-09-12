# GEMINI.md — Project Guide & Technical Reference

This document serves as the primary technical context and instructions for Gemini and AI pair-programming agents operating within the `werewolves-godot` codebase.

---

## 1. Project Overview & Tech Stack

- **Game**: *Werewolves* — 2D Top-down Action RPG / Survival Exploration.
- **Engine**: **Godot Engine 4.7-dev** (.NET / Mono Edition).
- **Runtime / Framework**: **.NET 8.0** (`net8.0` target framework), **C# 12**.
- **Godot SDK**: `Godot.NET.Sdk/4.7.2`.
- **Renderer**: Forward Plus (Vulkan 3D/2D).
- **Target Resolution**: 1920×1080 (Window stretch mode: `canvas_items`, aspect: `expand`).
- **Autoload Singletons**:
  - `GameState` (`res://Scripts/Core/GameState.cs`).
- **Main Scene**: `res://scenes/Main.tscn`.
- **Root Namespace**: `Werewolves`.

---

## 2. Architecture & Directory Blueprint

```text
werewolves-godot/
├── Werewolves.csproj          # C# project settings, TargetFramework net8.0, Nullable enabled
├── Werewolves.sln             # Solution file
├── project.godot              # Godot project settings, input map, autoloads, window settings
├── scenes/
│   ├── Main.tscn              # Root scene: WorldManager, Entities/Werewolf, HUD (CanvasLayer)
│   ├── Entities/              # Packed scenes for Werewolf, Deer, TreeObject, RockObject, HouseObject
│   └── UI/                    # Packed scenes for ActionBar, PouchWindow, StatsPanel, TargetPanel
├── Scripts/
│   ├── Main.cs                # Entry point bootstrap; dynamic node attachment fallback
│   ├── Core/                  # Domain models, formulas, persistence, and global state
│   │   ├── GameState.cs       # Autoload singleton: player stats, cooldowns, events, inventory
│   │   ├── Formulas.cs        # Combat formulas (damage, hit chance, power gains)
│   │   ├── SaveManager.cs     # JSON persistence to user://werewolves_save.json
│   │   ├── ICombatant.cs      # Interface for combat participants (damageable, stats)
│   │   └── PouchItemData.cs   # Data model for inventory items and UI coordinates
│   ├── Entities/              # Game actors and environmental interactive bodies
│   │   ├── Werewolf.cs        # Player CharacterBody2D: movement, animation states, combat, skills
│   │   ├── Deer.cs            # Prey/monster CharacterBody2D: wandering, aggro AI, loot drop
│   │   ├── TreeObject.cs      # StaticBody2D: 15% selectable, chop interaction, log drop
│   │   ├── RockObject.cs      # StaticBody2D: quarry interaction, stone drop
│   │   ├── HouseObject.cs     # StaticBody2D: village cottages with custom collision boxes
│   │   ├── DroppedLoot.cs     # Area2D: collectible pickups with custom hover cursors
│   │   └── ISelectableTarget.cs # Interface for entities that can be clicked and targeted
│   ├── UI/                    # CanvasLayer and HUD components
│   │   ├── HUDManager.cs      # Top-level UI manager: viewport resize listeners, layout anchors
│   │   ├── OrbGauge.cs        # Diablo-style liquid orb (procedural _Draw polygon + bubbles)
│   │   ├── StatsPanel.cs      # 4 skill hotkey slots with cooldown sweeps and timers
│   │   ├── TargetPanel.cs     # Selected target health bar + contextual interaction button
│   │   ├── PouchWindow.cs     # Freeform draggable inventory modal with drag-and-drop items
│   │   └── ActionBar.cs       # Mini action bar holding the pouch toggle button
│   └── Effects/               # Transient combat feedback and visual FX
│       ├── BuffAura.cs        # CpuParticles2D violet glowing aura for Howl buff
│       ├── DamageNumber.cs    # Floating text drift and fade effect
│       └── ScratchEffect.cs   # 4-frame swipe animation
└── assets/                    # Textures, spritesheets, fonts, cursors
    ├── cursors/               # normal_o.png, interaction_o.png, grab_o.png
    ├── werewolf/optimized/    # w_moving.png, w_idle_states.png, w_attacks.png, w_magic_attacks.png
    ├── houses/                # house_1..4.png, simple_path_cross_prim.png, lantern_light.png
    └── icons/                 # scratch_hit_icon.png, charge_attack.png, bite.png, blood_howling.png
```

---

## 3. Core Subsystems & Design Patterns

### 3.1 Singleton & Event-Driven State (`GameState.cs`)
- `GameState.Instance` provides global access. If not yet added by Godot autoload, it attaches deferred to the root scene.
- **Strongly-typed C# events** (`System.Action`) are preferred over Godot string signals for performance and type safety:
  - `OnHealthChanged(float current, float max)`
  - `OnPowerChanged(float current, float max)`
  - `OnPositionChanged(Vector2 position)`
  - `OnPouchChanged()`
  - `OnTargetChanged(Node2D? target)`
  - `OnCooldownUpdated(int index, float remaining, float total)`
  - `OnPouchToggled(bool isOpen)`
  - `OnSpawnDamageNumber(string text, Vector2 position, Color color)`

### 3.2 Dual-Mode Node Initialization
All scripts are designed to work under two scenarios:
1. **Instantiated via Scene (`.tscn`)**: Nodes use `GetNodeOrNull<T>()` or `[Export]` bindings.
2. **Instantiated via Code (`new T()`)**: If child nodes (`Sprite2D`, `CollisionShape2D`, containers) do not exist in the tree, the script programmatically constructs and configures them in `_Ready()`.

### 3.3 Combat & Formulas (`Formulas.cs`)
- **Hit Calculation**:
  $$\text{Hit Chance} = \text{Accuracy} - \text{Evasion}$$
  Rolled via `GD.Randf() < hitChance`.
- **Damage Calculations**:
  - Regular hit: $\max(0, \text{Damage} - \frac{\text{Defense}}{2})$
  - Scratch Hit: $\max(0, \text{Damage} - \frac{\text{Defense}}{2} + 12)$
  - Charge Attack: $\max(0, \text{Damage} - \frac{\text{Defense}}{2} + 20)$
  - Execute Bite: $\max(0, \text{Damage} - \frac{\text{Defense}}{2} + 15)$ *(Target must be $\le 25\%$ HP)*
- **Power Gain**:
  Successful auto-attacks generate dynamic power:
  $$\text{Power} = 12.5 \times (\text{rand}(0, 4) + 1)$$

### 3.4 Open World Map & Persistent Landmarks (`WorldManager.cs`)
- **World Bounds**: 10,000 × 10,000 units (`WorldRadius = 5000f`), surrounded by perimeter collision walls and dense border trees.
- **Camera2D Tracking**: Smooth camera attached to the `Werewolf` player (`PositionSmoothingEnabled = true`, limits clamped to world boundaries).
- **Dynamic Terrain Tiling**: Forest ground texture (`pine_tree_forest_ground_1.png`) is dynamically snapped to 350px tile intervals centered on the player/camera for seamless infinite scrolling.
- **Coordinate System & Map Coordinates**:
  - Internal world coordinates use Godot 2D (`+X` Right, `+Y` Down).
  - Map / HUD coordinates use standard Cartesian coordinates (`WorldManager.ToMapCoordinates`): `+X` East/Right, `+Y` North/Up (inverting Godot's vertical axis).
- **Points of Interest (Landmarks)**:
  - **Awakening Grove** at Map `(0, 0)` [World `(0, 0)`]: Central clearing where the player awakens.
  - **The Village** at Map `(2500, 1800)` [World `(2500, -1800)`]: 8 cottages arranged in a circle around the central lantern with cobblestone pathing (North-East).
  - **Silent Lake** at Map `(-2000, -2000)` [World `(-2000, 2000)`]: Dynamic lake with water body collision (South-West).
  - **Misty Lake** at Map `(-2200, 2200)` [World `(-2200, -2200)`]: Second natural lake formation (North-West).
  - **Quarry Hills** at Map `(2200, -2200)` [World `(2200, 2200)`]: Dense cluster of minable boulders (`RockObject`) (South-East).
  - **Hunting Grounds**: Open clearings at Map `(0, 2400)`, `(-2400, 0)`, etc. with roaming herds of deer (`Deer`).
  - **The Deep Wilderness**: ~450 pine trees (15% interactive `TreeObject`) and scattered rocks.

### 3.5 Custom UI Rendering (`OrbGauge.cs` & `PouchWindow.cs`)
- **`OrbGauge`**:
  - Inherits `Control`. Uses `_Draw()` with trigonometric chord calculations to render liquid fill polygons.
  - Maintains an internal pool of rising `Bubble` structs simulated in `_Process`.
  - Blits decorative ring frame texture over the orb.
- **`PouchWindow`**:
  - Window dragging implemented via `GuiInput` and global mouse delta, clamped to viewport rect.
  - Items are freeform controls within `_itemsArea`. Dragging items updates their local position and serializes `(PosX, PosY)` into `SaveManager`.

### 3.6 State Persistence (`SaveManager.cs`)
- Serializes `SaveData` to `user://werewolves_save.json` using `System.Text.Json`.
- Automatically invoked on:
  - Position changes / movement (`GameState.SetPlayerPosition` / `LoadedPlayerPosition`)
  - Item collection (`GameState.AddPouchItem`)
  - Item repositioning in pouch (`GameState.UpdatePouchItemPosition`)

---

## 4. C# Coding Conventions & Standards

### 4.1 Syntax & Formatting
- **File-Scoped Namespaces**: Always use `namespace Werewolves.Category;`.
- **Naming Conventions**:
  - `PascalCase` for classes, structs, interfaces, methods, properties, public events.
  - `_camelCase` for private fields (e.g., `private Sprite2D _sprite = null!;`).
  - `camelCase` for method arguments and local variables.
  - Interfaces must be prefixed with `I` (e.g., `ICombatant`, `ISelectableTarget`).
- **Nullability**: Nullable reference types are enabled (`<Nullable>enable</Nullable>`). Use `?` for optional references and `= null!;` for members initialized in `_Ready()`.

### 4.2 Godot Lifecycle Guidelines
- **`_EnterTree()`**: Singleton instance assignment or low-level tree hooks.
- **`_Ready()`**: Node lookups, resource preloading (`GD.Load<Texture2D>`), signal connections, and child instantiation fallbacks.
- **`_Process(double delta)`**: Visual timers, UI animations, procedural gauge updates.
- **`_PhysicsProcess(double delta)`**: Player movement, velocity calculations, collision handling (`MoveAndSlide()`), boundary wrapping.
- **`_Draw()`**: Custom CanvasItem graphics (selection brackets, liquid orbs). Always call `QueueRedraw()` when state changes.

### 4.3 Node Lifecycle & Memory Safety
- Always call `QueueFree()` to remove nodes from the tree.
- When subscribing to global events in long-lived singletons (`GameState.Instance`), ensure objects that can be freed unsubscribe or do not cause dangling handler leaks.

---

## 5. Input Mapping Reference (`project.godot`)

All input actions are configured in `project.godot`:

| Action Name | Default Key Binding | Description |
| :--- | :--- | :--- |
| `move_up` | <kbd>W</kbd> | Move upward |
| `move_down` | <kbd>S</kbd> | Move downward |
| `move_left` | <kbd>A</kbd> | Move left |
| `move_right` | <kbd>D</kbd> | Move right |
| `sprint` | <kbd>Shift</kbd> | Sprint multiplier (1.6×) |
| `skill_1` | <kbd>1</kbd> | Scratch Hit |
| `skill_2` | <kbd>2</kbd> | Charge Attack |
| `skill_3` | <kbd>3</kbd> | Execute Bite |
| `skill_4` | <kbd>4</kbd> | Blood Howling |
| `toggle_pouch` | <kbd>P</kbd> | Toggle Inventory Pouch |

When querying inputs in code:
```csharp
Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
bool isSprinting = Input.IsActionPressed("sprint");
if (Input.IsActionJustPressed("skill_1")) { ... }
```

---

## 6. Build, Test & Development Workflow

### 6.1 Building the C# Solution
```bash
dotnet build Werewolves.sln
```
*Note on Sandboxed / Offline Environments:*
In environments without direct internet access to `api.nuget.org`, NuGet will rely on local NuGet package caches (`~/.nuget/packages`). The project targets `.NET 8.0` with `Godot.NET.Sdk/4.7.2`.

### 6.2 Running the Project
```bash
# Direct run via Godot Mono binary
godot-mono --path .

# Or headless validation
godot-mono --headless --path . --quit-after 50
```

---

## 7. Critical Pitfalls & Rules for Agents

1. **Preserve Scene Hierarchy & Y-Sort**:
   All entities requiring depth sorting must be children of `WorldManager/Entities` (which has `YSortEnabled = true`).
2. **Sprite Footprint Offsets**:
   Always set the sprite's `Offset` so that the base of the object/feet of the character sits at `(0, 0)`. This ensures proper Y-sorting and collision alignment.
3. **Cursor Management**:
   Whenever setting a custom cursor on `MouseEntered` (`interaction_o.png` or `grab_o.png`), ensure `MouseExited` restores `normal_o.png`. If an object is collected or freed while hovered, reset the cursor to normal.
4. **Input Event Handling**:
   When consuming an input inside a UI control (such as dragging pouch items), call `GetViewport().SetInputAsHandled()` to prevent clicks from propagating to the game world.
5. **No Blind Deletion of `.uid` Files**:
   Godot 4 uses `.uid` files to track unique resource identifiers. Never remove or alter `.uid` files unless explicitly re-importing assets.
