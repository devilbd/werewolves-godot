# 🐺 Werewolves (Godot 4 C# Edition)

[![Godot Engine](https://img.shields.io/badge/Godot-4.7--dev-478cbf?logo=godotengine&logoColor=white)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512bd4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%2012-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Desktop-orange)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#)

A 2D top-down Action RPG / Survival Exploration game built with **Godot 4.7 (.NET / C#)**. Ported and modernized from an original Angular/HTML5 Canvas prototype into an extensible, object-oriented engine architecture.

---

## 📖 Table of Contents

- [Overview & Lore](#-overview--lore)
- [Key Features & Mechanics](#-key-features--mechanics)
  - [Persistent Open-World Exploration](#persistent-open-world-exploration)
  - [Werewolf Character & Movement](#werewolf-character--movement)
  - [Combat & Skills System](#combat--skills-system)
  - [Fauna & NPC Ecology](#fauna--npc-ecology)
  - [Resource Gathering & Harvesting](#resource-gathering--harvesting)
  - [Diablo-Style Liquid Orb HUD](#diablo-style-liquid-orb-hud)
  - [Freeform Draggable Pouch Inventory](#freeform-draggable-pouch-inventory)
  - [Game State & Persistence](#game-state--persistence)
- [Controls](#-controls)
- [Project Architecture](#-project-architecture)
  - [Directory Structure](#directory-structure)
  - [Core Components Breakdown](#core-components-breakdown)
- [Prerequisites & Installation](#-prerequisites--installation)
- [Building & Running](#-building--running)
- [Configuration & Settings](#-configuration--settings)
- [Documentation & Agent Guides](#-documentation--agent-guides)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🌑 Overview & Lore

In **Werewolves**, you inhabit a powerful werewolf traversing the secluded borderlands between human civilization and the primeval wilderness. Roam across a continuous open world (10,000×10,000 units), hunt forest fauna, fell timber, quarry stone, infiltrate the human village settlement, confront roaming villagers, and harness primal combat techniques to survive.

---

## ✨ Key Features & Mechanics

### Persistent Open-World Exploration
- **Massive Coordinate Space**: A continuous 10,000×10,000 world space (`WorldRadius = 5000f`) centered around the Awakening Grove at `(0, 0)`, bounded by dense perimeter tree barriers and physical collision walls.
- **Smooth Camera Tracking**: Smooth Camera2D attached to the player with boundary clamping and position smoothing (`PositionSmoothingEnabled = true`).
- **Dynamic Terrain Tiling**: Seamless infinite forest ground texture dynamically tiled and snapped to 350px intervals relative to camera and player movement.
- **Key Landmarks & Points of Interest**:
  - **Awakening Grove** at Map `(0, 0)` [World `(0, 0)`]: The central clearing where the player awakens.
  - **The Village** at Map `(2500, 1800)` [World `(2500, -1800)`]: 8 cobblestone cottages arranged in a circle around an illuminated lantern, cobblestone paths, and roaming human villagers.
  - **Silent Lake** at Map `(-2000, -2000)` [World `(-2000, 2000)`]: Large natural lake formation with water collision body.
  - **Misty Lake** at Map `(-2200, 2200)` [World `(-2200, -2200)`]: Second deep lake formation in the northern reaches.
  - **Quarry Hills** at Map `(2200, -2200)` [World `(2200, 2200)`]: Rocky hillside with a dense cluster of minable boulders.
  - **Hunting Grounds & Deep Wilderness**: Expansive clearings with roaming deer herds, ~450 harvestable pine trees, and scattered quarry rocks.
- **Y-Sort Depth Sorting**: Dynamic visual layering ensures player, creatures, NPCs, trees, and buildings sort correctly along the 2D vertical axis.

### Werewolf Character & Movement
- **Omnidirectional Movement**: 4-directional WASD movement with automatic sprite flipping (`FlipH`) tracking facing orientation.
- **Sprint Mode**: Hold <kbd>Shift</kbd> for a 60% movement velocity boost with accelerated running footstep frame cycles.
- **Multi-Row Spritesheet State Machine**:
  - Dedicated animations for standard walking, sprint runs, multi-variant idle states with randomized relaxation timers, physical attacks, and magic invocations.

### Combat & Skills System
Combat utilizes an active warmode auto-attack loop (1.0s interval within melee radius) combined with 4 manual hotkey abilities powered by the **Power (Fury/Mana)** orb:

| Skill | Hotkey | Power Cost | Cooldown | Description |
| :--- | :---: | :---: | :---: | :--- |
| **Scratch Hit** | <kbd>1</kbd> | 15 | 5.0s | Swift claw swipe dealing `BaseDamage - Def/2 + 12` bonus damage with a directional visual slash effect. |
| **Charge Attack** | <kbd>2</kbd> | 25 | 8.0s | High-speed dash toward the selected target, executing a crushing strike for `BaseDamage - Def/2 + 20` bonus damage upon impact. |
| **Execute Bite** | <kbd>3</kbd> | 20 | 10.0s | Savage bite usable **only** when target health is $<25\%$. Deals `BaseDamage - Def/2 + 15` damage and **heals the werewolf for +20 HP**. |
| **Blood Howling** | <kbd>4</kbd> | 40 | 30.0s | Primal roar buffing **Damage, Defense, Speed, Accuracy, and Evasion by +30% for 10 seconds**, enveloped in a glowing violet particle aura. |

### Fauna & NPC Ecology
- **Deer Fauna**:
  - Realistic wandering AI with randomized directional vectors and pause intervals.
  - Boundary collision avoidance.
  - **Retaliatory Aggro**: Taking damage turns deer aggressive, chasing the werewolf and striking back within melee range.
  - Visual selection bracket indicators rendered directly via CanvasItem `_Draw()`.
  - Smooth death opacity fade-out followed by **Meat** loot generation.
- **Villager NPCs**:
  - Inhabit The Village settlement with 3-row spritesheet state machine (wander/run, attack, and death animations).
  - Wandering AI within the village perimeter with randomized facing and movement cycles.
  - Selectable target with combat stats matching wildlife, fighting back if engaged.
  - Drops **Gold Coins** (2 to 8 coins per defeat).

### Resource Gathering & Harvesting
- **Interactive Pine Trees**: Trees flagged as harvestable highlight on mouse hover; interacting or chopping with <kbd>Attack</kbd> deals 25 chop damage per strike. Depleted trees drop **Logs** loot.
- **Quarry Boulders**: Interactive stone formations quarryable for 25 damage per hit, crumbling into collectible **Stones** upon destruction.
- **Ground Loot & Pickup**: Dropped logs, stones, meat, and gold coins float in the world with specialized hovering cursor indicators (`grab_o.png`). Left-clicking collects them into the player pouch with floating text feedback.

### Diablo-Style Liquid Orb HUD
- **Custom-Drawn Spherical Gauges**:
  - **Health Orb (Left)**: Deep crimson fluid with buoyant rising bubbles and gold filigree ring frame.
  - **Power Orb (Right)**: Glowing cyan/teal reservoir showing active combat energy.
  - Custom trigonometric polygon triangulation fills the sphere according to exact current percentages.
  - **Enlarged Decorative Ring Frames**: Wolf-head ornamental frames scaled with expanded radius (`RingRadiusOffset = 44f`) to seamlessly encapsulate the liquid orbs with proper padding.
- **Floating Combat Text**: Real-time floating damage numbers for melee damage (yellow), skill strikes (red), heals (green), misses (grey), and loot pickups.
- **Target Inspection Panel**: Context-sensitive HUD panel displaying selected entity name, health progress bar, and adaptive action button (*"Chop"*, *"Quarry"*, or *"Attack"*).
- **Skill Action Bar**: Visual hotkey slots with power affordability dimming, cooldown sweep overlays, and seconds countdown timers.

### Freeform Draggable Pouch Inventory
- **Draggable Window**: Toggleable with <kbd>P</kbd> or HUD bag button. Move the inventory window anywhere on screen via its leather handle bar or background.
- **Gridless Item Placement**: Items can be picked up and repositioned freely inside the pouch boundaries; spatial coordinates (`PosX`, `PosY`) persist between game sessions.
- **Stack Badging**: Visual quantity counters on every collected resource.

### Game State & Persistence
- Automatically writes game state to `user://werewolves_save.json`.
- Preserves:
  - Player global position coordinates (`PlayerX`, `PlayerY`).
  - Complete pouch item registry with freeform position offsets and stack counts.

---

## 🎮 Controls

| Input | Key / Mouse | Description |
| :--- | :--- | :--- |
| **Movement** | <kbd>W</kbd>, <kbd>A</kbd>, <kbd>S</kbd>, <kbd>D</kbd> | Move in 4 cardinal and diagonal directions |
| **Sprint** | <kbd>Shift</kbd> (Hold) | Sprint at 1.6× movement speed |
| **Skill 1** | <kbd>1</kbd> | Scratch Hit (Power: 15) |
| **Skill 2** | <kbd>2</kbd> | Charge Attack (Power: 25) |
| **Skill 3** | <kbd>3</kbd> | Execute Bite (Power: 20, Target $<25\%$ HP) |
| **Skill 4** | <kbd>4</kbd> | Blood Howling Buff (Power: 40) |
| **Pouch Inventory**| <kbd>P</kbd> | Open / Close Inventory Pouch |
| **Select / Target** | <kbd>Left Click</kbd> | Select enemy, tree, boulder, or target panel button |
| **Collect Loot** | <kbd>Left Click</kbd> | Pick up dropped logs, stones, meat, or gold coins |
| **Drag Window / Items** | <kbd>Left Click & Drag</kbd> | Reposition inventory window or arrange items |

---

## 🏗️ Project Architecture

### Directory Structure

```text
werewolves-godot/
├── .agents/                    # Specialized AI agent roles, workflows, and task playbooks
│   ├── roles/                  # Role guidelines (Gameplay, UI/UX, World, QA)
│   ├── workflows/              # Step-by-step playbooks (Add Skill, Add Creature, Add Item)
│   └── README.md               # Guide to the .agents system
├── AGENTS.md                   # Operational guidelines for autonomous and pair agents
├── GEMINI.md                   # Core development context for Gemini / AI assistants
├── README.md                   # Project documentation (this file)
├── Werewolves.csproj           # C# project definition (.NET 8.0, Godot.NET.Sdk 4.7.2)
├── Werewolves.sln              # Visual Studio / .NET Solution
├── project.godot               # Godot 4 project configuration, input map, autoloads
├── icon.svg                    # Application launcher icon
├── assets/                     # Graphic textures, fonts, and sprites
│   ├── cursors/                # Contextual mouse cursors (normal, interaction, grab)
│   ├── fonts/                  # Custom game fonts (Monster Blood TTF)
│   ├── houses/                 # Village building sprites and cobblestone tiles
│   ├── icons/                  # Skill, interaction, and menu icons
│   ├── rocks/                  # Boulder variations and optimized sprites
│   ├── trees/                  # Pine tree variants
│   ├── werewolf/               # Werewolf spritesheets (idle, moving, attack, magic)
│   ├── villager/               # Villager spritesheet assets
│   └── gold_coins.png          # Collectible currency sprite
├── scenes/                     # Packed Godot scene trees (.tscn)
│   ├── Main.tscn               # Root scene combining World, Entities, and HUD
│   ├── Entities/               # Entity scenes (Werewolf, Deer, Villager, Tree, Rock, House)
│   └── UI/                     # UI scenes (ActionBar, PouchWindow, StatsPanel, TargetPanel)
└── Scripts/                    # C# Source Code
    ├── Main.cs                 # Root initializer and node dependency binder
    ├── Core/                   # Fundamental systems
    │   ├── GameState.cs        # Global singleton: stats, skills, inventory, events
    │   ├── Formulas.cs         # Damage, hit chance, and power gain math
    │   ├── SaveManager.cs      # JSON save/load persistence layer
    │   ├── ICombatant.cs       # Interface for damageable combat entities
    │   └── PouchItemData.cs    # Inventory item serialization model
    ├── Effects/                # Visual FX and Combat Feedback
    │   ├── BuffAura.cs         # Procedural CPU particle aura for Howl buff
    │   ├── DamageNumber.cs     # Drifting floating combat text
    │   └── ScratchEffect.cs    # Claw swipe sprite animation
    ├── Entities/               # Game objects and actors
    │   ├── Werewolf.cs         # Player controller, input, combat, animations
    │   ├── Deer.cs             # Prey AI, retaliation, pathing, death
    │   ├── Villager.cs         # Human NPC AI, animations, combat, coin drops
    │   ├── TreeObject.cs       # Harvestable tree static body
    │   ├── RockObject.cs       # Quarryable boulder static body
    │   ├── HouseObject.cs      # Village cottage static obstacles
    │   ├── DroppedLoot.cs      # Interactive collectible pickups
    │   └── ISelectableTarget.cs# Interface for mouse-selectable targets
    ├── UI/                     # CanvasLayer and HUD management
    │   ├── HUDManager.cs       # Viewport manager, anchor layout, event wiring
    │   ├── OrbGauge.cs         # Diablo liquid gauge with bubble simulation
    │   ├── StatsPanel.cs       # Skill bar, cooldown overlays, hotkey labels
    │   ├── TargetPanel.cs      # Target health and contextual action button
    │   ├── PouchWindow.cs      # Draggable modal and freeform item slot organizer
    │   └── ActionBar.cs        # Mini HUD bar with pouch toggle button
    └── World/                  # Environment & Generation
        └── WorldManager.cs     # Open-world generator, persistent landmarks & dynamic terrain
```

### Core Components Breakdown

1. **`GameState` (`Scripts/Core/GameState.cs`)**:
   Registered as an autoload singleton. Holds player health/power, base stats, skill cooldown arrays, player world position (`Vector2`), pouch inventory, and active target. Dispatches C# events (`OnHealthChanged`, `OnPowerChanged`, `OnPositionChanged`, `OnCooldownUpdated`, `OnTargetChanged`, `OnSpawnDamageNumber`, `OnPouchToggled`, `OnPouchChanged`).

2. **`WorldManager` (`Scripts/World/WorldManager.cs`)**:
   Generates the continuous 10,000×10,000 open world, static landmark locations (Awakening Grove, The Village, Silent Lake, Misty Lake, Quarry Hills), dynamic infinite terrain ground tiling snapped to camera intervals, perimeter collision boundaries, and entity containers.

3. **`Werewolf` (`Scripts/Entities/Werewolf.cs`)**:
   Main player actor (`CharacterBody2D`). Coordinates 8-directional movement physics, sprite flipping, camera tracking, warmode auto-attack timers against combatants, manual skill executions, and interaction triggers.

4. **`Villager` (`Scripts/Entities/Villager.cs`)**:
   Village human NPC (`CharacterBody2D`). Features a 3-row spritesheet state machine (wander, attack, death), wanders The Village, fights back when targeted, and drops Gold Coins upon defeat.

5. **`OrbGauge` (`Scripts/UI/OrbGauge.cs`)**:
   Custom Control node utilizing Godot's `_Draw()` pipeline to render filled circular sectors, buoyant bubble particles, ornamental border rings, and value labels.

---

## 📦 Prerequisites & Installation

To develop or build this project, ensure you have:

1. **.NET SDK**: Version **8.0** or **10.0+**
   - Verify with: `dotnet --version`
2. **Godot Engine**: **Godot 4.7 (.NET / Mono version)**
   - Standard Godot builds without C#/.NET support will not compile C# scripts.
   - Download the .NET enabled build from the official Godot website or your package manager (`godot-mono`).

---

## 🚀 Building & Running

### Build via .NET CLI
Compile the C# solution:
```bash
dotnet build Werewolves.sln
```

### Run via Godot CLI
Launch the project directly:
```bash
godot-mono --path .
```

### Run via Godot Editor
1. Open the Godot 4.7 (.NET version) Editor.
2. Click **Import**, navigate to this directory, and select `project.godot`.
3. Press **F5** or the **Play** button in the top-right corner to launch the main scene (`scenes/Main.tscn`).

---

## ⚙️ Configuration & Settings

- **Base Resolution**: 1920×1080 (`canvas_items` stretch mode with `expand` aspect ratio for responsive UI scaling).
- **Renderer**: `Forward Plus` (Vulkan 3D/2D backend).
- **Save File**: Located at `user://werewolves_save.json`
  - Linux: `~/.local/share/godot/app_userdata/Werewolves/werewolves_save.json`
  - Windows: `%APPDATA%\Godot\app_userdata\Werewolves\werewolves_save.json`
  - macOS: `~/Library/Application Support/Godot/app_userdata/Werewolves/werewolves_save.json`

---

## 🤖 Documentation & Agent Guides

For contributors, autonomous agents, and AI pair-programmers:
- **[GEMINI.md](file:///run/media/devilbd/d/Development/godot-dev/werewolves-godot/GEMINI.md)**: Deep technical manual covering C# conventions, memory management, node lifecycles, and Godot 4.7 architecture.
- **[AGENTS.md](file:///run/media/devilbd/d/Development/godot-dev/werewolves-godot/AGENTS.md)**: Standard operating procedures, role definitions, and workflow constraints for autonomous agents.
- **[.agents/](file:///run/media/devilbd/d/Development/godot-dev/werewolves-godot/.agents/README.md)**: Role prompts and actionable step-by-step playbooks for adding skills, creatures, and items.

---

## 🤝 Contributing

Contributions are welcome! Please follow these rules:
1. Ensure all C# scripts compile cleanly (`dotnet build Werewolves.sln`) with zero warnings or errors.
2. Follow file-scoped namespaces (`namespace Werewolves.X;`) and project naming conventions.
3. Preserve existing scene node hierarchies and bindings.
4. When adding new assets or scenes, register corresponding UID files where appropriate.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) (or applicable open-source license). Assets and sprites are property of their respective creators.
