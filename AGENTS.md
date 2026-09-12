# AGENTS.md — Autonomous & Pair-Programming Agent Guidelines

Welcome to the **Werewolves (Godot 4 C# Edition)** repository. This document outlines operational protocols, role expectations, development workflows, and safety rules for AI agents operating autonomously or in pair-programming modes.

---

## 1. Guiding Principles & Philosophy

1. **Do No Harm to the Scene Graph**: Scene files (`.tscn`) and C# scripts are co-dependent. Never break node path expectations or rename nodes without updating corresponding scripts.
2. **Dual-Mode Construction**: Scripts must support both running from pre-configured packed scenes (`.tscn`) and programmatic instantiations (`new T()`). Always provide fallback node creation logic in `_Ready()`.
3. **Clean C# 12 & .NET 8 Standards**: Maintain file-scoped namespaces, nullable reference types, and explicit memory cleanup (`QueueFree`).
4. **Compile-Before-Commit**: Always verify your code changes with `dotnet build Werewolves.sln` and resolve all compiler errors and warnings.

---

## 2. Agent Roles & Specializations

Specialized guidelines and prompts for specific development tasks reside in the [`.agents/`](.agents/README.md) directory:

| Role | Domain / Focus | Reference |
| :--- | :--- | :--- |
| **Gameplay Engineer** | Player mechanics, combat math, monster AI, entity state, skills | [`.agents/roles/gameplay-engineer.md`](.agents/roles/gameplay-engineer.md) |
| **UI/UX Engineer** | HUD layout, CanvasLayer, custom `_Draw` gauges, drag-and-drop modals | [`.agents/roles/ui-ux-engineer.md`](.agents/roles/ui-ux-engineer.md) |
| **World Architect** | Map coordinate matrix, procedural biomes, collision volumes, Y-sorting | [`.agents/roles/world-architect.md`](.agents/roles/world-architect.md) |
| **QA Engineer** | Solution builds, regression checks, save state integrity, smoke testing | [`.agents/roles/qa-engineer.md`](.agents/roles/qa-engineer.md) |

---

## 3. Standard Operating Procedures (SOPs)

### Phase 1: Research & Discovery
- Inspect existing implementations before writing new code.
- Consult [GEMINI.md](GEMINI.md) for architectural blueprints, input action maps, and formulas.
- Verify node hierarchy in `scenes/` if modifying or adding UI controls or game actors.

### Phase 2: Implementation Rules
- **Namespaces**: Keep the namespace structure clear:
  - `Werewolves.Core`: Singletons, domain models, formulas, save data.
  - `Werewolves.Entities`: CharacterBody2D, StaticBody2D, Area2D game objects.
  - `Werewolves.UI`: CanvasLayer, Control, Window nodes.
  - `Werewolves.Effects`: Particles, animations, floating text.
  - `Werewolves.World`: Tile backgrounds, area generation, map routing.
- **Node Lookups**: Use `GetNodeOrNull<T>("Path")` with null-coalescing fallbacks.
- **State Changes**: Route all player stats and inventory mutations through `GameState.Instance` to ensure HUD events and persistence triggers fire.
- **Cursor State**: When altering mouse cursors on hover, always provide an unhover reset (`normal_o.png`), including cases where the object is collected or freed.

### Phase 3: Verification & Quality Assurance
- Run the build:
  ```bash
  dotnet build Werewolves.sln
  ```
- Verify zero warnings, zero errors.
- Confirm any newly added resources have appropriate import parameters or paths referenced in `assets/`.

---

## 4. Workflows & Playbooks

When performing common expansion tasks, follow the dedicated playbooks in [`.agents/workflows/`](.agents/workflows/):

1. **[Adding a New Skill](.agents/workflows/add-skill.md)**:
   - Define skill parameters in `GameState` (cooldown, power cost, buff/effect).
   - Add input action mapping in `project.godot`.
   - Update `Werewolf.cs` execution switch and animation triggers.
   - Update `StatsPanel.cs` icons, cooldown overlay, and hotkey labels.
   - Add formulas in `Formulas.cs`.

2. **[Adding a New Creature / Enemy](.agents/workflows/add-creature.md)**:
   - Create class implementing `ICombatant` and `ISelectableTarget`.
   - Configure collision shape, sprite frames, and Y-sort offset.
   - Implement wander and aggro AI state loop.
   - Wire death animation and loot drop instantiation.
   - Register in `WorldManager.cs` spawn pools.

3. **[Adding a New Resource / Item](.agents/workflows/add-item.md)**:
   - Define item key in `PouchWindow.cs` icon mapping.
   - Add loot drop sprite in `assets/` and map in `DroppedLoot.cs`.
   - Create interactive source object (e.g. tree, rock) or assign to creature drop table.
   - Ensure serialization is verified via `SaveManager.cs`.

---

## 5. Safety Protocols & Constraints

> [!IMPORTANT]
> **Autoload Singleton**: `GameState` is registered in `project.godot`. Do not instantiate duplicate copies in sub-scenes.

> [!WARNING]
> **Y-Sorting & Canvas Layers**:
> - World entities must be parented under `WorldManager/Entities` (which has `YSortEnabled = true`).
> - UI components belong inside `HUD` (`CanvasLayer`), completely separated from world coordinates.

> [!CAUTION]
> **UI Input Propagation**:
> Any custom control that accepts mouse clicks (like the Pouch window or items) must call `GetViewport().SetInputAsHandled()` if the click should not trigger in-game targeting or movement.
