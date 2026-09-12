# Role: World Architect

**Domain**: World coordinate matrix, procedural biome generation, static collision bodies, Y-sorting, and environmental persistence.

---

## 🎯 Objectives & Responsibilities

1. **Coordinate Grid & Routing (`WorldManager.cs`)**:
   - Manage the 11×11 map matrix: `[-5, -5]` to `[5, 5]`.
   - Listen to player screen edge departures (`Player.OnExitedScreenEdge`) and advance map coordinates via `GameState.Instance.SetMapPosition(...)`.
   - Clear existing temporary entities (`_entitiesContainer` and `_lakeContainer`) using `QueueFree()`, ensuring the player node is preserved.

2. **Village Settlement `[5, 5]`**:
   - Background: `simple_path_cross_prim.png` with tiled texture repeat.
   - Ring of 8 cobblestone cottages:
     - Centered at screen center, radius $r = 380\text{px}$.
     - Angular placement: $\theta_i = i \times \frac{2\pi}{8}$.
     - Variant rotation: `(i % 4) + 1`.
   - Central street lantern sprite at center.
   - Peripheral pine trees scattered beyond $r + 120\text{px}$.

3. **Wilderness Procedural Generation**:
   - Background: `pine_tree_forest_ground_1.png` with tiled repeat.
   - Dynamic Lake (25% probability):
     - Random texture between `lake.png` and `lake_1.png`.
     - Static collision body sized $0.85\times$ bounds to prevent walking into water.
     - Registers a exclusion bounding box (`lakeBounds.Grow(40f)`) to block tree/rock overlap.
   - Environmental Scatters:
     - ~60 Pine Trees (15% selectable/harvestable via `IsSelectable = GD.Randf() < 0.15f`).
     - 2 Quarry Boulders (`RockObject`).
     - 30% chance for Wildlife (`Deer`).

4. **Depth & Layering (Y-Sort)**:
   - The entity parent container (`_entitiesContainer`) MUST have `YSortEnabled = true`.
   - Ground backgrounds reside at `ZIndex = -10`.
   - Water/Lakes reside at `ZIndex = -5`.
   - All actors and obstacles sit at `ZIndex = 0` with dynamic Y-sorting.

---

## 🛠️ Code Conventions & Patterns

### Entity Placement with Exclusion Check
```csharp
for (int i = 0; i < objectCount; i++)
{
    Vector2 candidatePos = new Vector2(
        (float)GD.RandRange(minMargin, viewportSize.X - minMargin),
        (float)GD.RandRange(minMargin, viewportSize.Y - minMargin)
    );

    // Skip placement if overlapping existing features (e.g. lake)
    if (lakeBounds.HasValue && lakeBounds.Value.Grow(padding).HasPoint(candidatePos))
    {
        continue;
    }

    var obj = new MyWorldObject { GlobalPosition = candidatePos };
    _entitiesContainer.AddChild(obj);
}
```

---

## ⚠️ Anti-Patterns to Avoid
- **Never delete the player on map transition**: Always filter `child != Player` when clearing the entities container.
- **Never allow obstacles to spawn over the screen border**: Always leave a margin of at least 40–80px from screen edges to prevent player trap locks upon edge entry.
- **Never forget StaticBody collision layers**: Environmental obstacles must block physics movement while allowing mouse pickability for interaction (`InputPickable = true`).
