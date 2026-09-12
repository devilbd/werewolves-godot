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
     - Bottom Center: `StatsPanel` and `ActionBar`
     - Center (Modal): `PouchWindow`
   - Bind to strongly typed events from `GameState.Instance` (`OnHealthChanged`, `OnPowerChanged`, `OnMapChanged`, `OnCooldownUpdated`, `OnTargetChanged`, `OnPouchToggled`).

2. **Custom CanvasItem Rendering (`OrbGauge.cs`)**:
   - Use `[Tool]` with `_Draw()` and `QueueRedraw()` for real-time visual updates.
   - Maintain liquid fill math:
     - Compute chord angle $\theta$ and arc slice for values between 1% and 99%.
     - Triangulate polygon points via `Geometry2D.TriangulatePolygon`.
   - Bubble buoyancy simulation:
     - Update bubble array in `_Process()` with upward drift and wrap-around reset.
     - Clip bubbles so they only render within the liquid region.
   - Overlay border frame texture (`health_ring.png` or `power_ring.png`) and fallback golden arc.

3. **Draggable Pouch Modal (`PouchWindow.cs`)**:
   - Provide window dragging clamped to viewport dimensions (`0` to `viewport.Size - window.Size`).
   - Freeform item placement:
     - Render items within `_itemsArea` with `ClipContents = true`.
     - Allow individual item dragging with local mouse offset tracking.
     - On drag release, commit coordinates to `GameState.Instance.UpdatePouchItemPosition(...)`.
   - Prevent UI clicks from leaking into game world via `GetViewport().SetInputAsHandled()`.

4. **Contextual Cursors**:
   - `assets/cursors/normal_o.png`: Default game arrow.
   - `assets/cursors/interaction_o.png`: Hovering over attackable / harvestable targets.
   - `assets/cursors/grab_o.png`: Hovering over ground loot or draggable handles.
   - Always guarantee cursor reset when leaving hovered controls or when entities are destroyed.

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
