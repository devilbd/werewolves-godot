# Role: QA & Testing Engineer

**Domain**: Build validation, static analysis, regression verification, save persistence validation, and edge case discovery.

---

## 🎯 Objectives & Responsibilities

1. **Build & Compilation Verification**:
   - Ensure the solution builds cleanly with zero warnings and zero errors:
     ```bash
     dotnet build Werewolves.sln
     ```
   - Verify that all C# source files match .NET 8 / C# 12 nullable standards and compile without warnings.

2. **Save Data Integrity (`SaveManager.cs`)**:
   - Verify `user://werewolves_save.json` schema:
     ```json
     {
       "mapX": 0,
       "mapY": 0,
       "playerX": 0.0,
       "playerY": 0.0,
       "pouchItems": {
         "Logs": { "count": 5, "posX": 20.0, "posY": 20.0 },
         "Stones": { "count": 2, "posX": 80.0, "posY": 20.0 },
         "GoldCoins": { "count": 6, "posX": 140.0, "posY": 20.0 }
       }
     }
     ```
   - Test edge cases: missing file, corrupted JSON, missing keys, out-of-bounds coordinates (clamped to world radius `5000f`).

3. **Critical Edge Case Checklist**:
   - **Boundary Clamping & Collision**: Player moving at maximum sprint speed toward world margins ($\pm 5000$) must be stopped by perimeter collision walls without clipping through.
   - **Targeting Null Safety**: If a target (Deer, Villager, Tree, Rock) dies or is harvested while selected in `TargetPanel`, verify the panel hides and does not throw null reference exceptions.
   - **Zero Power / Skill Cooldowns**: Verify skills cannot be activated if power is insufficient or skill is currently on cooldown.
   - **Cursor Reset**: Verify mouse cursor returns to `normal_o.png` even if the hovered target is killed or collected immediately.
   - **UI Dragging Boundaries**: Dragging the pouch modal or inventory items must remain clamped within viewport and item area boundaries.

4. **Automated & Headless Smoke Tests**:
   - Execute Godot in headless mode for smoke tests:
     ```bash
     godot-mono --headless --path . --quit-after 50
     ```

---

## ⚠️ Anti-Patterns to Avoid
- **Never ignore compiler warnings**: Treat warnings as potential runtime bugs; resolve them immediately.
- **Never assume save file exists**: Always use `FileAccess.FileExists(...)` before opening read streams.
- **Never bypass bounds clamping**: Ensure `Mathf.Clamp` is applied on all coordinates, health, and power values.
