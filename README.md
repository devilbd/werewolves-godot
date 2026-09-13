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
  - [Subterranean Sanctuary: Werewolf's Lair & Blood Core](#subterranean-sanctuary-werewolfs-lair--blood-core)
  - [Werewolf Character & Movement](#werewolf-character--movement)
  - [Combat & Skills System](#combat--skills-system)
  - [Blood System & Alchemy Flasks](#blood-system--alchemy-flasks)
  - [Fauna & NPC Ecology](#fauna--npc-ecology)
  - [Resource Gathering & World Interaction](#resource-gathering--world-interaction)
  - [Diablo-Style Liquid Orb HUD](#diablo-style-liquid-orb-hud)
  - [Hero Details UI & Modular Menu Bar](#hero-details-ui--modular-menu-bar)
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

In **Werewolves**, you inhabit a powerful werewolf traversing the secluded borderlands between human civilization and the primeval wilderness. Roam across a continuous open world (10,000×10,000 units), hunt forest fauna, fell timber, quarry stone, mine quartz veins, discover hidden treasure chests, infiltrate the human village settlement, confront roaming villagers, and retreat to your subterranean cave sanctuary to restore vital essences at the ancient Blood Core altar.

---

## ✨ Key Features & Mechanics

### Persistent Open-World Exploration
- **Massive Coordinate Space**: A continuous 10,000×10,000 world space (`WorldRadius = 5000f`) centered around the Awakening Grove at `(0, 0)`, bounded by dense perimeter tree barriers and physical collision walls.
- **Smooth Camera Tracking**: Smooth Camera2D attached to the player with boundary clamping and position smoothing (`PositionSmoothingEnabled = true`).
- **Dynamic Terrain Tiling**: Seamless infinite forest ground texture dynamically tiled and snapped to 350px intervals relative to camera and player movement.
- **Terrain Artifact Decals**: Natural ground details (foliage, moss, twigs, fallen bark) scattered across the terrain (`assets/terrain-artifacts/1..8.png`) with intelligent obstacle clearance.
- **Key Landmarks & Points of Interest**:
  - **Awakening Grove** at Map `(0, 0)` [World `(0, 0)`]: The central clearing where the player awakens.
  - **Werewolf's Lair Entrance** at Map `(-650, 450)` [World `(-650, -450)`]: Ancient stone archway leading to the subterranean sanctuary. Features proximity detection, animated prompt (*"Hit Enter to enter the cave"*), and safe return position.
  - **The Village** at Map `(2500, 1800)` [World `(2500, -1800)`]: 8 enlarged cottages arranged in a circle around an illuminated lantern, cobblestone paths, and roaming human villagers dropping Gold Coins and Meat.
  - **Silent Lake** at Map `(-2000, -2000)` [World `(-2000, 2000)`]: Large natural lake formation with water collision body.
  - **Misty Lake** at Map `(-2200, 2200)` [World `(-2200, -2200)`]: Second deep lake formation in the northern reaches.
  - **Quarry Hills** at Map `(2200, -2200)` [World `(2200, 2200)`]: Rocky hillside with a dense cluster of minable boulders.
  - **Hunting Grounds & Deep Wilderness**: Expansive clearings with roaming deer herds, ~450 harvestable pine trees, mineral quartz deposits, hidden treasure chests, and choppable wild grass.
- **Y-Sort Depth Sorting**: Dynamic visual layering ensures player, creatures, NPCs, trees, and buildings sort correctly along the 2D vertical axis.

### Subterranean Sanctuary: Werewolf's Lair & Blood Core
- **Dedicated Cave Scene (`scenes/Lair.tscn`)**: A compact "one-screen" hideout surrounded by cosmic void (`#030305`), a procedurally twinkling 240-star field, and drifting cavern mist zones.
- **Procedural Stone Floor**: Constructed with a $5 \times 9$ stone tile matrix (`lair_1..5.jpeg`) bordered by natural rocky edges (`lair_border_o.png`) and solid collision barriers.
- **Central Blood Core Altar (`BloodCoreObject`)**:
  - Starts with **1000 Blood** reserves (persisted in save file).
  - Features an overhead stylized percentage progress bar showing `"{percent}% ({reserves} / {max})"`.
  - **Press <kbd>E</kbd>**: Consumes 250 blood reserves to restore up to **+12 HP** and **+13 Power** (25 points total).
  - **Press <kbd>R</kbd>**: Refills the core by pouring blood from carried blood flasks (+250 blood for a 100% flask) and returns an **Empty Flask**.
- **Active In-Cave Recovery**: Passive regeneration over time is suspended inside the cave; health and fury must be restored actively via the Blood Core, eating Meat, drinking Blood Flasks, or landing Execute Bite strikes.
- **Return Portal**: Left exit tunnel arch returning safely to wilderness coordinates `(-650, -360)`.

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

### Blood System & Alchemy Flasks
- **Blood Splatters**: Combat strikes display randomized blood splatter textures (`assets/blood-hits/1..3.png`) scaled appropriately for clean combat impact.
- **Blood Spots on Ground**: Defeating living targets (Deer, Villagers) drops temporary blood pools (`assets/blood-spots/1..3.png`) that persist for 40 seconds before evaporating.
- **Flask Harvesting**:
  - Walking up to blood spots collects blood if the player holds **Empty Flasks** in their pouch.
  - Each blood spot fills a flask by **+20% to +30%**.
  - 5 visual fluid tiers (`blood_flask_0`, `_25`, `_50`, `_75`, `_100.png`).
  - Empty flasks stack indefinitely; filled flasks retain individual fill percentages.
- **Consumable Alchemy**: Right-clicking a blood flask in the pouch drinks it to restore health and fury, leaving an empty flask behind.

### Fauna & NPC Ecology
- **Deer Fauna**:
  - Realistic wandering AI with randomized directional vectors and pause intervals.
  - **Retaliatory Aggro**: Taking damage turns deer aggressive, chasing the werewolf and striking back within melee range.
  - Drops **Meat** and leaves a **Blood Spot** upon defeat.
- **Villager NPCs**:
  - Inhabit The Village settlement with 3-row spritesheet state machine (wander/run, attack, and death animations).
  - Combatants with balanced combat stats that fight back if engaged.
  - Drops randomized loot: **Gold Coins** (2–12), **Meat** (1–3), or both, categorized into Small Loot and More Loot tiers, plus leaves a **Blood Spot**.

### Resource Gathering & World Interaction
- **Interactive Pine Trees**: Trees generate in 3 randomized scale tiers (Small, Medium, Large) yielding 1 to 3 **Logs** upon depletion (25 damage per strike).
- **Quarry Boulders**: Interactive stone formations minable for 25 damage per strike, crumbling into collectible **Stones**.
- **Quartz Mineral Veins**: Rich crystal clusters (`assets/resources/quartz/quartz_1..3.png`) harvestable for **Quartz** minerals.
- **Hidden Treasure Chests**: Scattered exploration containers (`assets/chests/chest.png`) interactable via Left-Click to unseal resources and rare **Empty Flasks**.
- **Choppable Wild Grass**: 4 visual variants (`assets/grass/1..4.png`) choppable for 25 damage per hit, dropping collectible **Grass** bundles (`grass_drop.png`).
- **Ground Loot & Pickup**: Dropped items float in the world with specialized hovering cursor indicators (`grab_o.png`). Left-clicking collects them into the pouch with floating text feedback.

### Diablo-Style Liquid Orb HUD
- **Custom-Drawn Spherical Gauges**:
  - **Health Orb (Left)**: Deep crimson fluid with buoyant rising bubbles and gold filigree ring frame.
  - **Power Orb (Right)**: Glowing cyan/teal reservoir showing active combat energy.
  - Custom trigonometric polygon triangulation fills the sphere according to exact current percentages.
  - **Enlarged Decorative Ring Frames**: Wolf-head ornamental frames scaled with expanded radius (`RingRadiusOffset = 44f`) to seamlessly encapsulate the liquid orbs.
- **Floating Combat Text**: Real-time floating damage numbers for melee damage (yellow), skill strikes (red), heals (green), misses (grey), and loot pickups.
- **Target Inspection Panel**: Context-sensitive HUD panel displaying selected entity name, health progress bar, and adaptive action button (*"Chop"*, *"Quarry"*, *"Mine"*, *"Open"*, or *"Attack"*).
- **Skill Action Bar**: Visual hotkey slots with power affordability dimming, cooldown sweep overlays, and seconds countdown timers.

### Hero Details UI & Modular Menu Bar
- **Hero Details Character Sheet**: Toggleable with <kbd>C</kbd> or the HUD menu button.
  - Draggable modal window backed by a rustic wooden sign (`wooden_sign_flat.png`).
  - High-resolution 600×650 Werewolf portrait (`solo.png`) anchored on the left side.
  - Display plate showing player identity and live combat stats (Health, Power, Damage, Defense, Movement Speed, Accuracy, Evasion).
- **HUD Menu Bar**: Sleek bottom bar hosting the Hero Details button (featuring `werewolf_head.png` scaled at 50%) and the Pouch Bag toggle button.

### Freeform Draggable Pouch Inventory
- **Draggable Window**: Toggleable with <kbd>P</kbd> or HUD bag button. Move the inventory window anywhere on screen via its leather handle bar or background.
- **Gridless Item Placement**: Items can be picked up and repositioned freely inside the pouch boundaries; spatial coordinates (`PosX`, `PosY`) persist between game sessions.
- **Stack Badging & Uniform Slots**: 44×44px uniform slots with numerical stack badges on stackable materials.
- **Right-Click Consumption**:
  - Right-click **Meat**: Consumes 1 meat to restore **+20 HP** and **+10 Power**.
  - Right-click **Blood Flask**: Drinks blood to restore up to **+25 HP** and **+25 Power**, returning an **Empty Flask**.

### Game State & Persistence
- Automatically writes game state to `user://werewolves_save.json`.
- Preserves:
  - Player global position coordinates (`playerX`, `playerY`).
  - Complete pouch item registry with freeform position offsets, stack counts, and flask fill percentages.
  - Blood Core altar reserve pool (`bloodCoreReserves`).

---

## 🎮 Controls

| Input | Key / Mouse | Description |
| :--- | :--- | :--- |
| **Movement** | <kbd>W</kbd>, <kbd>A</kbd>, <kbd>S</kbd>, <kbd>D</kbd> | Move in 4 cardinal and diagonal directions |
| **Sprint** | <kbd>Shift</kbd> (Hold) | Sprint at 1.6× movement speed |
| **Skill 1** | <kbd>1</kbd> | Scratch Hit (Melee swipe, Power: 15) |
| **Skill 2** | <kbd>2</kbd> | Charge Attack (Dash strike, Power: 25) |
| **Skill 3** | <kbd>3</kbd> | Execute Bite (Execute target $<25\%$ HP & heal +20 HP, Power: 20) |
| **Skill 4** | <kbd>4</kbd> | Blood Howling Buff (+30% all combat stats, Power: 40) |
| **Hero Details** | <kbd>C</kbd> | Open / Close Hero Details Character Sheet |
| **Pouch Inventory**| <kbd>P</kbd> | Open / Close Inventory Pouch Window |
| **Blood Core Restore** | <kbd>E</kbd> | Restore +12 HP & +13 Power near Blood Core Altar (-250 Blood) |
| **Blood Core Fill** | <kbd>R</kbd> | Pour Blood Flask into Blood Core Altar (leaves Empty Flask) |
| **Enter / Exit Lair** | <kbd>Enter</kbd> | Step through the Cave Entrance / Exit Archway |
| **Select / Target** | <kbd>Left Click</kbd> | Target enemy, NPC, tree, boulder, quartz, chest, or grass |
| **Collect Loot** | <kbd>Left Click</kbd> | Pick up dropped logs, stones, quartz, grass, meat, flasks, or gold |
| **Consume Item** | <kbd>Right Click</kbd> | Eat Meat or drink Blood Flask inside the Pouch Window |
| **Drag Window / Items** | <kbd>Left Click & Drag</kbd> | Reposition inventory/hero windows or arrange items freely |

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
├── CALCULATIONS.md             # Combat formulas, drop rates, entity stats & balance models
├── GEMINI.md                   # Core development context for Gemini / AI assistants
├── LAIR.md                     # Cave hideout technical specification & coordinate mapping
├── VERSION-HISTORY.md          # Version history and release changelog
├── README.md                   # Project documentation (this file)
├── Werewolves.csproj           # C# project definition (.NET 8.0, Godot.NET.Sdk 4.7.2)
├── Werewolves.sln              # Visual Studio / .NET Solution
├── project.godot               # Godot 4 project configuration, input map, autoloads
├── icon.svg                    # Application launcher icon
├── config/                     # External balance and configuration JSON files
│   ├── combat.json             # Combat formulas, player/enemy stats, regen rates
│   ├── resources.json          # Node harvesting health, damage, and drop tables
│   └── chests.json             # Chest spawn counts, loot weights, and exclusions
├── assets/                     # Graphic textures, fonts, and sprites
│   ├── blood-hits/             # 1..3.png combat splatter hit textures
│   ├── blood-spots/            # 1..3.png 40-second ground drop textures
│   ├── cave-objects/           # blood-core.png (650x802), blood-juicer.png
│   ├── chests/                 # chest.png treasure container sprite
│   ├── cursors/                # Contextual mouse cursors (normal, interaction, grab)
│   ├── flasks/                 # blood_flask_0..100.png 5-tier fluid sprites
│   ├── fonts/                  # Custom game fonts (Monster Blood TTF)
│   ├── grass/                  # 1..4.png wild grass variants and grass_drop.png
│   ├── houses/                 # Village building sprites and cobblestone tiles
│   ├── icons/                  # Skill, interaction, and menu icons
│   ├── lair/                   # lair_entrance.png, lair_border_o.png, lair-floor/lair_1..5.jpeg
│   ├── resources/quartz/       # quartz_1..3.png mineral deposit sprites
│   ├── rocks/                  # Boulder variations and optimized sprites
│   ├── terrain-artifacts/      # 1..8.png ground debris decals
│   ├── trees/                  # Pine tree variants (Small, Medium, Large)
│   ├── werewolf/               # werewolf_head.png, solo.png, optimized/ animation spritesheets
│   ├── villager/               # Villager spritesheet assets
│   └── gold_coins.png          # Collectible currency sprite
├── scenes/                     # Packed Godot scene trees (.tscn)
│   ├── Main.tscn               # Root surface scene (World, Entities, HUD)
│   ├── Lair.tscn               # Subterranean hideout scene (LairManager, Entities, HUD)
│   ├── Entities/               # Entity scenes (Werewolf, Deer, Villager, Tree, Rock, House, Quartz, Chest, Grass, BloodCore, LairEntrance)
│   └── UI/                     # UI scenes (ActionBar, MenuBar, PouchWindow, StatsPanel, TargetPanel, HeroDetailsWindow)
└── Scripts/                    # C# Source Code
    ├── Main.cs                 # Root surface initializer and node dependency binder
    ├── Core/                   # Fundamental systems
    │   ├── GameState.cs        # Global singleton: stats, skills, inventory, events, blood core
    │   ├── Formulas.cs         # Combat math (damage, hit chance, power/health recovery)
    │   ├── ConfigManager.cs    # Runtime JSON configuration loader
    │   ├── SaveManager.cs      # JSON save/load persistence layer
    │   ├── ICombatant.cs       # Interface for damageable combat entities
    │   └── PouchItemData.cs    # Inventory item serialization model
    ├── Effects/                # Visual FX and Combat Feedback
    │   ├── BuffAura.cs         # Procedural CPU particle aura for Howl buff
    │   ├── DamageNumber.cs     # Drifting floating combat text
    │   ├── ScratchEffect.cs    # Claw swipe sprite animation
    │   └── FogZone.cs          # Cavern and wilderness mist effects
    ├── Entities/               # Game objects and actors
    │   ├── Werewolf.cs         # Player controller, input, combat, animations
    │   ├── Deer.cs             # Prey AI, retaliation, pathing, death
    │   ├── Villager.cs         # Human NPC AI, animations, combat, coin/meat drops
    │   ├── TreeObject.cs       # Harvestable tree static body (3 scale tiers)
    │   ├── RockObject.cs       # Quarryable boulder static body
    │   ├── QuartzObject.cs     # Minable crystal node static body
    │   ├── ChestObject.cs      # Interactive loot container static body
    │   ├── GrassObject.cs      # Choppable wild grass static body
    │   ├── HouseObject.cs      # Village cottage static obstacles
    │   ├── BloodCoreObject.cs  # Subterranean blood reservoir altar entity
    │   ├── BloodSpotObject.cs  # Temporary collectible ground blood puddle
    │   ├── LairEntranceObject.cs # Outside world entrance landmark portal
    │   ├── DroppedLoot.cs      # Interactive collectible pickups
    │   └── ISelectableTarget.cs# Interface for mouse-selectable targets
    ├── UI/                     # CanvasLayer and HUD management
    │   ├── HUDManager.cs       # Viewport manager, anchor layout, event wiring
    │   ├── OrbGauge.cs         # Diablo liquid gauge with bubble simulation
    │   ├── StatsPanel.cs       # Skill bar, cooldown overlays, hotkey labels
    │   ├── TargetPanel.cs      # Target health and contextual action button
    │   ├── PouchWindow.cs      # Draggable modal and freeform item slot organizer
    │   ├── HeroDetailsWindow.cs# Draggable character sheet with stats and portrait
    │   ├── MenuBar.cs          # Action bar holding Hero Details and Pouch buttons
    │   └── ActionBar.cs        # Compact HUD action bar
    └── World/                  # Environment & Generation
        ├── WorldManager.cs     # Open-world generator, landmarks, dynamic terrain & decals
        └── LairManager.cs      # Cave hideout procedural builder, collision & starfield
```

### Core Components Breakdown

1. **`GameState` (`Scripts/Core/GameState.cs`)**:
   Registered as an autoload singleton. Holds player health/power, base stats, skill cooldown arrays, player world position (`Vector2`), pouch inventory, active target, and `BloodCoreReserves`. Dispatches strongly-typed C# events (`OnHealthChanged`, `OnPowerChanged`, `OnPositionChanged`, `OnCooldownUpdated`, `OnTargetChanged`, `OnSpawnDamageNumber`, `OnPouchToggled`, `OnPouchChanged`, `OnBloodCoreReservesChanged`). Enforces suppression of passive regen when `IsInLair` is true.

2. **`WorldManager` (`Scripts/World/WorldManager.cs`)**:
   Generates the continuous 10,000×10,000 open world, static landmark locations (Awakening Grove, Lair Entrance, The Village, Silent Lake, Misty Lake, Quarry Hills), dynamic infinite terrain ground tiling, perimeter collision boundaries, randomized terrain decals (`TerrainArtifact`), and entity spawn pools.

3. **`LairManager` (`Scripts/World/LairManager.cs`)**:
   Scene controller for the cave sanctuary (`scenes/Lair.tscn`). Generates the $5 \times 9$ stone floor schema, seamless terrain borders, cavern collision walls, procedural 240-star twinkling cosmos, drifting fog mist, and instantiates the central `BloodCoreObject`.

4. **`Werewolf` (`Scripts/Entities/Werewolf.cs`)**:
   Main player actor (`CharacterBody2D`). Coordinates 8-directional movement physics, sprite flipping, camera tracking, warmode auto-attack timers against combatants, manual skill executions, and interaction triggers.

5. **`BloodCoreObject` (`Scripts/Entities/BloodCoreObject.cs`)**:
   Ancient blood reservoir altar at the center of the cave hideout. Manages 1000 blood reserves, overhead percentage progress bar, 'Press E' active restoration, and 'Press R' flask refilling.

6. **`HeroDetailsWindow` (`Scripts/UI/HeroDetailsWindow.cs`)**:
   Draggable character sheet modal displaying the 600×650 Werewolf portrait, wooden sign header, and real-time combat attributes.

7. **`PouchWindow` (`Scripts/UI/PouchWindow.cs`)**:
   Draggable inventory modal supporting freeform spatial item layout, stack counts, and right-click consumption for healing meat and blood flasks.

8. **`OrbGauge` (`Scripts/UI/OrbGauge.cs`)**:
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
