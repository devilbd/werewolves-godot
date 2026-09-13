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

---

## 🔨 Version 2.6 — Cavern Workshop, Crafting Menu & Storage Chests

### 1. Cavern Crafting System (`CraftingWindow.cs`)
- **Interactive Crafting Modal**: Toggleable via physical hotkey <kbd>B</kbd> or the HUD Action Bar workbench slot; draggable modal displaying all craftable subterranean hideout structures.
- **Card-Based UI**: Individual cards for Storage Chest, Crafting Table, Blood Juicer, and Alchemical Laboratory detailing lore, fixed/freeform placement rules, and material requirements.
- **Real-Time Cost Verification**: Live material tracking (green when satisfied, red when lacking) reflecting pouch inventory and Blood Core reserves.
- **Lair Proximity Enforcement**: Crafting and placement actions require the player to be physically inside the Werewolf's Lair.

### 2. Cavern Storage Chests (`CaveChestObject.cs` & `ChestInventoryWindow.cs`)
- **Crafting Recipe**: $10\text{ Wood Logs} + 5\text{ Stones}$ (unlimited build capacity).
- **Placement Mode**:
  - Semi-transparent ghost preview follows cursor in world space with real-time chamber boundary and obstacle validation.
  - Tint feedback: green (`Color(0.4, 1.0, 0.4, 0.75)`) when valid, red (`Color(1.0, 0.3, 0.3, 0.75)`) when invalid (near Blood Core, walls, or other objects).
  - Left-click commits placement and deducts resources; Right-click or <kbd>Esc</kbd> cancels.
- **Dynamic Relocation**:
  - Any placed chest can be moved at any time by clicking the **"Move Chest"** button inside the Chest Inventory modal.
  - Temporarily removes the chest from the world into placement mode without consuming additional materials.
- **Dual Visual States**:
  - Renders as [`chest_closed.png`](assets/chests/chest_closed.png) in world.
  - Dynamically transitions to [`chest_opened.png`](assets/chests/chest_opened.png) while the chest inventory modal is open.
- **Chest Storage Interface**:
  - Dedicated $600 \times 450\text{px}$ window utilizing [`chest_inventory.png`](assets/chests/chest_inventory.png) background.
  - 8-column grid displaying stored items with stack counts, tooltips, and click-to-retrieve mechanics.
  - **"Deposit All"** & **"Take All"** buttons for rapid batch transfers between pouch and chest.
  - **Bi-Directional Shift+Click**: Shift-clicking any item in Pouch or Chest instantly transfers the entire stack.
  - Automatic side-by-side positioning of PouchWindow and ChestInventoryWindow upon opening.

### 3. Fixed Workshop Installations (`CaveStaticObject.cs`)
- **Crafting Table** (Top Left at `X = -700, Y = -520`):
  - Sprite: `assets/cave-objects/crafting-table.png` (scale $0.35$).
  - Recipe: $25\text{ Wood Logs} + 15\text{ Stones}$.
  - Interaction: Left-clicking or approaching opens the Crafting Window.
- **Blood Juicer** (Top Center at `X = 0, Y = -520`):
  - Sprite: `assets/cave-objects/blood-juicer.png` (scale $0.35$).
  - Recipe: $30\text{ Stones} + 20\text{ Quartz} + 100\text{ Blood}$ (drained directly from Blood Core reserves).
  - Purpose: Subterranean refinery for essence synthesis and life fluid distillation.
- **Alchemical Laboratory** (Top Right at `X = 700, Y = -520`):
  - Sprite: `assets/cave-objects/laboratory.png` (scale $0.35$).
  - Recipe: $25\text{ Stones} + 25\text{ Quartz} + 15\text{ Grass}$.
  - Purpose: Distillation and alembic research apparatus for potent concoctions.

### 4. HUD Action Bar Expansion (`ActionBar.cs`)
- Expanded Action Bar to 3 uniform slots ($258 \times 120\text{px}$):
  - Slot 1: **Hero Details** (<kbd>C</kbd>) with Werewolf portrait.
  - Slot 2: **Inventory Pouch** (<kbd>P</kbd>) with pouch bag icon.
  - Slot 3: **Cave Crafting** (<kbd>B</kbd>) with workbench icon.

### 5. State Persistence (`SaveManager.cs`)
- Extended `user://werewolves_save.json` schema:
  - `craftedCaveObjects`: List of crafted unique static structures (`"CraftingTable"`, `"BloodJuicer"`, `"Laboratory"`).
  - `caveChests`: Array of placed chests containing unique IDs, world coordinates `(posX, posY)`, and stored items dictionaries.
- Restores all crafted structures and placed chests upon loading into the cave.

---

## Version 2.7 — Freeform Chest Storage, Stack Splitting Modal & Quit Persistence

### 1. Freeform Chest Inventory Redesign (`ChestInventoryWindow.cs`)
- **No Slot Grids**: Removed static slot containers and empty panel borders. The chest compartment is now a clean freeform canvas ($530 \times 365\text{px}$) matching [`PouchWindow.cs`](file:///run/media/devilbd/d/Development/godot-dev/werewolves-godot/Scripts/UI/PouchWindow.cs).
- **Freeform Item Dragging**:
  - Items can be freely dragged and positioned anywhere within the chest interior.
  - Custom item coordinates (`PosX`, `PosY`) persist per-chest in `CaveChestData.Items` via `GameState.Instance.UpdateChestItemPosition()`.
  - Removed obsolete "Deposit All" and "Take All" buttons in favor of precision individual and split-stack controls.

### 2. Stack Split Selector Modal (`ItemSplitModal.cs`)
- **Interactive Quantity Picker**:
  - Holding <kbd>Shift</kbd> and clicking any stack ($N > 1$) in either the Chest or the Pouch opens a dedicated quantity modal.
  - Interactive slider (`HSlider`) spanning $1$ to $N$.
  - Precision stepper buttons `[-]` and `[+]` with live numeric readouts.
  - Quick preset buttons: `[ 1 ]` (minimum), `[ Half (N/2) ]`, `[ All (N) ]`.
  - Keyboard shortcuts: <kbd>Enter</kbd> to confirm transfer, <kbd>Escape</kbd> to cancel.
- **Bi-Directional Transfer Flow**:
  - **Chest $\to$ Pouch**: Click grabs $1$; <kbd>Shift</kbd> + Click opens split dialog to grab custom quantity.
  - **Pouch $\to$ Chest**: Right-click deposits $1$; <kbd>Shift</kbd> + Click opens split dialog to deposit custom quantity.

### 3. Save-on-Quit & Subterranean State Persistence
- **Cave State Tracking (`SaveManager.cs` & `GameState.cs`)**:
  - Added `isInLair` boolean property to `SaveData`.
  - Intercepts Godot application termination events (`NotificationWMCloseRequest` and `NotificationPredelete`) in `GameState._Notification` to commit the exact werewolf coordinates and cave state to `user://werewolves_save.json`.
  - Removed accidental `GameState.Instance.IsInLair = false` overwrite in `LairManager._ExitTree()`.
- **Seamless Startup Routing (`Main.cs` & `LairManager.cs`)**:
  - Upon game launch, `Main.cs` inspects `GameState.Instance.IsInLair`. If true, it immediately routes directly to `res://scenes/Lair.tscn`.
  - `LairManager.cs` restores the werewolf to `SaveManager.LoadedPlayerPosition`, resuming the player exactly where they stood inside the cave hideout.

---

## 🗺️ Version 2.8 — Alt-Key Ground Loot Names, Death Scatter Fix & World/Cavern Map System

### 1. Living Entity Drop Scatter & Overlap Bug Fix
- **Scatter Offset**: Resolved bug where blood spot pools would spawn at the exact same location as dropped Meat/Gold and intercept user mouse clicks.
  - Added a randomized offset ($\pm 24\text{px}$ X, $\pm 16\text{px}$ Y) to drops in `Deer.cs` and `Villager.cs`.
  - Meat and currency pickups now land separated from the central blood pool, allowing easy targeting.

### 2. ARPG-Style Alt-Key Loot Highlight System
- **Key Binding**: Holding <kbd>Alt</kbd> (Left or Right) dynamically reveals floating, clickable nameplates above all ground items and blood pools in the world.
- **Visual Nameplates**:
  - `[ ItemName (Count) ]` buttons color-coded by rarity (Gold = Yellow, Meat = Crimson, Quartz = Lavender, Flasks = Cyan, Resources = Green) positioned at $Y = -38\text{px}$.
  - `[ Blood Spot ({seconds}s) ]` crimson badge with live lifetime countdown positioned at $Y = -62\text{px}$ to prevent label collisions.
- **Direct Collection**: Clicking either label directly collects the corresponding item or fills an empty flask, completely bypassing 2D collision occlusion.
- **Event-Driven Architecture**: `GameState.Instance.OnLootLabelsToggled` with continuous key state polling in `HUDManager._Process`.

### 3. World & Cavern Map System (`MapWindow.cs`)
- **Hotkey & Action Bar Integration**: Toggleable via keypress <kbd>M</kbd> (`toggle_map`), Escape key dismissal, or the new 4th slot on the HUD Action Bar featuring an antique compass rose icon.
- **Wilderness Map View**:
  - Full $10,000 \times 10,000$ open-world Cartesian coordinate space with major grid markers every 1000m.
  - Permanent landmark pins: Awakening Grove `(0, 0)`, Werewolf's Lair Entrance `(-650, 450)`, The Village `(2500, 1800)`, Quarry Hills `(2200, -2200)`, Silent Lake, and Misty Lake.
  - **Visible Perception Radar Range ($R = 1800\text{m}$)**: A glowing radial perception aura centered on the player that scans and renders real-time entity blips within sensory range:
    - Humans / Villagers (Gold)
    - Wildlife / Deer (Emerald)
    - Treasure Chests (Golden Amber)
    - Minable Quartz Clusters (Purple)
    - Ground Loot & Collectibles (Cyan)
    - Harvestable Blood Spots (Crimson)
  - Interactive tooltips showing entity name, distance, and relative bearing.
  - Pan & Zoom controls (0.035x to 0.35x), "Center Player" button, and layer filter toggles (Landmarks, Creatures, Resources, Loot).
- **Cavern Map View**:
  - Automatically switches when inside the hideout (`GameState.Instance.IsInLair == true`).
  - Displays cavern borders, Central Blood Core altar (with live blood reserves %), fixed workshop installations (Crafting Table, Blood Juicer, Laboratory), placed storage chests, and exit portal.


