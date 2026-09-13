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
├── config/                    # External JSON game balance & spawn configurations
│   ├── combat.json            # Player, skills, enemy, and harvestable stats & damage
│   ├── resources.json         # Quartz, grass, and terrain artifact spawn parameters
│   └── chests.json            # Chest spawn separation & loot drop tables
├── scenes/
│   ├── Main.tscn              # Root scene: WorldManager, Entities/Werewolf, HUD (CanvasLayer)
│   ├── Lair.tscn              # Cave hideout scene: LairManager, BloodCore, Werewolf, HUD
│   ├── Entities/              # Packed scenes: Werewolf, Deer, Villager, TreeObject, RockObject,
│   │                          # HouseObject, QuartzObject, ChestObject, GrassObject, BloodCoreObject
│   └── UI/                    # Packed scenes: ActionBar, PouchWindow, StatsPanel, TargetPanel, HeroDetailsWindow
├── Scripts/
│   ├── Main.cs                # Entry point bootstrap; dynamic node attachment fallback
│   ├── Core/                  # Domain models, formulas, persistence, and global state
│   │   ├── GameState.cs       # Autoload singleton: player stats, cooldowns, events, inventory, Blood Core
│   │   ├── ConfigManager.cs   # JSON configuration loader & typed options models
│   │   ├── CursorManager.cs   # Global cursor state manager (normal, interaction, grab)
│   │   ├── Formulas.cs        # Combat formulas (damage, hit chance, loot distributions)
│   │   ├── SaveManager.cs     # JSON persistence to user://werewolves_save.json
│   │   ├── ICombatant.cs      # Interface for combat participants (damageable, stats)
│   │   └── PouchItemData.cs   # Data model for inventory items, blood percent, and UI coordinates
│   ├── Entities/              # Game actors and environmental interactive bodies
│   │   ├── Werewolf.cs        # Player CharacterBody2D: movement, animation states, combat, skills
│   │   ├── BloodCoreObject.cs # StaticBody2D: central cave altar, [E] restore, [R] flask refill
│   │   ├── BloodSpot.cs       # Area2D: 40s ground pickup from killed living creatures
│   │   ├── GrassObject.cs     # StaticBody2D: selectable/choppable wild grass, drops Grass
│   │   ├── TerrainArtifact.cs # Node2D: decorative ground debris spaced away from obstacles
│   │   ├── QuartzObject.cs    # StaticBody2D: 3-variant choppable mineral crystals, drops Quartz
│   │   ├── ChestObject.cs     # StaticBody2D: rare treasure chests with multi-resource & flask loot
│   │   ├── LairEntranceObject.cs # StaticBody2D: wilderness entrance archway to cave hideout
│   │   ├── Deer.cs            # Prey/monster CharacterBody2D: wandering, aggro AI, meat drop
│   │   ├── Villager.cs        # Village NPC CharacterBody2D: 3-row state machine, combatant, coin drop
│   │   ├── TreeObject.cs      # StaticBody2D: 3-tier selectable/choppable trees, log drop
│   │   ├── RockObject.cs      # StaticBody2D: quarry interaction, stone drop
│   │   ├── HouseObject.cs     # StaticBody2D: village cottages with custom collision boxes
│   │   ├── DroppedLoot.cs     # Area2D: collectible pickups with custom hover cursors
│   │   └── ISelectableTarget.cs # Interface for entities that can be clicked and targeted
│   ├── World/                 # World generation and environment managers
│   │   ├── WorldManager.cs    # Open-world generator: landmarks, vegetation, quartz, chests, grass
│   │   └── LairManager.cs     # Cave hideout controller: multi-tile floor schema, borders, exit portal
│   ├── UI/                    # CanvasLayer and HUD components
│   │   ├── HUDManager.cs      # Top-level UI manager: viewport resize listeners, layout anchors
│   │   ├── OrbGauge.cs        # Diablo-style liquid orb (procedural _Draw polygon + bubbles)
│   │   ├── StatsPanel.cs      # 4 skill hotkey slots with cooldown sweeps and timers
│   │   ├── TargetPanel.cs     # Selected target health bar + contextual interaction button
│   │   ├── PouchWindow.cs     # Freeform draggable inventory modal with right-click consumption
│   │   ├── HeroDetailsWindow.cs # Draggable character attribute sheet with solo portrait & wooden sign
│   │   └── ActionBar.cs       # Action bar holding Pouch and Hero Details toggle buttons
│   └── Effects/               # Transient combat feedback and visual FX
│       ├── BuffAura.cs        # CpuParticles2D violet glowing aura for Howl buff
│       ├── DamageNumber.cs    # Floating text drift and fade effect
│       ├── ScratchEffect.cs   # Blood splatter hit effect (1/3 scale from blood-hits/1..3.png)
│       └── FogZone.cs         # Atmospheric drifting mist particles
└── assets/                    # Textures, spritesheets, fonts, cursors
    ├── cave-objects/          # blood-core.png (650x802), blood-juicer.png, etc.
    ├── blood-hits/            # 1.png, 2.png, 3.png combat splatter hit textures
    ├── blood-spots/           # 1.png, 2.png, 3.png 40-second ground drop textures
    ├── flasks/                # blood_flask_0..100.png 5-tier fluid sprites
    ├── grass/                 # 1..4.png wild grass variants and grass_drop.png
    ├── terrain-artifacts/     # 1..8.png ground debris decals
    ├── resources/quartz/      # quartz_1..3.png mineral deposit sprites
    ├── chests/                # chest.png treasure container sprite
    ├── lair/                  # lair_entrance.png, lair_border_o.png, lair-floor/lair_1..5.jpeg
    ├── cursors/               # normal_o.png, interaction_o.png, grab_o.png
    ├── werewolf/              # werewolf_head.png, solo.png, optimized/ animation spritesheets
    ├── houses/                # house_1..4.png, simple_path_cross_prim.png, lantern_light.png
    ├── icons/                 # scratch_hit_icon.png, charge_attack.png, bite.png, blood_howling.png
    ├── villager/              # villager.png (3x3 spritesheet)
    └── gold_coins.png         # Collectible currency sprite
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
  $$\text{Hit Chance} = \text{Clamp}(\text{Accuracy} - \text{Evasion}, 0.05, 0.95)$$
  Rolled via `GD.Randf() < hitChance`.
- **Damage Calculations**:
  - Regular melee hit: $\max(1, \text{Damage} - \frac{\text{Defense}}{2})$
  - Scratch Hit: $\max(1, \text{Damage} - \frac{\text{Defense}}{2} + 12)$
  - Charge Attack: $\max(1, \text{Damage} - \frac{\text{Defense}}{2} + 20)$
  - Execute Bite: $\max(1, \text{Damage} - \frac{\text{Defense}}{2} + 15)$ *(Target must be $\le 25\%$ HP; heals werewolf for $+20$ HP)*
- **Health & Power Economy**:
  - **Wilderness Passive Regeneration**:
    - Health regenerates passively at $0.10\text{ units/sec}$ ($1.0\text{ HP}$ per $10\text{s}$).
    - Power regenerates passively at $0.20\text{ units/sec}$ ($2.0\text{ Power}$ per $10\text{s}$).
  - **Cave / Lair Regeneration Rules**:
    - Inside the Werewolf's Lair (`GameState.Instance.IsInLair == true`), passive health and power regeneration are **completely disabled**.
    - Recovery inside the cave is strictly active:
      1. **Blood Core Altar ('Press E')**: Consumes $250\text{ blood}$ from the core to restore $+12\text{ HP}$ and $+13\text{ Power}$ ($25\text{ points total}$).
      2. **Eating Meat**: Right-click `"Meat"` in the pouch modal to consume $1$ meat and gain $+20\text{ HP}$ and $+10\text{ Power}$.
      3. **Drinking Blood Flasks**: Right-click `"BloodFlask"` in the pouch modal to gain up to $+25\text{ HP}$ and $+25\text{ Power}$ (proportional to flask fill), returning an `"EmptyFlask"`.
      4. **Execute Bite**: Usable on living combatants $\le 25\%$ HP to heal $+20\text{ HP}$.
  - **Skill Power Costs**:
    - Scratch Hit: 15 Power
    - Charge Attack: 25 Power
    - Execute Bite: 20 Power
    - Blood Howling: 40 Power (Grants $+30\%$ to Damage, Defense, Speed, Accuracy, Evasion for $10\text{s}$)

### 3.4 Open World Map & Persistent Landmarks (`WorldManager.cs`)
- **World Bounds**: 10,000 × 10,000 units (`WorldRadius = 5000f`), surrounded by perimeter collision walls and dense border trees.
- **Camera2D Tracking**: Smooth camera attached to the `Werewolf` player (`PositionSmoothingEnabled = true`, limits clamped to world boundaries).
- **Dynamic Terrain Tiling**: Forest ground texture (`pine_tree_forest_ground_1.png`) is dynamically snapped to 350px tile intervals centered on the player/camera for seamless infinite scrolling.
- **Coordinate System & Map Coordinates**:
  - Internal world coordinates use Godot 2D (`+X` Right, `+Y` Down).
  - Map / HUD coordinates use standard Cartesian coordinates (`WorldManager.ToMapCoordinates`): `+X` East/Right, `+Y` North/Up (inverting Godot's vertical axis).
- **Points of Interest (Landmarks)**:
  - **Awakening Grove** at Map `(0, 0)` [World `(0, 0)`]: Central clearing where the player awakens.
  - **Werewolf's Lair Entrance** at Map `(-650, 450)` [World `(-650, -450)`]: Ancient stone archway leading to the subterranean cave hideout (`scenes/Lair.tscn`). Features blinking prompt `"Hit Enter to enter the cave"` and safe return coordinate at `(-650, -360)`.
  - **The Village** at Map `(2500, 1800)` [World `(2500, -1800)`]: 8 enlarged cottages arranged in a circle around the central lantern with cobblestone pathing (North-East) and roaming human NPCs (`Villager`) dropping Gold Coins and Meat.
  - **Silent Lake** at Map `(-2000, -2000)` [World `(-2000, 2000)`]: Dynamic lake with water body collision (South-West).
  - **Misty Lake** at Map `(-2200, 2200)` [World `(-2200, -2200)`]: Second natural lake formation (North-West).
  - **Quarry Hills** at Map `(2200, -2200)` [World `(2200, 2200)`]: Dense cluster of minable boulders (`RockObject`) (South-East).
  - **Hunting Grounds & Deep Wilderness**: Expansive clearings with roaming herds of deer (`Deer`), ~450 harvestable pine trees (`TreeObject`), minable quartz clusters (`QuartzObject`), hidden treasure chests (`ChestObject`), choppable wild grass (`GrassObject`), and decorative ground decals (`TerrainArtifact`).

### 3.5 Custom UI Rendering & Windows
- **`OrbGauge`**:
  - Inherits `Control`. Uses `_Draw()` with trigonometric chord calculations to render liquid fill polygons.
  - Maintains an internal pool of rising `Bubble` structs simulated in `_Process`.
  - Blits decorative ring frame texture over the orb, scaled with an expanded radius (`RingRadiusOffset = 44f`) to seamlessly encompass the liquid fluid.
- **`PouchWindow`**:
  - Freeform draggable modal toggleable with <kbd>P</kbd> or HUD pouch button.
  - Items are freeform controls within `_itemsArea` clamped to boundaries; item coordinates persist in save file.
  - Supports uniform 44×44px slot display, stack counters, and right-click consumption for `"Meat"` and `"BloodFlask"`.
- **`HeroDetailsWindow`**:
  - Draggable character sheet toggleable via <kbd>C</kbd> or HUD menu button.
  - Features 600×650 werewolf portrait (`solo.png`) anchored on the left, rustic wooden sign background (`wooden_sign_flat.png`), and live combat attributes (Health, Power, Damage, Defense, Speed, Accuracy, Evasion).
- **`MenuBar`**:
  - Clean modular action bar positioned beside the skill bar holding the Hero Details icon (`werewolf_head.png` scaled 50%) and Pouch Bag icon.
- **`TargetPanel`**:
  - Contextual target frame showing name, health bar, and dynamic action button (*"Chop"*, *"Quarry"*, *"Mine"*, *"Open"*, or *"Attack"*).

### 3.6 State Persistence (`SaveManager.cs`)
- Serializes `SaveData` to `user://werewolves_save.json` using `System.Text.Json`.
- Automatically invoked on:
  - Position changes / movement (`GameState.SetPlayerPosition` / `LoadedPlayerPosition`)
  - Item collection (`GameState.AddPouchItem`)
  - Item repositioning in pouch (`GameState.UpdatePouchItemPosition`)
  - Blood Core reserve updates (`bloodCoreReserves`, default 1000)

### 3.7 Subterranean Hideout & Blood Core (`scenes/Lair.tscn` & `BloodCoreObject.cs`)
- **Single-Screen Cavern**: A compact subterranean sanctuary surrounded by pitch-black space (`#030305`), a procedurally twinkling 240-star field, and drifting cavern fog zones.
- **Procedural Floor Schema**: $5 \times 9$ stone tile matrix (`lair_1..5.jpeg`, scale 0.1709) with perimeter edge borders (`lair_border_o.png`) and solid collision barriers.
- **Blood Core Altar (`BloodCoreObject`)**:
  - Placed at cavern center `(0, 0)`.
  - Overhead percentage progress bar showing `"{percent}% ({reserves} / {max})"`.
  - Dual prompts:
    - `[E] Restore Health & Power (-250 Blood)` $\implies$ consumes 250 blood, restores +12 HP & +13 Power.
    - `[R] Fill Core with Blood Flask` $\implies$ pours blood from held flasks (+250 blood for 100% flask) and returns an `"EmptyFlask"`.

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
| `skill_1` | <kbd>1</kbd> | Scratch Hit (Melee swipe) |
| `skill_2` | <kbd>2</kbd> | Charge Attack (Dash strike) |
| `skill_3` | <kbd>3</kbd> | Execute Bite (Execute target $\le 25\%$ HP & heal) |
| `skill_4` | <kbd>4</kbd> | Blood Howling (Buff Damage, Def, Speed, Acc, Eva) |
| `toggle_pouch` | <kbd>P</kbd> | Toggle Inventory Pouch Window |
| `toggle_hero_details` | <kbd>C</kbd> | Toggle Hero Details Character Sheet |
| `interact_core` | <kbd>E</kbd> | Restore Health & Power at Blood Core Altar |
| `fill_core` | <kbd>R</kbd> | Pour Blood Flask into Blood Core Altar |
| `enter_lair` / `ui_accept` | <kbd>Enter</kbd> | Enter / Exit Werewolf's Lair Portal |
| *(Pouch GUI)* | <kbd>Right Click</kbd> | Consume Meat (+20 HP, +10 Pwr) or Drink Blood Flask |

When querying inputs in code:
```csharp
Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
bool isSprinting = Input.IsActionPressed("sprint");
if (Input.IsActionJustPressed("skill_1")) { ... }
if (Input.IsActionJustPressed("toggle_hero_details")) { ... }
if (Input.IsActionJustPressed("interact_core")) { ... }
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
