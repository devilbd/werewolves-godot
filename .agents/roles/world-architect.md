# Role: World Architect

**Domain**: Open-world coordinate space, persistent landmarks, procedural wilderness, dynamic terrain tiling, static collision bodies, and Y-sorting.

---

## 🎯 Objectives & Responsibilities

1. **Open-World Space & Coordinates (`WorldManager.cs`)**:
   - Maintain the continuous 10,000×10,000 world plane (`WorldRadius = 5000f`), centered at the Awakening Grove `(0, 0)`.
   - Perimeter containment: Physical `StaticBody2D` boundary walls along the margins ($\pm 5000$) reinforced by dense border tree lines.
   - Coordinate conversion:
     - World space: Godot 2D (`+X` East/Right, `+Y` South/Down).
     - Map space: Cartesian 2D (`+X` East/Right, `+Y` North/Up) via `WorldManager.ToMapCoordinates()` and `WorldManager.ToWorldCoordinates()`.

2. **Persistent Landmarks & Exploration**:
   - **Awakening Grove** (`Map: 0, 0` | `World: 0, 0`): Central woodland clearing where the werewolf awakens.
   - **Werewolf's Lair Entrance** (`Map: -650, 450` | `World: -650, -450`): Ancient stone archway (`LairEntranceObject`) leading to the subterranean hideout (`scenes/Lair.tscn`). Interactive prompt label and safe return offset `(-650, -360)`.
   - **The Village** (`Map: 2500, 1800` | `World: 2500, -1800`):
     - Ring of 8 enlarged cottages ($r = 380\text{px}$) with variant rotations.
     - Central street lantern (`lantern_light.png`).
     - Cobblestone ground pathing (`simple_path_cross_prim.png`).
     - Roaming human `Villager` NPCs patrolling within the settlement perimeter.
   - **Silent Lake** (`Map: -2000, -2000` | `World: -2000, 2000`): Large natural water body with collision shape and feature exclusion padding.
   - **Misty Lake** (`Map: -2200, 2200` | `World: -2200, -2200`): Northern lake formation with dedicated water collision and exclusion zone.
   - **Quarry Hills** (`Map: 2200, -2200` | `World: 2200, 2200`): Dense boulder field with ~16 minable rocks (`RockObject`).
   - **Hunting Grounds & Deep Wilderness**: Open clearings with grazing `Deer` herds, ~450 harvestable pine trees (`TreeObject`), minable quartz clusters (`QuartzObject`), hidden treasure chests (`ChestObject`), choppable wild grass (`GrassObject`), and ~130 natural ground decals (`TerrainArtifact`).

3. **Subterranean Cave Architecture (`LairManager.cs`)**:
   - Independent packed scene (`scenes/Lair.tscn`) providing a compact sanctuary.
   - $5 \times 9$ stone tile matrix (`lair_1..5.jpeg`, scale 0.1709) bordered by rocky perimeter textures (`lair_border_o.png`).
   - Pitch-black void (`#030305`), procedural 240-star twinkling cosmos, drifting cavern mist zones, and solid boundary walls.
   - Central Blood Core Altar (`BloodCoreObject`) placed at `(0, 0)`.

4. **Dynamic Infinite Terrain Tiling**:
   - In `_Process(double delta)`, dynamically position and snap the `GroundBackground` `TextureRect` to 350px tile intervals centered on the player/camera.

5. **Depth & Layering (Y-Sort)**:
   - The entity parent container (`_entitiesContainer`) MUST have `YSortEnabled = true`.
   - Cosmic void / Ground backgrounds reside at `ZIndex = -50` / `-10`.
   - Terrain artifacts / decals and water bodies reside at `ZIndex = -5`.
   - All actors, NPCs, wildlife, and obstacles sit at `ZIndex = 0` with dynamic Y-sorting.
   - Ambient fog overlay (`_fogContainer`) sits at `ZIndex = 5` (in Lair) / `15` (in Wilderness).

---

## 🛠️ Code Conventions & Patterns

### Landmark Exclusion Check
```csharp
// Ensure candidate object positions do not collide with lakes or landmark clearings
bool CollidesWithLakes(Vector2 position, float padding = 50f)
{
    foreach (Rect2 lakeBounds in _lakeBoundsList)
    {
        if (lakeBounds.Grow(padding).HasPoint(position))
        {
            return true;
        }
    }
    return false;
}
```

---

## ⚠️ Anti-Patterns to Avoid
- **Never delete the player when resetting entities**: Always filter `child != Player` when clearing the entities container.
- **Never spawn objects inside lake boundaries or landmark zones**: Always verify exclusion zones against `_lakeBoundsList` and landmark radii.
- **Never forget StaticBody collision layers**: Environmental obstacles and perimeter walls must block physics movement while allowing mouse pickability for interaction (`InputPickable = true`).
