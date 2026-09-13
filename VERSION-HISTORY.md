# Werewolves — Version History & Release Changelog (v2.0 – v2.5)

This document tracks the evolution of game systems, balance updates, content additions, and architectural enhancements introduced across Versions 2.0 through 2.5.

---

## 🐺 Version 2.0 — Wilderness Economy, Architecture & Mathematical Foundations

### 1. Villager Loot Overhaul (Random Principles & Quantity Tiers)
- **Dynamic Loot Distribution**: Defeating human villagers now yields **Gold Coins**, **Meat**, or **Both**, determined via weighted probabilities:
  - $40\%$ chance for Gold Coins only.
  - $30\%$ chance for Meat only.
  - $30\%$ chance for both Meat and Gold Coins.
- **Quantity Tiers**:
  - Small Loot Tier ($65\%$ probability): $2$–$5$ Gold Coins / $1$ Meat piece.
  - More Loot Tier ($35\%$ probability): $6$–$12$ Gold Coins / $2$–$3$ Meat pieces.
- **Expected Value (EV)**: Each villager yields an expected value of $\sim 3.80$ Gold Coins and $\sim 0.92$ Meat pieces.

### 2. Village Cottages Rescaling
- **Architectural Scale**: Village houses rescaled to match realistic human settlement proportions, increasing footprint and collision barriers.
- **Visual Depth**: Improved Y-sorting offsets ensuring characters pass in front of or behind buildings naturally.

### 3. Subterranean Hideout: Werewolf's Lair (`scenes/Lair.tscn`)
- **Cave Hideout Scene**: Added a dedicated single-screen sanctuary separated from the wilderness map.
- **Exterior Entrance Landmark**: Ancient stone archway at Map `(-650, 450)` [World `(-650, -450)`] with animated prompt label (*"Hit Enter to enter the cave"*).
- **Procedural Floor Schema**: $5 \times 9$ stone tile matrix utilizing textures `assets/lair/lair-floor/lair_1..5.jpeg` ($350\text{px}$ in-game scale).
- **Atmosphere & Cosmos**: Pitch-black cosmic backdrop (`#030305`), procedural 240-star twinkling field, and drifting blue cavern mist zones.
- **Exit Portal & Safety Offset**: Left exit arch returning the player safely to wilderness coordinates `(-650, -360)` to prevent immediate re-triggering.

### 4. Mathematical Documentation (`CALCULATIONS.md`)
- Created [`CALCULATIONS.md`](CALCULATIONS.md) as the formal repository for combat formulas, mitigation math, drop rates, tree size distributions, time-to-kill (TTK) metrics, and economy balancing.

---

## 🗡️ Version 2.1 — Hero Details UI & Menu Modularization

### 1. Hero Details Character Sheet (`HeroDetailsWindow.cs`)
- **Interactive Modal**: Toggleable via keypress <kbd>C</kbd> or the HUD menu button; fully draggable across the viewport.
- **Werewolf Portrait**: Features high-resolution $600 \times 650$ Werewolf portrait (`assets/werewolf/solo.png`) anchored on the left.
- **Rustic Aesthetic**: Framed with a wooden sign background (`assets/houses/wooden_sign_flat.png`) with engraved display plate and close button.
- **Live Attribute Display**: Real-time attribute readouts for Current/Max Health, Current/Max Power, Base Damage, Base Defense, Movement Speed, Accuracy, and Evasion (including active buff modifiers).

### 2. HUD Menu Bar (`MenuBar.cs`)
- **Unified Action Bar**: Introduced a secondary modular bar positioned next to the skills bar.
- **Custom Button Sprites**:
  - Hero Details icon using `assets/werewolf/werewolf_head.png` scaled to $50\%$ ($67.5 \times 102.5\text{px}$).
  - Pouch Bag icon for inventory toggling.

---

## 🩸 Version 2.2 — Combat Splatters, Blood Spots & Alchemy Flasks

### 1. Randomized Blood Splatters
- **Combat Feedback**: Replaced standard scratch slash with randomized blood splatter impacts (`assets/blood-hits/1..3.png`), downscaled by $\frac{1}{3}$ for crisp combat impact.

### 2. Ground Blood Spots
- **Living Entity Deaths**: Defeating living targets (Deer, Villagers) drops a temporary blood puddle (`assets/blood-spots/1..3.png`) at their death location.
- **Persistence Lifespan**: Blood spots remain on the ground for exactly $40$ seconds before dissipating.
- **Harvester Mechanics**: Walking near blood spots collects blood if the player holds **Empty Flasks** in their pouch. Each spot fills a flask by $+20\%$ to $+30\%$.

### 3. Flask Tier System & Pouch Logic
- **Visual Fill Tiers**: 5 distinct sprite states (`assets/flasks/blood_flask_0..100.png` at $0\%$, $25\%$, $50\%$, $75\%$, and $100\%$).
- **Stacking Rules**: Empty flasks stack indefinitely. Filled flasks (`BloodFlask_<id>`) are tracked individually with independent fill levels.
- **Flask Drinking**: Right-clicking a blood flask in the pouch modal drinks the blood, recovering health and fury proportional to fill level, returning an empty flask.

---

## 🌲 Version 2.3 — Wilderness Discoveries & Cursor Stability

### 1. Cursor State Stability
- Fixed cursor reset race conditions where looting, harvesting, or destroying objects left the mouse stuck on `grab_o.png` or `interaction_o.png`. Cursors now reliably reset to `normal_o.png` on object freeing or modal closure.

### 2. Quartz Mineral Veins (`QuartzObject.cs`)
- Minable quartz crystal formations (`assets/resources/quartz/quartz_1..3.png`) generating across rocky outcroppings, yielding collectible **Quartz** crystals upon destruction (4 strikes @ 25 HP/hit).

### 3. Hidden Exploration Chests (`ChestObject.cs`)
- Interactive wooden chests (`assets/chests/chest.png`) placed across the wilderness, opening on click to reward resources and rare empty flasks.

---

## 🌿 Version 2.4 — Flora, Choppable Grass & Ground Decals

### 1. Choppable Wild Grass (`GrassObject.cs`)
- 4 natural grass variations (`assets/grass/1..4.png`) spawning across open fields and clearings.
- Choppable with standard attack strikes ($50\text{ HP}$, $25\text{ dmg/hit}$), dropping collectible **Grass** bundles (`grass_drop.png`).
- Ground drop and pouch icon textures normalized to matching visual scale.

### 2. Procedural Terrain Artifacts (`TerrainArtifact.cs`)
- Placed $\sim 130$ natural terrain decals (`assets/terrain-artifacts/1..8.png`) across the open world.
- Integrated spatial distance filtering ($\ge 130\text{px}$) preventing overlap with trees, boulders, chests, quartz, and water bodies.
- Rendered with subtle randomized rotations, horizontal flipping, and background depth (`ZIndex = -5`).

---

## 🏺 Version 2.5 — Subterranean Blood Core & Cavern Recovery Redesign

### 1. Central Blood Core Altar (`BloodCoreObject.cs`)
- **Chamber Centerpiece**: Ancient altar positioned at cave center `(0, 0)` using `assets/cave-objects/blood-core.png`.
- **Reserve Economy**: Starts with **1000 Blood** reserves (saved in `user://werewolves_save.json`).
- **Overhead Percentage Gauge**: Stylized progress bar showing `"{percent}% ({reserves} / {max})"`.
- **Manual Restoration ('Press E')**:
  - Consumes $250$ blood reserves.
  - Restores $+12\text{ Health}$ and $+13\text{ Power}$ ($25\text{ points total}$).
- **Flask Refilling ('Press R')**:
  - Pours blood from carried blood flasks into the core altar ($+250$ blood per $100\%$ flask).
  - Returns an **Empty Flask** into the pouch for reuse.

### 2. Cavern Passive Regeneration Suppression
- **Hard Rule**: When `GameState.Instance.IsInLair == true`, passive over-time health and fury regeneration are **completely disabled** ($0.0\text{ HP/s}$, $0.0\text{ Power/s}$).
- **Active Recovery Only**: Survival in the sanctuary requires active resource management:
  1. Blood Core Altar activations (<kbd>E</kbd>).
  2. Right-click eating **Meat** in pouch (+20 HP, +10 Power).
  3. Right-click drinking **Blood Flasks** in pouch (up to +25 HP & +25 Power).
  4. Executing low-health targets with **Execute Bite** (+20 HP).
