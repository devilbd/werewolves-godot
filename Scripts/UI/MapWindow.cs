using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;
using Werewolves.World;

namespace Werewolves.UI;

public partial class MapWindow : Control
{
    private Panel _windowPanel = null!;
    private Label _titleLabel = null!;
    private Label _coordsLabel = null!;
    private Button _closeButton = null!;
    private MapCanvas _canvas = null!;

    // Filter Checkboxes
    private CheckBox _filterLandmarks = null!;
    private CheckBox _filterCreatures = null!;
    private CheckBox _filterResources = null!;
    private CheckBox _filterLoot = null!;

    // Window dragging
    private bool _isDraggingWindow = false;
    private Vector2 _windowDragOffset = Vector2.Zero;

    public MapWindow()
    {
        Visible = false;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(900, 720);
        Size = new Vector2(900, 720);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        BuildUI();

        GameState.Instance.OnMapToggled += OnMapToggled;
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnMapToggled -= OnMapToggled;
        }
    }

    private void BuildUI()
    {
        // 1. Background Frame Panel
        _windowPanel = new Panel
        {
            Name = "WindowPanel",
            CustomMinimumSize = new Vector2(900, 720),
            Size = new Vector2(900, 720),
            MouseFilter = MouseFilterEnum.Stop
        };
        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.13f, 0.98f),
            BorderColor = new Color(0.72f, 0.58f, 0.30f, 1.0f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.7f),
            ShadowSize = 10
        };
        _windowPanel.AddThemeStyleboxOverride("panel", frameStyle);
        _windowPanel.GuiInput += OnHeaderGuiInput;
        AddChild(_windowPanel);

        // Center on screen
        SetAnchorsPreset(LayoutPreset.Center);

        // 2. Header Bar
        _titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "World Map — Wilderness",
            Position = new Vector2(24, 14),
            Size = new Vector2(300, 26)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.45f));
        _titleLabel.AddThemeConstantOverride("outline_size", 3);
        _titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _windowPanel.AddChild(_titleLabel);

        _coordsLabel = new Label
        {
            Name = "CoordsLabel",
            Text = "Player: (0, 0) | Region: Wilderness",
            Position = new Vector2(330, 16),
            Size = new Vector2(480, 22),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _coordsLabel.AddThemeFontSizeOverride("font_size", 13);
        _coordsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.90f, 0.75f));
        _coordsLabel.AddThemeConstantOverride("outline_size", 2);
        _coordsLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _windowPanel.AddChild(_coordsLabel);

        // Close Button
        _closeButton = new Button
        {
            Name = "CloseButton",
            Text = "X",
            Position = new Vector2(856, 12),
            Size = new Vector2(28, 28)
        };
        ApplyCloseButtonStyle(_closeButton);
        _closeButton.Pressed += () => GameState.Instance.CloseMap();
        _windowPanel.AddChild(_closeButton);

        // 3. Map Canvas Area (850 x 590)
        _canvas = new MapCanvas
        {
            Name = "MapCanvas",
            Position = new Vector2(24, 50),
            CustomMinimumSize = new Vector2(852, 600),
            Size = new Vector2(852, 600),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Stop
        };
        _windowPanel.AddChild(_canvas);

        // 4. Bottom Toolbar & Filters
        var toolbarHBox = new HBoxContainer
        {
            Position = new Vector2(24, 662),
            Size = new Vector2(852, 40)
        };
        toolbarHBox.AddThemeConstantOverride("separation", 16);
        _windowPanel.AddChild(toolbarHBox);

        // Navigation controls
        var centerBtn = new Button
        {
            Text = "⌖ Center Player",
            CustomMinimumSize = new Vector2(120, 32)
        };
        ApplySmallButtonStyle(centerBtn, new Color(0.18f, 0.22f, 0.30f));
        centerBtn.Pressed += () => _canvas.CenterOnPlayer();
        toolbarHBox.AddChild(centerBtn);

        var zoomInBtn = new Button
        {
            Text = "+ Zoom In",
            CustomMinimumSize = new Vector2(90, 32)
        };
        ApplySmallButtonStyle(zoomInBtn, new Color(0.18f, 0.22f, 0.30f));
        zoomInBtn.Pressed += () => _canvas.Zoom(1.25f);
        toolbarHBox.AddChild(zoomInBtn);

        var zoomOutBtn = new Button
        {
            Text = "- Zoom Out",
            CustomMinimumSize = new Vector2(90, 32)
        };
        ApplySmallButtonStyle(zoomOutBtn, new Color(0.18f, 0.22f, 0.30f));
        zoomOutBtn.Pressed += () => _canvas.Zoom(0.80f);
        toolbarHBox.AddChild(zoomOutBtn);

        var resetBtn = new Button
        {
            Text = "Overview",
            CustomMinimumSize = new Vector2(80, 32)
        };
        ApplySmallButtonStyle(resetBtn, new Color(0.18f, 0.22f, 0.30f));
        resetBtn.Pressed += () => _canvas.ResetView();
        toolbarHBox.AddChild(resetBtn);

        // Separator
        var sep = new VSeparator();
        toolbarHBox.AddChild(sep);

        // Filter Checkboxes
        _filterLandmarks = CreateFilterCheckbox("Landmarks", true);
        _filterLandmarks.Toggled += (on) => { _canvas.ShowLandmarks = on; _canvas.QueueRedraw(); };
        toolbarHBox.AddChild(_filterLandmarks);

        _filterCreatures = CreateFilterCheckbox("Creatures", true);
        _filterCreatures.Toggled += (on) => { _canvas.ShowCreatures = on; _canvas.QueueRedraw(); };
        toolbarHBox.AddChild(_filterCreatures);

        _filterResources = CreateFilterCheckbox("Resources", true);
        _filterResources.Toggled += (on) => { _canvas.ShowResources = on; _canvas.QueueRedraw(); };
        toolbarHBox.AddChild(_filterResources);

        _filterLoot = CreateFilterCheckbox("Loot", true);
        _filterLoot.Toggled += (on) => { _canvas.ShowLoot = on; _canvas.QueueRedraw(); };
        toolbarHBox.AddChild(_filterLoot);
    }

    private CheckBox CreateFilterCheckbox(string text, bool defaultChecked)
    {
        var cb = new CheckBox
        {
            Text = text,
            ButtonPressed = defaultChecked
        };
        cb.AddThemeFontSizeOverride("font_size", 12);
        cb.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        return cb;
    }

    private void OnMapToggled(bool visible)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Visible = visible;
        if (visible)
        {
            UpdateHeaderInfo();
            _canvas.CenterOnPlayer();
            _canvas.QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        UpdateHeaderInfo();
        _canvas.QueueRedraw();
    }

    private void UpdateHeaderInfo()
    {
        Vector2 worldPos = GameState.Instance.PlayerPosition;
        Vector2 mapPos = WorldManager.ToMapCoordinates(worldPos);

        if (GameState.Instance.IsInLair)
        {
            _titleLabel.Text = "Cavern Sanctuary — Werewolf's Lair";
            _coordsLabel.Text = $"Hideout Pos: ({(int)worldPos.X}, {(int)worldPos.Y})";
        }
        else
        {
            string region = WorldManager.GetRegionName(worldPos);
            _titleLabel.Text = "World Map — Wilderness";
            _coordsLabel.Text = $"Map Pos: ({(int)mapPos.X}, {(int)mapPos.Y}) | Region: {region}";
        }
    }

    private void OnHeaderGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _isDraggingWindow = true;
                _windowDragOffset = GetGlobalMousePosition() - Position;
            }
            else
            {
                _isDraggingWindow = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDraggingWindow && @event is InputEventMouseMotion)
        {
            Position = GetGlobalMousePosition() - _windowDragOffset;
            var vpSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, vpSize.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, vpSize.Y - Size.Y))
            );
        }
    }

    private static void ApplySmallButtonStyle(Button btn, Color baseBg)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = baseBg,
            BorderColor = new Color(0.65f, 0.52f, 0.32f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = baseBg.Lightened(0.18f),
            BorderColor = new Color(1.0f, 0.85f, 0.40f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f));
        btn.AddThemeFontSizeOverride("font_size", 12);
    }

    private static void ApplyCloseButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.20f, 0.08f, 0.08f, 0.85f),
            BorderColor = new Color(0.65f, 0.25f, 0.25f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.10f, 0.10f, 1.0f),
            BorderColor = new Color(0.95f, 0.35f, 0.35f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.85f));
        btn.AddThemeFontSizeOverride("font_size", 14);
    }

    /// <summary>
    /// Custom procedural map rendering canvas.
    /// Handles camera pan, zoom, grid projection, radar perception aura, and entity blips.
    /// </summary>
    public sealed partial class MapCanvas : Control
    {
        public bool ShowLandmarks { get; set; } = true;
        public bool ShowCreatures { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowLoot { get; set; } = true;

        public const float PerceptionRadius = 1800f; // World units visible radar range around player

        private Vector2 _centerWorldPos = Vector2.Zero;
        private float _zoomScale = 0.075f; // Scale factor from world to canvas
        private const float MinZoom = 0.035f; // Overview zoom
        private const float MaxZoom = 0.35f;  // Detailed close-up zoom

        private bool _isPanning = false;
        private Vector2 _panStartMouse = Vector2.Zero;
        private Vector2 _panStartCenter = Vector2.Zero;

        // Hover tooltip tracking
        private string? _hoveredInfo = null;
        private Vector2 _hoveredScreenPos = Vector2.Zero;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            ResetView();
        }

        public void CenterOnPlayer()
        {
            _centerWorldPos = GameState.Instance.PlayerPosition;
            QueueRedraw();
        }

        public void ResetView()
        {
            _centerWorldPos = Vector2.Zero;
            _zoomScale = GameState.Instance.IsInLair ? 0.22f : 0.075f;
            QueueRedraw();
        }

        public void Zoom(float factor)
        {
            _zoomScale = Mathf.Clamp(_zoomScale * factor, MinZoom, MaxZoom);
            QueueRedraw();
        }

        public Vector2 WorldToCanvas(Vector2 worldPos)
        {
            Vector2 canvasCenter = Size / 2f;
            Vector2 delta = worldPos - _centerWorldPos;
            return canvasCenter + delta * _zoomScale;
        }

        public Vector2 CanvasToWorld(Vector2 canvasPos)
        {
            Vector2 canvasCenter = Size / 2f;
            Vector2 delta = (canvasPos - canvasCenter) / _zoomScale;
            return _centerWorldPos + delta;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _isPanning = true;
                        _panStartMouse = mb.Position;
                        _panStartCenter = _centerWorldPos;
                    }
                    else
                    {
                        _isPanning = false;
                    }
                }
                else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
                {
                    Zoom(1.15f);
                }
                else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
                {
                    Zoom(0.87f);
                }
            }
            else if (@event is InputEventMouseMotion mm)
            {
                if (_isPanning)
                {
                    Vector2 mouseDelta = mm.Position - _panStartMouse;
                    _centerWorldPos = _panStartCenter - (mouseDelta / _zoomScale);
                    QueueRedraw();
                }
                else
                {
                    UpdateHoverInfo(mm.Position);
                }
            }
        }

        private void UpdateHoverInfo(Vector2 mousePos)
        {
            _hoveredInfo = null;
            Vector2 playerWorld = GameState.Instance.PlayerPosition;

            // Check landmarks
            if (!GameState.Instance.IsInLair && ShowLandmarks)
            {
                CheckHoverLandmark("The Village", WorldManager.VillagePosition, mousePos, playerWorld);
                CheckHoverLandmark("Werewolf's Lair Entrance", WorldManager.LairEntrancePosition, mousePos, playerWorld);
                CheckHoverLandmark("Awakening Grove", WorldManager.AwakeningGrovePosition, mousePos, playerWorld);
                CheckHoverLandmark("Quarry Hills", WorldManager.QuarryPosition, mousePos, playerWorld);
                CheckHoverLandmark("Silent Lake", WorldManager.SilentLakePosition, mousePos, playerWorld);
                CheckHoverLandmark("Misty Lake", WorldManager.MistyLakePosition, mousePos, playerWorld);
            }

            QueueRedraw();
        }

        private void CheckHoverLandmark(string name, Vector2 worldPos, Vector2 mousePos, Vector2 playerWorld)
        {
            Vector2 canvasPos = WorldToCanvas(worldPos);
            if (mousePos.DistanceTo(canvasPos) <= 18f)
            {
                Vector2 mapPos = WorldManager.ToMapCoordinates(worldPos);
                float dist = playerWorld.DistanceTo(worldPos);
                _hoveredInfo = $"{name}\nMap: ({(int)mapPos.X}, {(int)mapPos.Y}) | Dist: {(int)dist}m";
                _hoveredScreenPos = canvasPos + new Vector2(10f, -10f);
            }
        }

        public override void _Draw()
        {
            // Background Canvas Fill
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.04f, 0.07f, 0.05f, 1f));

            if (GameState.Instance.IsInLair)
            {
                DrawCavernMap();
            }
            else
            {
                DrawWildernessMap();
            }

            // Draw Hovered Tooltip Card
            if (!string.IsNullOrEmpty(_hoveredInfo))
            {
                var font = ThemeDB.FallbackFont;
                int fontSize = 12;
                var stringSize = font.GetStringSize(_hoveredInfo, HorizontalAlignment.Left, -1, fontSize);
                Rect2 tipRect = new Rect2(_hoveredScreenPos, new Vector2(stringSize.X + 16, stringSize.Y + 12));
                DrawRect(tipRect, new Color(0.08f, 0.10f, 0.15f, 0.95f));
                DrawRect(tipRect, new Color(0.85f, 0.75f, 0.40f, 1.0f), false, 1.5f);
                DrawMultilineString(font, _hoveredScreenPos + new Vector2(8, 14), _hoveredInfo, HorizontalAlignment.Left, -1, fontSize, maxLines: -1, modulate: Colors.White);
            }
        }

        private void DrawWildernessMap()
        {
            var font = ThemeDB.FallbackFont;
            Vector2 playerWorld = GameState.Instance.PlayerPosition;
            Vector2 playerCanvas = WorldToCanvas(playerWorld);

            // 1. World Boundaries (10,000 x 10,000 box centered at 0,0)
            Vector2 topLeft = WorldToCanvas(new Vector2(-WorldManager.WorldRadius, -WorldManager.WorldRadius));
            Vector2 botRight = WorldToCanvas(new Vector2(WorldManager.WorldRadius, WorldManager.WorldRadius));
            Rect2 worldBox = new Rect2(topLeft, botRight - topLeft);
            DrawRect(worldBox, new Color(0.07f, 0.12f, 0.08f, 1f));
            DrawRect(worldBox, new Color(0.40f, 0.35f, 0.22f, 0.85f), false, 2f);

            // 2. Coordinate Grid Lines (every 1000 units)
            for (float x = -WorldManager.WorldRadius; x <= WorldManager.WorldRadius; x += 1000f)
            {
                Vector2 p1 = WorldToCanvas(new Vector2(x, -WorldManager.WorldRadius));
                Vector2 p2 = WorldToCanvas(new Vector2(x, WorldManager.WorldRadius));
                DrawLine(p1, p2, new Color(0.15f, 0.22f, 0.16f, 0.45f), 1f);
            }
            for (float y = -WorldManager.WorldRadius; y <= WorldManager.WorldRadius; y += 1000f)
            {
                Vector2 p1 = WorldToCanvas(new Vector2(-WorldManager.WorldRadius, y));
                Vector2 p2 = WorldToCanvas(new Vector2(WorldManager.WorldRadius, y));
                DrawLine(p1, p2, new Color(0.15f, 0.22f, 0.16f, 0.45f), 1f);
            }

            // 3. Natural Lakes
            if (ShowLandmarks)
            {
                DrawLake(WorldManager.SilentLakePosition, "Silent Lake", 480f);
                DrawLake(WorldManager.MistyLakePosition, "Misty Lake", 480f);
            }

            // 4. Visible Range Radar / Perception Aura (R = 1800 units)
            float canvasRadarRadius = PerceptionRadius * _zoomScale;
            // Translucent fill
            DrawCircle(playerCanvas, canvasRadarRadius, new Color(0.85f, 0.2f, 0.2f, 0.06f));
            // Outer dashed-like circle
            DrawArc(playerCanvas, canvasRadarRadius, 0f, Mathf.Tau, 64, new Color(0.95f, 0.25f, 0.25f, 0.65f), 1.5f);
            // Label along radar rim
            DrawString(font, playerCanvas + new Vector2(canvasRadarRadius + 8f, 4f), "Perception (1800m)", HorizontalAlignment.Left, -1, 10, new Color(0.95f, 0.45f, 0.45f, 0.8f));

            // 5. Permanent Landmarks
            if (ShowLandmarks)
            {
                DrawLandmark(WorldManager.AwakeningGrovePosition, "Awakening Grove (0, 0)", new Color(0.3f, 0.9f, 0.4f), 1);
                DrawLandmark(WorldManager.LairEntrancePosition, "Lair Entrance (-650, 450)", new Color(0.7f, 0.4f, 0.9f), 2);
                DrawLandmark(WorldManager.VillagePosition, "The Village (2500, 1800)", new Color(1.0f, 0.75f, 0.2f), 3);
                DrawLandmark(WorldManager.QuarryPosition, "Quarry Hills (2200, -2200)", new Color(0.7f, 0.7f, 0.75f), 4);
            }

            // 6. Living Entities & Drops inside Visible Perception Range
            ScanAndDrawVisibleEntities(playerWorld);

            // 7. Player Beacon & Heading
            DrawPlayerMarker(playerCanvas);
        }

        private void DrawCavernMap()
        {
            var font = ThemeDB.FallbackFont;
            Vector2 playerWorld = GameState.Instance.PlayerPosition;
            Vector2 playerCanvas = WorldToCanvas(playerWorld);

            // 1. Cavern Floor Chamber Bounds (-1750 to 1050 X, -700 to 700 Y)
            Vector2 cTopLeft = WorldToCanvas(new Vector2(-1850f, -800f));
            Vector2 cBotRight = WorldToCanvas(new Vector2(1150f, 800f));
            Rect2 chamberBox = new Rect2(cTopLeft, cBotRight - cTopLeft);
            DrawRect(chamberBox, new Color(0.10f, 0.11f, 0.16f, 1f));
            DrawRect(chamberBox, new Color(0.65f, 0.50f, 0.30f, 0.9f), false, 2f);

            // Corridor Outline
            Vector2 corrTopLeft = WorldToCanvas(new Vector2(-1850f, -200f));
            Vector2 corrBotRight = WorldToCanvas(new Vector2(-1050f, 200f));
            DrawRect(new Rect2(corrTopLeft, corrBotRight - corrTopLeft), new Color(0.14f, 0.15f, 0.22f, 0.9f));

            // Central Blood Core Altar (0, 0)
            Vector2 coreCanvas = WorldToCanvas(Vector2.Zero);
            DrawCircle(coreCanvas, 14f, new Color(0.85f, 0.12f, 0.15f, 1f));
            DrawArc(coreCanvas, 14f, 0f, Mathf.Tau, 32, new Color(1.0f, 0.85f, 0.35f), 2f);
            float reservesPct = GameState.Instance.BloodCoreReserves / GameState.Instance.BloodCoreMaxReserves * 100f;
            DrawString(font, coreCanvas + new Vector2(-55, -20), $"Blood Core ({reservesPct:F0}%)", HorizontalAlignment.Left, -1, 11, new Color(1.0f, 0.4f, 0.4f));

            // Workshop Stations
            Vector2 craftPos = WorldToCanvas(new Vector2(-700f, -520f));
            DrawStation(craftPos, "Crafting Table", new Color(0.8f, 0.55f, 0.25f));

            Vector2 juicerPos = WorldToCanvas(new Vector2(0f, -520f));
            DrawStation(juicerPos, "Blood Juicer", new Color(0.9f, 0.25f, 0.25f));

            Vector2 labPos = WorldToCanvas(new Vector2(700f, -520f));
            DrawStation(labPos, "Laboratory", new Color(0.5f, 0.8f, 1.0f));

            // Exit Portal Arch (-1650, 0)
            Vector2 exitPos = WorldToCanvas(new Vector2(-1650f, 0f));
            DrawCircle(exitPos, 10f, new Color(0.7f, 0.4f, 0.95f));
            DrawString(font, exitPos + new Vector2(-40, 22), "Cave Exit (Portal)", HorizontalAlignment.Left, -1, 11, new Color(0.85f, 0.65f, 1.0f));

            // Storage Chests
            foreach (var kvp in GameState.Instance.CaveChests)
            {
                var chest = kvp.Value;
                Vector2 chestCanvas = WorldToCanvas(new Vector2(chest.PosX, chest.PosY));
                DrawRect(new Rect2(chestCanvas - new Vector2(8, 8), new Vector2(16, 16)), new Color(0.85f, 0.70f, 0.25f));
                DrawRect(new Rect2(chestCanvas - new Vector2(8, 8), new Vector2(16, 16)), Colors.Black, false, 1f);
                DrawString(font, chestCanvas + new Vector2(-18, -12), "Chest", HorizontalAlignment.Left, -1, 10, new Color(1f, 0.9f, 0.4f));
            }

            // Player Marker
            DrawPlayerMarker(playerCanvas);
        }

        private void DrawStation(Vector2 canvasPos, string label, Color color)
        {
            var font = ThemeDB.FallbackFont;
            DrawRect(new Rect2(canvasPos - new Vector2(9, 9), new Vector2(18, 18)), color);
            DrawRect(new Rect2(canvasPos - new Vector2(9, 9), new Vector2(18, 18)), Colors.White, false, 1f);
            DrawString(font, canvasPos + new Vector2(-40, -14), label, HorizontalAlignment.Left, -1, 11, color.Lightened(0.2f));
        }

        private void DrawLake(Vector2 worldPos, string label, float radius)
        {
            var font = ThemeDB.FallbackFont;
            Vector2 canvasPos = WorldToCanvas(worldPos);
            float canvasR = radius * _zoomScale;
            DrawCircle(canvasPos, canvasR, new Color(0.12f, 0.35f, 0.55f, 0.85f));
            DrawArc(canvasPos, canvasR, 0f, Mathf.Tau, 32, new Color(0.35f, 0.65f, 0.95f, 0.9f), 1.5f);
            DrawString(font, canvasPos + new Vector2(-canvasR * 0.7f, 4), label, HorizontalAlignment.Left, -1, 11, new Color(0.7f, 0.9f, 1.0f));
        }

        private void DrawLandmark(Vector2 worldPos, string label, Color color, int iconType)
        {
            var font = ThemeDB.FallbackFont;
            Vector2 canvasPos = WorldToCanvas(worldPos);

            // Ring beacon
            DrawCircle(canvasPos, 9f, color);
            DrawArc(canvasPos, 13f, 0f, Mathf.Tau, 24, color.Lightened(0.2f), 1.5f);

            // Landmark Name Badge
            DrawString(font, canvasPos + new Vector2(16, 4), label, HorizontalAlignment.Left, -1, 11, Colors.White);
        }

        private void ScanAndDrawVisibleEntities(Vector2 playerWorld)
        {
            var font = ThemeDB.FallbackFont;
            var tree = GetTree();
            if (tree == null) return;

            var entitiesNode = tree.Root.FindChild("Entities", true, false) as Node2D;
            if (entitiesNode == null) return;

            foreach (Node child in entitiesNode.GetChildren())
            {
                if (child is not Node2D entity || entity is Werewolf) continue;

                float dist = playerWorld.DistanceTo(entity.GlobalPosition);
                // Only reveal entities within player's Perception Range!
                if (dist > PerceptionRadius) continue;

                Vector2 canvasPos = WorldToCanvas(entity.GlobalPosition);

                // 1. Villagers
                if (ShowCreatures && child is Villager villager)
                {
                    DrawCircle(canvasPos, 5f, new Color(1.0f, 0.72f, 0.20f));
                    DrawArc(canvasPos, 7f, 0f, Mathf.Tau, 16, Colors.Black, 1f);
                    DrawString(font, canvasPos + new Vector2(8, 3), "Villager", HorizontalAlignment.Left, -1, 10, new Color(1.0f, 0.85f, 0.4f));
                }
                // 2. Deer
                else if (ShowCreatures && child is Deer deer)
                {
                    DrawCircle(canvasPos, 5f, new Color(0.35f, 0.95f, 0.45f));
                    DrawArc(canvasPos, 7f, 0f, Mathf.Tau, 16, Colors.Black, 1f);
                    DrawString(font, canvasPos + new Vector2(8, 3), "Deer", HorizontalAlignment.Left, -1, 10, new Color(0.65f, 1.0f, 0.65f));
                }
                // 3. Treasure Chests
                else if (ShowResources && child is ChestObject)
                {
                    DrawRect(new Rect2(canvasPos - new Vector2(5, 5), new Vector2(10, 10)), new Color(1.0f, 0.85f, 0.25f));
                    DrawRect(new Rect2(canvasPos - new Vector2(5, 5), new Vector2(10, 10)), Colors.Black, false, 1f);
                    DrawString(font, canvasPos + new Vector2(8, 3), "Chest", HorizontalAlignment.Left, -1, 10, new Color(1.0f, 0.9f, 0.35f));
                }
                // 4. Quartz Crystals
                else if (ShowResources && child is QuartzObject)
                {
                    DrawCircle(canvasPos, 5f, new Color(0.85f, 0.60f, 1.0f));
                    DrawArc(canvasPos, 6f, 0f, Mathf.Tau, 16, Colors.Black, 1f);
                    DrawString(font, canvasPos + new Vector2(8, 3), "Quartz", HorizontalAlignment.Left, -1, 10, new Color(0.9f, 0.75f, 1.0f));
                }
                // 5. Dropped Loot
                else if (ShowLoot && child is DroppedLoot loot)
                {
                    DrawCircle(canvasPos, 4f, new Color(0.35f, 0.95f, 0.95f));
                    DrawString(font, canvasPos + new Vector2(6, 3), loot.ItemType, HorizontalAlignment.Left, -1, 9, new Color(0.7f, 1.0f, 1.0f));
                }
                // 6. Blood Spots
                else if (ShowLoot && child is BloodSpot)
                {
                    DrawCircle(canvasPos, 4f, new Color(0.95f, 0.20f, 0.20f));
                    DrawString(font, canvasPos + new Vector2(6, 3), "Blood", HorizontalAlignment.Left, -1, 9, new Color(1.0f, 0.45f, 0.45f));
                }
            }
        }

        private void DrawPlayerMarker(Vector2 playerCanvas)
        {
            var font = ThemeDB.FallbackFont;

            // Outer pulsing beacon
            DrawArc(playerCanvas, 16f, 0f, Mathf.Tau, 32, new Color(1.0f, 0.25f, 0.25f, 0.8f), 2f);
            DrawCircle(playerCanvas, 9f, new Color(0.85f, 0.12f, 0.15f));
            DrawCircle(playerCanvas, 4f, new Color(1.0f, 0.9f, 0.4f));

            // Direction arrow based on werewolf movement or facing
            var tree = GetTree();
            var werewolf = tree?.Root.FindChild("Werewolf", true, false) as Werewolf;
            Vector2 facingDir = Vector2.Right;
            if (werewolf != null)
            {
                if (werewolf.Velocity.LengthSquared() > 10f)
                {
                    facingDir = werewolf.Velocity.Normalized();
                }
                else
                {
                    var sprite = werewolf.GetNodeOrNull<Sprite2D>("Sprite2D");
                    facingDir = (sprite != null && sprite.FlipH) ? Vector2.Left : Vector2.Right;
                }
            }

            Vector2 arrowTip = playerCanvas + facingDir * 20f;
            Vector2 side1 = playerCanvas + facingDir * 10f + new Vector2(-facingDir.Y, facingDir.X) * 6f;
            Vector2 side2 = playerCanvas + facingDir * 10f + new Vector2(facingDir.Y, -facingDir.X) * 6f;
            DrawColoredPolygon(new[] { arrowTip, side1, side2 }, new Color(1.0f, 0.85f, 0.25f));

            // Player Label
            DrawString(font, playerCanvas + new Vector2(-36, -20), "Werewolf (You)", HorizontalAlignment.Left, -1, 11, new Color(1.0f, 0.92f, 0.45f));
        }
    }
}
