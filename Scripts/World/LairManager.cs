using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;
using Werewolves.UI;

namespace Werewolves.World;

public partial class LairManager : Node2D
{
    [Export] public Werewolf? Player { get; set; }

    private Node2D _floorContainer = null!;
    private Node2D _entitiesContainer = null!;
    private Area2D _exitArea = null!;
    private HUDManager? _hud;
    private bool _isTransitioning = false;
    private readonly Dictionary<int, Texture2D> _floorTextures = new();

    // Floor tile parameters matching pine_tree_forest_ground_1.png (350x350)
    public const float TileSize = 350f;
    public const float SourceTextureSize = 2048f;
    public const float TileScale = TileSize / SourceTextureSize; // 350 / 2048 ~ 0.1709

    /// <summary>
    /// User specified schema for the lair floor:
    ///     2 3 4 5 4 3 2
    ///     1 1 1 1 1 1 1
    /// 1 1 1 2 3 4 5 4 2
    ///     1 1 1 1 1 1 1
    ///     2 3 4 5 4 3 2
    /// 0 denotes empty space (outside the cave boundaries)
    /// </summary>
    private static readonly int[][] FloorSchema = new int[][]
    {
        new int[] { 0, 0, 2, 3, 4, 5, 4, 3, 2 },
        new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1 },
        new int[] { 1, 1, 1, 2, 3, 4, 5, 4, 2 },
        new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1 },
        new int[] { 0, 0, 2, 3, 4, 5, 4, 3, 2 },
    };

    private Label _exitPromptLabel = null!;
    private bool _isPlayerInExitArea = false;
    private float _exitBlinkTimer = 0f;

    // Chest Placement Preview State
    private Node2D? _placementGhost;
    private Sprite2D? _ghostSprite;
    private Label? _ghostPrompt;

    public override void _Ready()
    {
        GameState.Instance.IsInLair = true;

        // 0. Cosmic dark background & faintly gleaming starfield
        BuildBackgroundAndStarfield();

        // 1. Reset mouse cursor to normal
        CursorManager.ResetNormal();

        // 2. Preload the 5 textures from lair-floor (lair_1 through lair_5)
        _floorTextures.Clear();
        for (int i = 1; i <= 5; i++)
        {
            var tex = GD.Load<Texture2D>($"res://assets/lair/lair-floor/lair_{i}.jpeg");
            if (tex != null)
            {
                _floorTextures[i] = tex;
            }
        }

        // 3. Build multi-textured floor grid using the custom schema
        _floorContainer = GetNodeOrNull<Node2D>("FloorContainer");
        if (_floorContainer == null)
        {
            _floorContainer = new Node2D { Name = "FloorContainer", ZIndex = -10 };
            AddChild(_floorContainer);
        }
        BuildFloorGrid();

        // 3.1 Build rocky terrain borders using lair_border_o.png
        BuildTerrainBorders();

        // 4. Physical Wall Boundaries matching the schema with single solid right wall
        BuildBoundaries();

        // 5. Entities Container with Y-Sort
        _entitiesContainer = GetNodeOrNull<Node2D>("Entities");
        if (_entitiesContainer == null)
        {
            _entitiesContainer = new Node2D
            {
                Name = "Entities",
                YSortEnabled = true
            };
            AddChild(_entitiesContainer);
        }

        // 6. Werewolf Player
        Player = GetNodeOrNull<Werewolf>("Entities/Werewolf");
        if (Player == null)
        {
            var werewolfScene = GD.Load<PackedScene>("res://scenes/Entities/Werewolf.tscn");
            if (werewolfScene != null)
            {
                Player = werewolfScene.Instantiate<Werewolf>();
            }
            else
            {
                Player = new Werewolf();
            }
            Player.Name = "Werewolf";
            _entitiesContainer.AddChild(Player);
        }

        // Spawn player: restore saved position if valid inside cave, else spawn at entrance
        Vector2 spawnPos = SaveManager.LoadedPlayerPosition;
        if (spawnPos == Vector2.Zero || spawnPos.X < -1975f || spawnPos.X > 1275f || spawnPos.Y < -950f || spawnPos.Y > 950f)
        {
            spawnPos = new Vector2(-1650f, 0f);
        }
        Player.GlobalPosition = spawnPos;
        GameState.Instance.PlayerPosition = Player.GlobalPosition;
        GameState.Instance.IsInLair = true;

        var playerSprite = Player.GetNodeOrNull<Sprite2D>("Sprite2D");
        if (playerSprite != null)
        {
            playerSprite.FlipH = false; // Facing right towards the rest of the cave
        }

        // Set camera limits to encompass the lair chamber and side corridor
        Player.SetCameraLimits(-1975, -950, 1275, 950);

        // 6.1 Blood Core Altar at the center of the cave chamber
        BuildBloodCore();

        // 6.2 Workshop installations (Crafting Table, Blood Juicer, Laboratory) & Storage Chests
        BuildCraftedStaticObjects();
        BuildPlacedChests();

        GameState.Instance.OnStaticObjectCrafted += OnStaticObjectCrafted;
        GameState.Instance.OnChestPlaced += OnChestPlaced;
        GameState.Instance.OnChestPlacementModeChanged += OnChestPlacementModeChanged;

        // 7. Cave Entrance Visual Landmarks & Atmospheric Fog (no stones, woods, or materials in cave)
        BuildEntranceVisuals();
        BuildFog();

        // 7.1 Fog dashed border overlay
        var borderOverlay = GetNodeOrNull<Effects.FogDashedBorderOverlay>("FogDashedBorderOverlay");
        if (borderOverlay == null)
        {
            borderOverlay = new Effects.FogDashedBorderOverlay { Name = "FogDashedBorderOverlay" };
            AddChild(borderOverlay);
        }

        // 8. Exit Area with Enter-key interaction at the left '1 1' entrance
        BuildExitArea();

        // 9. HUD
        _hud = GetNodeOrNull<HUDManager>("HUD");
        if (_hud == null)
        {
            _hud = new HUDManager
            {
                Name = "HUD",
                Player = Player
            };
            AddChild(_hud);
        }
        else
        {
            _hud.Player = Player;
        }
    }

    private void BuildBackgroundAndStarfield()
    {
        // 1. Deep dark background covering the entire void surrounding the cave
        var bg = GetNodeOrNull<ColorRect>("LairBackground");
        if (bg == null)
        {
            bg = new ColorRect
            {
                Name = "LairBackground",
                ZIndex = -50,
                Position = new Vector2(-3600f, -2400f),
                Size = new Vector2(7200f, 4800f),
                Color = new Color(0.010f, 0.012f, 0.018f, 1f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(bg);
        }

        // 2. Faintly gleaming starfield in the background void
        var starfield = GetNodeOrNull<LairStarfield>("LairStarfield");
        if (starfield == null)
        {
            starfield = new LairStarfield { Name = "LairStarfield" };
            starfield.Initialize(new Rect2(-2400f, -1400f, 4200f, 2800f), 240);
            AddChild(starfield);
        }

        // Set engine clear color for deep dark immersion
        RenderingServer.SetDefaultClearColor(new Color(0.010f, 0.012f, 0.018f, 1f));
    }

    private void BuildFog()
    {
        var fogContainer = GetNodeOrNull<Node2D>("FogContainer");
        if (fogContainer == null)
        {
            fogContainer = new Node2D
            {
                Name = "FogContainer",
                ZIndex = 5 // Drifts gently over the floor tiles and feet of entities
            };
            AddChild(fogContainer);

            // Subtle ethereal cave mist in the main chamber
            var chamberFog = FogZone.Instantiate(
                position: new Vector2(0f, 0f),
                radius: 950f,
                color: new Color(0.72f, 0.82f, 0.95f, 0.22f),
                density: 0.25f,
                coverage: 0.40f,
                cycleSpeed: 0.025f
            );
            fogContainer.AddChild(chamberFog);

            // Gentle mist patch drifting through the left corridor
            var corridorFog = FogZone.Instantiate(
                position: new Vector2(-1350f, 0f),
                radius: 520f,
                color: new Color(0.72f, 0.82f, 0.95f, 0.18f),
                density: 0.20f,
                coverage: 0.34f,
                cycleSpeed: 0.03f
            );
            fogContainer.AddChild(corridorFog);
        }
    }

    private void BuildBloodCore()
    {
        var existing = _entitiesContainer.GetNodeOrNull<BloodCoreObject>("BloodCore")
            ?? _entitiesContainer.GetNodeOrNull<BloodCoreObject>("BloodCoreObject");
        if (existing == null)
        {
            var coreScene = GD.Load<PackedScene>("res://scenes/Entities/BloodCoreObject.tscn");
            BloodCoreObject core = coreScene != null
                ? coreScene.Instantiate<BloodCoreObject>()
                : BloodCoreObject.Instantiate(Vector2.Zero);
            core.Name = "BloodCore";
            core.GlobalPosition = Vector2.Zero;
            _entitiesContainer.AddChild(core);
        }
    }

    private void BuildCraftedStaticObjects()
    {
        foreach (var objType in GameState.Instance.CraftedStaticObjects)
        {
            SpawnStaticObject(objType);
        }
    }

    private void SpawnStaticObject(string objType)
    {
        string nodeName = $"Static_{objType}";
        if (_entitiesContainer.GetNodeOrNull<Node2D>(nodeName) != null) return;

        if (!GameState.Recipes.TryGetValue(objType, out var recipe)) return;

        var obj = CaveStaticObject.Instantiate(objType, recipe.StaticPosition);
        obj.Name = nodeName;
        _entitiesContainer.AddChild(obj);
    }

    private void OnStaticObjectCrafted(string objType)
    {
        SpawnStaticObject(objType);
    }

    private void BuildPlacedChests()
    {
        foreach (var kvp in GameState.Instance.CaveChests)
        {
            SpawnChest(kvp.Value.Id, new Vector2(kvp.Value.PosX, kvp.Value.PosY));
        }
    }

    private void SpawnChest(string chestId, Vector2 position)
    {
        string nodeName = $"CaveChest_{chestId}";
        var existing = _entitiesContainer.GetNodeOrNull<CaveChestObject>(nodeName);
        if (existing != null)
        {
            existing.GlobalPosition = position;
            existing.Visible = true;
            return;
        }

        var scene = GD.Load<PackedScene>("res://scenes/Entities/CaveChestObject.tscn");
        var chest = scene != null ? scene.Instantiate<CaveChestObject>() : CaveChestObject.Instantiate(chestId, position);
        chest.ChestId = chestId;
        chest.GlobalPosition = position;
        chest.Name = nodeName;
        _entitiesContainer.AddChild(chest);
    }

    private void OnChestPlaced(string chestId, Vector2 position)
    {
        SpawnChest(chestId, position);
    }

    private void OnChestPlacementModeChanged(bool isPlacing, string? chestId)
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        if (isPlacing)
        {
            // If moving existing chest, temporarily hide it from world
            if (chestId != null)
            {
                var existingNode = _entitiesContainer.GetNodeOrNull<CaveChestObject>($"CaveChest_{chestId}");
                if (existingNode != null) existingNode.Visible = false;
            }

            if (_placementGhost == null)
            {
                _placementGhost = new Node2D
                {
                    Name = "PlacementGhost",
                    ZIndex = 50
                };

                var tex = GD.Load<Texture2D>("res://assets/chests/chest_closed.png");
                _ghostSprite = new Sprite2D
                {
                    Name = "GhostSprite",
                    Texture = tex,
                    Scale = new Vector2(0.30f, 0.30f),
                    Offset = new Vector2(0, -tex.GetHeight() * 0.45f),
                    Modulate = new Color(0.4f, 1.0f, 0.4f, 0.75f)
                };
                _placementGhost.AddChild(_ghostSprite);

                _ghostPrompt = new Label
                {
                    Name = "GhostPrompt",
                    Text = "Left-Click: Confirm Placement\nRight-Click / Esc: Cancel",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Position = new Vector2(-150, -95),
                    Size = new Vector2(300, 36)
                };
                _ghostPrompt.AddThemeFontSizeOverride("font_size", 12);
                _ghostPrompt.AddThemeConstantOverride("outline_size", 3);
                _ghostPrompt.AddThemeColorOverride("font_outline_color", Colors.Black);
                _placementGhost.AddChild(_ghostPrompt);

                AddChild(_placementGhost);
            }

            _placementGhost.GlobalPosition = GetGlobalMousePosition();
            _placementGhost.Visible = true;
        }
        else
        {
            // If placement was cancelled for an existing chest, restore its visibility
            if (chestId != null)
            {
                var existingNode = _entitiesContainer.GetNodeOrNull<CaveChestObject>($"CaveChest_{chestId}");
                if (existingNode != null) existingNode.Visible = true;
            }

            if (_placementGhost != null)
            {
                _placementGhost.QueueFree();
                _placementGhost = null;
                _ghostSprite = null;
                _ghostPrompt = null;
            }
        }
    }

    public bool IsValidChestPlacement(Vector2 pos, string? ignoreChestId)
    {
        // 1. Must be inside main chamber floor
        if (pos.X < -1000f || pos.X > 1000f || pos.Y < -620f || pos.Y > 620f)
        {
            return false;
        }

        // 2. Clearance from Blood Core at (0, 0)
        if (pos.DistanceTo(Vector2.Zero) < 130f)
        {
            return false;
        }

        // 3. Clearance from static workshop locations
        Vector2 craftPos = new Vector2(-700f, -520f);
        Vector2 juicerPos = new Vector2(0f, -520f);
        Vector2 labPos = new Vector2(700f, -520f);

        if (pos.DistanceTo(craftPos) < 140f) return false;
        if (pos.DistanceTo(juicerPos) < 140f) return false;
        if (pos.DistanceTo(labPos) < 140f) return false;

        // 4. Clearance from other chests
        foreach (var kvp in GameState.Instance.CaveChests)
        {
            if (kvp.Key == ignoreChestId) continue;
            Vector2 otherPos = new Vector2(kvp.Value.PosX, kvp.Value.PosY);
            if (pos.DistanceTo(otherPos) < 80f) return false;
        }

        // 5. Clearance from player
        if (Player != null && pos.DistanceTo(Player.GlobalPosition) < 45f)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Arranges 350x350 tiles across the lair matching the user schema:
    /// Main chamber: columns 2..8 (centered on column 5 at X = 0)
    /// Left corridor: columns 0..1 on row 2 (at Y = 0)
    /// </summary>
    private void BuildFloorGrid()
    {
        foreach (Node child in _floorContainer.GetChildren())
        {
            child.QueueFree();
        }

        // 9 columns centered around column 5 (X = 0):
        // col 0: -1750, col 1: -1400, col 2: -1050, col 3: -700, col 4: -350,
        // col 5: 0, col 6: 350, col 7: 700, col 8: 1050
        float[] cols = { -1750f, -1400f, -1050f, -700f, -350f, 0f, 350f, 700f, 1050f };

        // 5 rows centered around row 2 (Y = 0):
        // row 0: -700, row 1: -350, row 2: 0, row 3: 350, row 4: 700
        float[] rows = { -700f, -350f, 0f, 350f, 700f };

        for (int r = 0; r < FloorSchema.Length; r++)
        {
            for (int c = 0; c < FloorSchema[r].Length; c++)
            {
                int variant = FloorSchema[r][c];
                if (variant <= 0) continue; // Empty space (outside the cave boundaries)

                if (!_floorTextures.TryGetValue(variant, out var tex) || tex == null) continue;

                var tileSprite = new Sprite2D
                {
                    Texture = tex,
                    Position = new Vector2(cols[c], rows[r]),
                    Scale = new Vector2(TileScale, TileScale),
                    ZIndex = -10
                };
                _floorContainer.AddChild(tileSprite);
            }
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Process chest placement preview
        if (GameState.Instance.IsPlacingChest && _placementGhost != null)
        {
            Vector2 mousePos = GetGlobalMousePosition();
            _placementGhost.GlobalPosition = mousePos;

            bool isValid = IsValidChestPlacement(mousePos, GameState.Instance.ActivePlacementChestId);
            if (isValid)
            {
                if (_ghostSprite != null) _ghostSprite.Modulate = new Color(0.4f, 1.0f, 0.4f, 0.75f);
                if (_ghostPrompt != null)
                {
                    _ghostPrompt.Text = "Left-Click: Confirm Placement\nRight-Click / Esc: Cancel";
                    _ghostPrompt.Modulate = new Color(0.9f, 1.0f, 0.9f, 0.95f);
                }
            }
            else
            {
                if (_ghostSprite != null) _ghostSprite.Modulate = new Color(1.0f, 0.3f, 0.3f, 0.75f);
                if (_ghostPrompt != null)
                {
                    _ghostPrompt.Text = "Invalid Location\nRight-Click / Esc: Cancel";
                    _ghostPrompt.Modulate = new Color(1.0f, 0.5f, 0.5f, 0.95f);
                }
            }
        }

        // Proximity detection for left entrance/exit at '1 1'
        float distSq = Player != null ? Player.GlobalPosition.DistanceSquaredTo(new Vector2(-1800f, 0f)) : float.MaxValue;
        bool isPlayerNearExit = _isPlayerInExitArea || (distSq <= (220f * 220f));

        if (isPlayerNearExit)
        {
            _exitPromptLabel.Visible = true;
            _exitBlinkTimer += dt * 5.0f;
            // Smooth sinusoidal blinking between 0.20 and 1.0 matching outside entrance prompt
            float alpha = 0.20f + 0.80f * (0.5f + 0.5f * Mathf.Sin(_exitBlinkTimer));
            _exitPromptLabel.Modulate = new Color(1f, 1f, 1f, alpha);

            // Quitting cave happens with 'Enter' key
            if (!_isTransitioning && (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.KpEnter) || Input.IsActionJustPressed("ui_accept")))
            {
                ExitLair();
            }
        }
        else
        {
            _exitPromptLabel.Visible = false;
            _exitBlinkTimer = 0f;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GameState.Instance.IsPlacingChest)
        {
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    Vector2 mousePos = GetGlobalMousePosition();
                    if (IsValidChestPlacement(mousePos, GameState.Instance.ActivePlacementChestId))
                    {
                        GameState.Instance.ConfirmChestPlacement(mousePos);
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    else
                    {
                        GameState.Instance.TriggerDamageNumber("Cannot place chest here!", mousePos + new Vector2(0, -30), new Color(1f, 0.4f, 0.4f));
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                }
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    GameState.Instance.CancelChestPlacement();
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
            else if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
            {
                GameState.Instance.CancelChestPlacement();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        float distSq = Player != null ? Player.GlobalPosition.DistanceSquaredTo(new Vector2(-1800f, 0f)) : float.MaxValue;
        bool isPlayerNearExit = _isPlayerInExitArea || (distSq <= (220f * 220f));

        if (isPlayerNearExit && !_isTransitioning)
        {
            if (@event is InputEventKey key && key.Pressed && !key.Echo &&
                (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))
            {
                GetViewport().SetInputAsHandled();
                ExitLair();
            }
            else if (@event.IsActionPressed("ui_accept"))
            {
                GetViewport().SetInputAsHandled();
                ExitLair();
            }
        }
    }

    private void BuildBoundaries()
    {
        var walls = GetNodeOrNull<StaticBody2D>("Boundaries");
        if (walls != null) return;

        walls = new StaticBody2D { Name = "Boundaries" };

        // Main Chamber Top Wall (from X = -1225 to X = 1225 at Y = -875)
        AddWallSegment(walls, new Vector2(0f, -885f), new Vector2(2500f, 60f));

        // Main Chamber Bottom Wall (from X = -1225 to X = 1225 at Y = 875)
        AddWallSegment(walls, new Vector2(0f, 885f), new Vector2(2500f, 60f));

        // Right Wall: Solid single wall with NO opening/exit (from Y = -875 to Y = 875 at X = 1225)
        AddWallSegment(walls, new Vector2(1235f, 0f), new Vector2(60f, 1800f));

        // Main Chamber Left Wall: Upper segment (above corridor, from Y = -875 to Y = -175 at X = -1225)
        AddWallSegment(walls, new Vector2(-1235f, -525f), new Vector2(60f, 720f));

        // Main Chamber Left Wall: Lower segment (below corridor, from Y = 175 to Y = 875 at X = -1225)
        AddWallSegment(walls, new Vector2(-1235f, 525f), new Vector2(60f, 720f));

        // Corridor Top Wall (from X = -1925 to X = -1225 at Y = -175)
        AddWallSegment(walls, new Vector2(-1575f, -185f), new Vector2(720f, 40f));

        // Corridor Bottom Wall (from X = -1925 to X = -1225 at Y = 175)
        AddWallSegment(walls, new Vector2(-1575f, 185f), new Vector2(720f, 40f));

        // Corridor End Wall (at X = -1925 from Y = -175 to Y = 175, backing the left exit)
        AddWallSegment(walls, new Vector2(-1925f, 0f), new Vector2(40f, 380f));

        AddChild(walls);
    }

    private void AddWallSegment(StaticBody2D parent, Vector2 pos, Vector2 size)
    {
        var col = new CollisionShape2D();
        col.Shape = new RectangleShape2D { Size = size };
        col.Position = pos;
        parent.AddChild(col);
    }

    private void BuildEntranceVisuals()
    {
        var entranceNode = new Node2D { Name = "EntranceVisuals", ZIndex = -2 };
        var archTex = GD.Load<Texture2D>("res://assets/lair/lair_entrance.png");

        // Cave entrance arch on the left side at '1 1', horizontally oriented
        if (archTex != null)
        {
            var leftArch = new Sprite2D
            {
                Texture = archTex,
                Position = new Vector2(-1860f, 0f),
                Scale = new Vector2(0.58f, 0.58f),
                RotationDegrees = 0f // Horizontally oriented
            };
            entranceNode.AddChild(leftArch);
        }

        AddChild(entranceNode);
    }

    /// <summary>
    /// Places the custom rocky border sprite (lair_border_o.png) along the outer perimeter
    /// of the cave floor terrain grid.
    /// </summary>
    private void BuildTerrainBorders()
    {
        var borderTex = GD.Load<Texture2D>("res://assets/lair/lair_border_o.png");
        if (borderTex == null) return;

        var borderContainer = GetNodeOrNull<Node2D>("BorderContainer");
        if (borderContainer == null)
        {
            borderContainer = new Node2D
            {
                Name = "BorderContainer",
                ZIndex = -5 // Sits between floor tiles (-10) and entities/actors (0)
            };
            AddChild(borderContainer);
        }
        else
        {
            foreach (Node child in borderContainer.GetChildren())
            {
                child.QueueFree();
            }
        }

        float[] cols = { -1750f, -1400f, -1050f, -700f, -350f, 0f, 350f, 700f, 1050f };
        float[] rows = { -700f, -350f, 0f, 350f, 700f };
        const float halfTile = TileSize / 2f; // 175f

        for (int r = 0; r < FloorSchema.Length; r++)
        {
            for (int c = 0; c < FloorSchema[r].Length; c++)
            {
                if (FloorSchema[r][c] <= 0) continue;

                // 1. Top border: if neighbor cell above is empty or out of bounds
                if (r == 0 || FloorSchema[r - 1][c] <= 0)
                {
                    var sprite = new Sprite2D
                    {
                        Texture = borderTex,
                        Position = new Vector2(cols[c], rows[r] - halfTile),
                        RotationDegrees = 0f
                    };
                    borderContainer.AddChild(sprite);
                }

                // 2. Bottom border: if neighbor cell below is empty or out of bounds
                if (r == FloorSchema.Length - 1 || FloorSchema[r + 1][c] <= 0)
                {
                    var sprite = new Sprite2D
                    {
                        Texture = borderTex,
                        Position = new Vector2(cols[c], rows[r] + halfTile),
                        RotationDegrees = 180f
                    };
                    borderContainer.AddChild(sprite);
                }

                // 3. Left border: if neighbor cell to the left is empty or out of bounds
                if (c == 0 || FloorSchema[r][c - 1] <= 0)
                {
                    var sprite = new Sprite2D
                    {
                        Texture = borderTex,
                        Position = new Vector2(cols[c] - halfTile, rows[r]),
                        RotationDegrees = -90f
                    };
                    borderContainer.AddChild(sprite);
                }

                // 4. Right border: if neighbor cell to the right is empty or out of bounds
                if (c == FloorSchema[r].Length - 1 || FloorSchema[r][c + 1] <= 0)
                {
                    var sprite = new Sprite2D
                    {
                        Texture = borderTex,
                        Position = new Vector2(cols[c] + halfTile, rows[r]),
                        RotationDegrees = 90f
                    };
                    borderContainer.AddChild(sprite);
                }
            }
        }
    }


    private void BuildExitArea()
    {
        var exitContainer = new Node2D { Name = "ExitAreas" };

        // Only one exit area on the left side at '1 1'
        _exitArea = new Area2D { Name = "LeftExitArea" };
        var leftShape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(160f, 300f) },
            Position = new Vector2(-1800f, 0f)
        };
        _exitArea.AddChild(leftShape);
        _exitArea.BodyEntered += OnExitAreaBodyEntered;
        _exitArea.BodyExited += OnExitAreaBodyExited;
        exitContainer.AddChild(_exitArea);

        // Blinking text prompt placed above the horizontally oriented entrance arch
        _exitPromptLabel = new Label
        {
            Name = "ExitPromptLabel",
            Text = "Hit Enter to exit the cave",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ZIndex = 25,
            Visible = false,
            Position = new Vector2(-1920f, -130f),
            Size = new Vector2(300f, 35f)
        };
        _exitPromptLabel.AddThemeFontSizeOverride("font_size", 18);
        _exitPromptLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.93f, 0.45f, 1f)); // Warm gold
        _exitPromptLabel.AddThemeConstantOverride("outline_size", 4);
        _exitPromptLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 1f));
        _exitPromptLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        _exitPromptLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        _exitPromptLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
        exitContainer.AddChild(_exitPromptLabel);

        var leftLabel = new Label
        {
            Text = "◄ Exit to Wilderness",
            Position = new Vector2(-1890f, 110f),
            Size = new Vector2(200f, 25f),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        leftLabel.AddThemeFontSizeOverride("font_size", 14);
        leftLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.55f, 0.75f));
        exitContainer.AddChild(leftLabel);

        AddChild(exitContainer);
    }

    private void OnExitAreaBodyEntered(Node2D body)
    {
        if (body is Werewolf)
        {
            _isPlayerInExitArea = true;
        }
    }

    private void OnExitAreaBodyExited(Node2D body)
    {
        if (body is Werewolf)
        {
            _isPlayerInExitArea = false;
        }
    }

    public void ExitLair()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        GameState.Instance.IsInLair = false;

        // Reset cursor to normal
        CursorManager.ResetNormal();

        // Position player outside the cave entrance
        Vector2 returnPos = WorldManager.LairEntrancePosition + new Vector2(0f, 90f);
        SaveManager.LoadedPlayerPosition = returnPos;
        GameState.Instance.PlayerPosition = returnPos;
        SaveManager.SaveGame();

        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnStaticObjectCrafted -= OnStaticObjectCrafted;
            GameState.Instance.OnChestPlaced -= OnChestPlaced;
            GameState.Instance.OnChestPlacementModeChanged -= OnChestPlacementModeChanged;
        }

        if (_placementGhost != null && GodotObject.IsInstanceValid(_placementGhost))
        {
            _placementGhost.QueueFree();
            _placementGhost = null;
        }

        RenderingServer.SetDefaultClearColor(new Color(0.12f, 0.12f, 0.12f, 1f));
    }

    /// <summary>
    /// Custom lightweight canvas element that renders a cosmic void of faintly gleaming stars
    /// with gentle sinusoidal twinkling and delicate peak flares.
    /// </summary>
    private sealed partial class LairStarfield : Node2D
    {
        private struct Star
        {
            public Vector2 Position;
            public float BaseRadius;
            public Color BaseColor;
            public float MinAlpha;
            public float MaxAlpha;
            public float Speed;
            public float Phase;
            public bool CanGleam;
        }

        private readonly List<Star> _stars = new();

        public void Initialize(Rect2 area, int count)
        {
            ZIndex = -45;
            _stars.Clear();
            var rng = new RandomNumberGenerator();
            rng.Randomize();

            Color[] starColors =
            {
                new Color(0.95f, 0.95f, 1.00f), // Soft celestial white
                new Color(0.80f, 0.88f, 1.00f), // Pale ice-blue
                new Color(1.00f, 0.94f, 0.82f), // Faint warm starlight
                new Color(0.88f, 0.82f, 1.00f)  // Faint ethereal lavender
            };

            for (int i = 0; i < count; i++)
            {
                float x = rng.RandfRange(area.Position.X, area.Position.X + area.Size.X);
                float y = rng.RandfRange(area.Position.Y, area.Position.Y + area.Size.Y);

                float radius = rng.RandfRange(0.8f, 2.2f);
                Color col = starColors[rng.RandiRange(0, starColors.Length - 1)];
                float minAlpha = rng.RandfRange(0.06f, 0.16f);
                float maxAlpha = rng.RandfRange(0.38f, 0.72f);
                float speed = rng.RandfRange(0.7f, 2.2f);
                float phase = rng.RandfRange(0f, Mathf.Tau);
                bool canGleam = radius >= 1.5f && rng.Randf() > 0.45f;

                _stars.Add(new Star
                {
                    Position = new Vector2(x, y),
                    BaseRadius = radius,
                    BaseColor = col,
                    MinAlpha = minAlpha,
                    MaxAlpha = maxAlpha,
                    Speed = speed,
                    Phase = phase,
                    CanGleam = canGleam
                });
            }
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            for (int i = 0; i < _stars.Count; i++)
            {
                var s = _stars[i];
                s.Phase += s.Speed * dt;
                if (s.Phase > Mathf.Tau)
                {
                    s.Phase -= Mathf.Tau;
                }
                _stars[i] = s;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                var s = _stars[i];
                float sine = (Mathf.Sin(s.Phase) + 1f) * 0.5f;
                float currentAlpha = Mathf.Lerp(s.MinAlpha, s.MaxAlpha, sine);

                // Faint soft halo
                DrawCircle(s.Position, s.BaseRadius * 2.6f, new Color(s.BaseColor.R, s.BaseColor.G, s.BaseColor.B, currentAlpha * 0.18f));

                // Star core
                DrawCircle(s.Position, s.BaseRadius, new Color(s.BaseColor.R, s.BaseColor.G, s.BaseColor.B, currentAlpha));

                // Faint gleam flare at peak twinkle
                if (s.CanGleam && currentAlpha > 0.50f)
                {
                    float gleamFactor = (currentAlpha - 0.50f) / (s.MaxAlpha - 0.50f);
                    float gleamLen = s.BaseRadius * 3.2f * gleamFactor;
                    float gleamAlpha = currentAlpha * 0.35f;
                    Color gleamCol = new Color(s.BaseColor.R, s.BaseColor.G, s.BaseColor.B, gleamAlpha);

                    DrawLine(s.Position - new Vector2(gleamLen, 0f), s.Position + new Vector2(gleamLen, 0f), gleamCol, 1.0f);
                    DrawLine(s.Position - new Vector2(0f, gleamLen), s.Position + new Vector2(0f, gleamLen), gleamCol, 1.0f);
                }
            }
        }
    }
}
