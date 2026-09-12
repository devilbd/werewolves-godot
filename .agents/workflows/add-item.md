# Workflow: Adding a New Resource or Inventory Item

This playbook outlines how to create new collectible materials, drops, or pouch inventory items.

---

## 📋 Checklist

- [ ] **Step 1**: Place Textures in `assets/` (Ground drop and collected pouch icon)
- [ ] **Step 2**: Map Drop Texture in `DroppedLoot.cs`
- [ ] **Step 3**: Map Pouch Icon in `PouchWindow.cs`
- [ ] **Step 4**: Hook Spawner (Mob death, tree chop, or rock quarry)
- [ ] **Step 5**: Test Inventory Persistence via `SaveManager.cs`
- [ ] **Step 6**: Verify Build & Compilation

---

## Detailed Steps

### Step 1: Asset Preparation
Ensure two textures exist in `assets/`:
1. Ground pickup icon: e.g. `res://assets/herb_o.png`
2. Collected pouch icon: e.g. `res://assets/herb_collected_o.png`

### Step 2: Map Drop Texture in `DroppedLoot.cs`
Add the item key to the switch block in `DroppedLoot.cs`:
```csharp
Texture2D? tex = ItemType switch
{
    "Logs" => GD.Load<Texture2D>("res://assets/logs_o.png"),
    "Stones" => GD.Load<Texture2D>("res://assets/rock_stones_loot_o.png"),
    "Meat" => GD.Load<Texture2D>("res://assets/meat_o.png"),
    "Herb" => GD.Load<Texture2D>("res://assets/herb_o.png"),
    _ => GD.Load<Texture2D>("res://assets/logs_o.png")
};
```

### Step 3: Map Inventory Icon in `PouchWindow.cs`
Add the collected icon path in `PouchWindow.RefreshItems()`:
```csharp
string iconPath = itemName switch
{
    "Logs" => "res://assets/logs_collected_o.png",
    "Stones" => "res://assets/rock_stones_loot_collected_o.png",
    "Meat" => "res://assets/meat_collected_o.png",
    "Herb" => "res://assets/herb_collected_o.png",
    _ => "res://assets/logs_collected_o.png"
};
```

### Step 4: Hook Spawner
Spawn the item when an entity is destroyed or an interactive object is harvested:
```csharp
var loot = DroppedLoot.Instantiate("Herb", GlobalPosition);
GetParent()?.AddChild(loot);
```

### Step 5: Persistence Validation
Items are automatically saved into `user://werewolves_save.json`:
- `GameState.Instance.AddPouchItem("Herb", 1)` registers the entry and calls `SaveManager.SaveGame()`.
- Dragging the herb icon inside the pouch updates its `PosX` and `PosY` in `GameState.Instance.UpdatePouchItemPosition(...)`.

### Step 6: Build Verification
```bash
dotnet build Werewolves.sln
```
Ensure 0 warnings and 0 errors.
