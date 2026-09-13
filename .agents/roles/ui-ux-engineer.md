# Role: UI/UX Engineer

**Domain**: HUD layout, CanvasLayer, custom CanvasItem rendering (`_Draw`), Diablo orbs, inventory modals, and input event routing.

---

## 🎯 Objectives & Responsibilities

1. **HUD Management (`HUDManager.cs`)**:
   - Reside in a top-level `CanvasLayer` node.
   - Wire responsive repositioning via `GetViewport().SizeChanged`:
     - Top Center: `AreaLabel`
     - Top Left: `TargetPanel`
     - Bottom Left: `HealthOrb`
     - Bottom Right: `PowerOrb`
     - Bottom Center: `StatsPanel` and `MenuBar`
     - Center (Modals): `PouchWindow` and `HeroDetailsWindow`
   - Bind to strongly typed events from `GameState.Instance` (`OnHealthChanged`, `OnPowerChanged`, `OnPositionChanged`, `OnCooldownUpdated`, `OnTargetChanged`, `OnPouchToggled`, `OnHeroDetailsToggled`, `OnBloodCoreReservesChanged`).

2. **Custom CanvasItem Rendering (`OrbGauge.cs`)**:
   - Use `[Tool]` with `_Draw()` and `QueueRedraw()` for real-time visual updates.
   - Maintain liquid fill math:
     - Compute chord angle $\theta$ and arc slice for values between 1% and 99%.
     - Triangulate polygon points via `Geometry2D.TriangulatePolygon`.
   - Bubble buoyancy simulation:
     - Update bubble array in `_Process()` with upward drift and wrap-around reset.
     - Clip bubbles so they only render within the liquid region.
   - Overlay border frame texture (`health_ring.png` or `power_ring.png`) scaled with `RingRadiusOffset = 44f` (giving $r = 126\text{px}$) to cleanly encapsulate the orb fluid.

3. **Draggable Modals (`PouchWindow.cs` & `HeroDetailsWindow.cs`)**:
   - **`PouchWindow`**:
     - Window dragging clamped to viewport dimensions (`0` to `viewport.Size - window.Size`).
     - Freeform item placement within `_itemsArea` with `ClipContents = true`.
     - Support uniform 44×44px slot items while preserving texture aspect ratio.
     - Numerical stack badging and 5-tier flask visuals (`_0`, `_25`, `_50`, `_75`, `_100`).
     - Right-click consumption: Eat Meat or drink Blood Flask with instant feedback.
   - **`HeroDetailsWindow`**:
     - Toggleable via <kbd>C</kbd> or HUD menu button; draggable across viewport.
     - Anchors 600×650 Werewolf portrait (`solo.png`) on the left on a wooden sign background (`wooden_sign_flat.png`).
     - Displays character plate and live combat stats (Health, Power, Damage, Defense, Speed, Accuracy, Evasion).
   - **Input Safety**: Prevent UI clicks from leaking into game world via `GetViewport().SetInputAsHandled()`.

4. **HUD Menu Bar (`MenuBar.cs`) & World UI**:
   - Secondary action bar beside the skills bar housing the Hero Details icon (`werewolf_head.png` scaled 50%) and Pouch Bag icon.
   - Overhead progress bars: e.g. `BloodCoreObject` styled percentage bar showing reserves and interaction prompts.

5. **Contextual Cursors**:
   - `assets/cursors/normal_o.png`: Default game arrow.
   - `assets/cursors/interaction_o.png`: Hovering over attackable / harvestable targets.
   - `assets/cursors/grab_o.png`: Hovering over ground loot or draggable handles.
   - Always guarantee cursor reset to `normal_o.png` when leaving hovered controls, picking items, changing scenes, or when entities are destroyed.

---

## 🛠️ Code Conventions & Patterns

### Event Subscription Pattern in Controls
```csharp
public override void _Ready()
{
    // 1. Initialize UI elements
    SetupControls();

    // 2. Subscribe to GameState events
    GameState.Instance.OnHealthChanged += HandleHealthChanged;
}

public override void _ExitTree()
{
    // 3. Clean up event subscriptions to prevent memory leaks
    if (GameState.Instance != null)
    {
        GameState.Instance.OnHealthChanged -= HandleHealthChanged;
    }
}
```

### Consuming Input Events
```csharp
private void OnItemGuiInput(InputEvent @event)
{
    if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
    {
        if (mb.Pressed)
        {
            StartDragging();
            GetViewport().SetInputAsHandled(); // Prevents click passing to world
        }
    }
}
```

---

## ⚠️ Anti-Patterns to Avoid
- **Never perform heavy allocations in `_Draw()`**: Pre-allocate arrays and reuse points lists where possible.
- **Never hardcode screen coordinates**: Always derive positions relative to `GetViewport().GetVisibleRect().Size` or use Godot control anchors/presets.
- **Never forget `MouseFilter`**: Set interactive UI elements to `MouseFilter = MouseFilterEnum.Stop` so world clicks are blocked behind modals.
