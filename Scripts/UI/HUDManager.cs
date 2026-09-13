using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.UI;

public partial class HUDManager : CanvasLayer
{
    [Export] public Werewolf? Player { get; set; }

    private Label _positionLabel = null!;
    private OrbGauge _healthOrb = null!;
    private OrbGauge _powerOrb = null!;
    private StatsPanel _statsPanel = null!;
    private TargetPanel _targetPanel = null!;
    private ActionBar _actionBar = null!;
    private PouchWindow _pouchWindow = null!;
    private HeroDetailsWindow _heroDetailsWindow = null!;
    private CraftingWindow _craftingWindow = null!;
    private ChestInventoryWindow _chestInventoryWindow = null!;
    private ItemSplitModal _itemSplitModal = null!;
    private MapWindow _mapWindow = null!;

    private Vector2 ViewportSize => GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);

    public override void _Ready()
    {
        // 1. Position Label (Top Center)
        _positionLabel = GetNodeOrNull<Label>("PositionLabel") ?? GetNodeOrNull<Label>("AreaLabel");
        if (_positionLabel == null)
        {
            _positionLabel = new Label
            {
                Name = "PositionLabel",
                Text = "Wilderness | Pos: (0, 0)",
                HorizontalAlignment = HorizontalAlignment.Center,
                Size = new Vector2(360, 30),
                Position = new Vector2(ViewportSize.X / 2 - 180, 15)
            };
            _positionLabel.AddThemeFontSizeOverride("font_size", 16);
            _positionLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            AddChild(_positionLabel);
        }
        else
        {
            _positionLabel.Name = "PositionLabel";
            _positionLabel.Size = new Vector2(360, 30);
            _positionLabel.HorizontalAlignment = HorizontalAlignment.Center;
        }

        GameState.Instance.OnPositionChanged += OnGameStatePositionChanged;

        // 2. Target Panel (Top Left)
        _targetPanel = GetNodeOrNull<TargetPanel>("TargetPanel");
        if (_targetPanel == null)
        {
            _targetPanel = new TargetPanel
            {
                Name = "TargetPanel",
                Player = Player,
                Position = new Vector2(20, 20)
            };
            AddChild(_targetPanel);
        }
        else
        {
            _targetPanel.Player = Player;
        }

        // 3. Health Orb (Bottom Left)
        _healthOrb = GetNodeOrNull<OrbGauge>("HealthOrb");
        if (_healthOrb == null)
        {
            _healthOrb = new OrbGauge
            {
                Name = "HealthOrb",
                LabelText = "Health",
                PrimaryColor = new Color(0.55f, 0f, 0f, 1f),
                BackgroundColor = new Color(0.2f, 0f, 0f, 1f),
                RingTexture = GD.Load<Texture2D>("res://assets/health_ring.png"),
                CurrentValue = GameState.Instance.PlayerHealth,
                MaxValue = GameState.Instance.PlayerMaxHealth,
                Position = new Vector2(24, ViewportSize.Y - 234)
            };
            AddChild(_healthOrb);
        }
        else
        {
            if (_healthOrb.RingTexture == null)
                _healthOrb.RingTexture = GD.Load<Texture2D>("res://assets/health_ring.png");
            _healthOrb.CurrentValue = GameState.Instance.PlayerHealth;
            _healthOrb.MaxValue = GameState.Instance.PlayerMaxHealth;
        }

        GameState.Instance.OnHealthChanged += OnGameStateHealthChanged;

        // 4. Power Orb (Bottom Right)
        _powerOrb = GetNodeOrNull<OrbGauge>("PowerOrb");
        if (_powerOrb == null)
        {
            _powerOrb = new OrbGauge
            {
                Name = "PowerOrb",
                LabelText = "Power",
                PrimaryColor = new Color(0f, 0.55f, 0.55f, 1f),
                BackgroundColor = new Color(0f, 0.2f, 0.2f, 1f),
                RingTexture = GD.Load<Texture2D>("res://assets/power_ring.png"),
                CurrentValue = GameState.Instance.PlayerPower,
                MaxValue = GameState.Instance.PlayerMaxPower,
                Position = new Vector2(ViewportSize.X - 234, ViewportSize.Y - 234)
            };
            AddChild(_powerOrb);
        }
        else
        {
            if (_powerOrb.RingTexture == null)
                _powerOrb.RingTexture = GD.Load<Texture2D>("res://assets/power_ring.png");
            _powerOrb.CurrentValue = GameState.Instance.PlayerPower;
            _powerOrb.MaxValue = GameState.Instance.PlayerMaxPower;
        }

        GameState.Instance.OnPowerChanged += OnGameStatePowerChanged;

        // 5. Stats Panel (Bottom Center)
        _statsPanel = GetNodeOrNull<StatsPanel>("StatsPanel");
        if (_statsPanel == null)
        {
            _statsPanel = new StatsPanel
            {
                Name = "StatsPanel",
                Player = Player,
                Position = new Vector2(ViewportSize.X / 2 - 140, ViewportSize.Y - 80)
            };
            AddChild(_statsPanel);
        }
        else
        {
            _statsPanel.Player = Player;
        }

        // 6. Action Bar (Right side of Stats Panel)
        _actionBar = GetNodeOrNull<ActionBar>("ActionBar");
        if (_actionBar == null)
        {
            _actionBar = new ActionBar
            {
                Name = "ActionBar",
                Position = new Vector2(ViewportSize.X / 2 + 150, ViewportSize.Y - 132)
            };
            AddChild(_actionBar);
        }

        // 7. Pouch Window (Centered initial modal)
        _pouchWindow = GetNodeOrNull<PouchWindow>("PouchWindow");
        if (_pouchWindow == null)
        {
            _pouchWindow = new PouchWindow
            {
                Name = "PouchWindow",
                Position = new Vector2(ViewportSize.X / 2 - 170, ViewportSize.Y / 2 - 180)
            };
            AddChild(_pouchWindow);
        }

        // 8. Hero Details Window (Centered initial modal)
        _heroDetailsWindow = GetNodeOrNull<HeroDetailsWindow>("HeroDetailsWindow");
        if (_heroDetailsWindow == null)
        {
            _heroDetailsWindow = new HeroDetailsWindow
            {
                Name = "HeroDetailsWindow",
                Position = new Vector2(ViewportSize.X / 2 - 520, ViewportSize.Y / 2 - 325)
            };
            AddChild(_heroDetailsWindow);
        }

        // 9. Crafting Window (Modal for crafting cave installations)
        _craftingWindow = GetNodeOrNull<CraftingWindow>("CraftingWindow");
        if (_craftingWindow == null)
        {
            _craftingWindow = new CraftingWindow
            {
                Name = "CraftingWindow",
                Position = new Vector2(ViewportSize.X / 2 - 290, ViewportSize.Y / 2 - 280)
            };
            AddChild(_craftingWindow);
        }

        // 10. Chest Inventory Window (Modal for storage chests)
        _chestInventoryWindow = GetNodeOrNull<ChestInventoryWindow>("ChestInventoryWindow");
        if (_chestInventoryWindow == null)
        {
            _chestInventoryWindow = new ChestInventoryWindow
            {
                Name = "ChestInventoryWindow",
                Position = new Vector2(ViewportSize.X / 2 - 300, ViewportSize.Y / 2 - 225)
            };
            AddChild(_chestInventoryWindow);
        }

        // 11. Item Split Modal (Quantity picker for Shift+click)
        _itemSplitModal = GetNodeOrNull<ItemSplitModal>("ItemSplitModal");
        if (_itemSplitModal == null)
        {
            _itemSplitModal = new ItemSplitModal
            {
                Name = "ItemSplitModal"
            };
            AddChild(_itemSplitModal);
        }

        // 12. Map Window (Toggleable world & cavern map)
        _mapWindow = GetNodeOrNull<MapWindow>("MapWindow");
        if (_mapWindow == null)
        {
            _mapWindow = new MapWindow
            {
                Name = "MapWindow",
                Position = new Vector2(ViewportSize.X / 2 - 450, ViewportSize.Y / 2 - 360)
            };
            AddChild(_mapWindow);
        }

        GameState.Instance.OnChestInventoryToggled += OnChestInventoryToggled;

        // Responsive repositioning on window resize
        var vp = GetViewport();
        if (vp != null)
        {
            vp.SizeChanged += OnViewportSizeChanged;
            OnViewportSizeChanged();
        }
    }

    private void OnChestInventoryToggled(bool isOpen, string? chestId)
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        if (isOpen)
        {
            var size = ViewportSize;
            if (_pouchWindow != null && GodotObject.IsInstanceValid(_pouchWindow))
            {
                _pouchWindow.Position = new Vector2(
                    Mathf.Max(10f, size.X / 2 - 470),
                    Mathf.Clamp(size.Y / 2 - 225, 10f, size.Y - _pouchWindow.Size.Y)
                );
            }
            if (_chestInventoryWindow != null && GodotObject.IsInstanceValid(_chestInventoryWindow))
            {
                _chestInventoryWindow.Position = new Vector2(
                    Mathf.Min(size.X - _chestInventoryWindow.Size.X - 10f, size.X / 2 + 10),
                    Mathf.Clamp(size.Y / 2 - 225, 10f, size.Y - _chestInventoryWindow.Size.Y)
                );
            }
        }
    }

    private void OnGameStatePositionChanged(Vector2 pos)
    {
        if (!GodotObject.IsInstanceValid(this) || _positionLabel == null || !GodotObject.IsInstanceValid(_positionLabel))
            return;

        UpdatePositionText(pos);
    }

    private void OnGameStateHealthChanged(float current, float max)
    {
        if (!GodotObject.IsInstanceValid(this) || _healthOrb == null || !GodotObject.IsInstanceValid(_healthOrb))
            return;

        _healthOrb.CurrentValue = current;
        _healthOrb.MaxValue = max;
    }

    private void OnGameStatePowerChanged(float current, float max)
    {
        if (!GodotObject.IsInstanceValid(this) || _powerOrb == null || !GodotObject.IsInstanceValid(_powerOrb))
            return;

        _powerOrb.CurrentValue = current;
        _powerOrb.MaxValue = max;
    }

    private void UpdatePositionText(Vector2 pos)
    {
        if (GameState.Instance.IsInLair)
        {
            _positionLabel.Text = $"Werewolf's Lair | Hideout ({(int)pos.X}, {(int)pos.Y})";
        }
        else
        {
            Vector2 mapPos = World.WorldManager.ToMapCoordinates(pos);
            string region = World.WorldManager.GetRegionName(pos);
            _positionLabel.Text = $"{region} | Pos: ({(int)mapPos.X}, {(int)mapPos.Y})";
        }
    }

    public override void _Process(double delta)
    {
        if (_positionLabel != null && GodotObject.IsInstanceValid(_positionLabel))
        {
            Vector2 pos = Player != null ? Player.GlobalPosition : GameState.Instance.PlayerPosition;
            UpdatePositionText(pos);
        }

        // Alt key polling for ground loot nameplates
        bool isAltPressed = Input.IsKeyPressed(Key.Alt);
        if (GameState.Instance.IsLootLabelsVisible != isAltPressed)
        {
            GameState.Instance.SetLootLabelsVisible(isAltPressed);
        }
    }

    private void OnViewportSizeChanged()
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        var vp = GetViewport();
        if (vp == null || !GodotObject.IsInstanceValid(vp)) return;

        Vector2 size = vp.GetVisibleRect().Size;
        if (_positionLabel != null && GodotObject.IsInstanceValid(_positionLabel))
        {
            _positionLabel.Position = new Vector2(size.X / 2 - _positionLabel.Size.X / 2, 15);
        }
        if (_healthOrb != null && GodotObject.IsInstanceValid(_healthOrb))
        {
            _healthOrb.Position = new Vector2(24, size.Y - 234);
        }
        if (_powerOrb != null && GodotObject.IsInstanceValid(_powerOrb))
        {
            _powerOrb.Position = new Vector2(size.X - 234, size.Y - 234);
        }
        if (_statsPanel != null && GodotObject.IsInstanceValid(_statsPanel))
        {
            _statsPanel.Position = new Vector2(size.X / 2 - 140, size.Y - 80);
        }
        if (_actionBar != null && GodotObject.IsInstanceValid(_actionBar))
        {
            _actionBar.Position = new Vector2(size.X / 2 + 150, size.Y - 132);
        }
        if (_heroDetailsWindow != null && GodotObject.IsInstanceValid(_heroDetailsWindow))
        {
            _heroDetailsWindow.Position = new Vector2(
                Mathf.Clamp(_heroDetailsWindow.Position.X, 0, Mathf.Max(0, size.X - _heroDetailsWindow.Size.X)),
                Mathf.Clamp(_heroDetailsWindow.Position.Y, 0, Mathf.Max(0, size.Y - _heroDetailsWindow.Size.Y))
            );
        }
        if (_craftingWindow != null && GodotObject.IsInstanceValid(_craftingWindow))
        {
            _craftingWindow.Position = new Vector2(
                Mathf.Clamp(_craftingWindow.Position.X, 0, Mathf.Max(0, size.X - _craftingWindow.Size.X)),
                Mathf.Clamp(_craftingWindow.Position.Y, 0, Mathf.Max(0, size.Y - _craftingWindow.Size.Y))
            );
        }
        if (_chestInventoryWindow != null && GodotObject.IsInstanceValid(_chestInventoryWindow))
        {
            _chestInventoryWindow.Position = new Vector2(
                Mathf.Clamp(_chestInventoryWindow.Position.X, 0, Mathf.Max(0, size.X - _chestInventoryWindow.Size.X)),
                Mathf.Clamp(_chestInventoryWindow.Position.Y, 0, Mathf.Max(0, size.Y - _chestInventoryWindow.Size.Y))
            );
        }
        if (_mapWindow != null && GodotObject.IsInstanceValid(_mapWindow))
        {
            _mapWindow.Position = new Vector2(
                Mathf.Clamp(_mapWindow.Position.X, 0, Mathf.Max(0, size.X - _mapWindow.Size.X)),
                Mathf.Clamp(_mapWindow.Position.Y, 0, Mathf.Max(0, size.Y - _mapWindow.Size.Y))
            );
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_hero_details") ||
            (@event is InputEventKey heroKey && heroKey.Pressed && !heroKey.Echo && heroKey.Keycode == Key.C))
        {
            GameState.Instance.ToggleHeroDetails();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("toggle_pouch") ||
                 (@event is InputEventKey pouchKey && pouchKey.Pressed && !pouchKey.Echo && pouchKey.Keycode == Key.P))
        {
            GameState.Instance.TogglePouch();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("toggle_crafting") ||
                 (@event is InputEventKey craftKey && craftKey.Pressed && !craftKey.Echo && craftKey.Keycode == Key.B))
        {
            GameState.Instance.ToggleCrafting();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("toggle_map") ||
                 (@event is InputEventKey mapKey && mapKey.Pressed && !mapKey.Echo && mapKey.Keycode == Key.M))
        {
            GameState.Instance.ToggleMap();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventKey escKey && escKey.Pressed && !escKey.Echo && escKey.Keycode == Key.Escape)
        {
            if (GameState.Instance.IsMapOpen)
            {
                GameState.Instance.CloseMap();
                GetViewport().SetInputAsHandled();
            }
            else if (GameState.Instance.IsPlacingChest)
            {
                GameState.Instance.CancelChestPlacement();
                GetViewport().SetInputAsHandled();
            }
            else if (GameState.Instance.IsChestInventoryOpen)
            {
                GameState.Instance.CloseChestInventory();
                GetViewport().SetInputAsHandled();
            }
            else if (GameState.Instance.IsCraftingOpen)
            {
                GameState.Instance.ToggleCrafting(false);
                GetViewport().SetInputAsHandled();
            }
            else if (GameState.Instance.IsHeroDetailsOpen)
            {
                GameState.Instance.ToggleHeroDetails();
                GetViewport().SetInputAsHandled();
            }
            else if (GameState.Instance.IsPouchOpen)
            {
                GameState.Instance.TogglePouch();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPositionChanged -= OnGameStatePositionChanged;
            GameState.Instance.OnHealthChanged -= OnGameStateHealthChanged;
            GameState.Instance.OnPowerChanged -= OnGameStatePowerChanged;
            GameState.Instance.OnChestInventoryToggled -= OnChestInventoryToggled;
        }

        var vp = GetViewport();
        if (vp != null && GodotObject.IsInstanceValid(vp))
        {
            vp.SizeChanged -= OnViewportSizeChanged;
        }
    }
}
