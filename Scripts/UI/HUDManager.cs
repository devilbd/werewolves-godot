using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.UI;

public partial class HUDManager : CanvasLayer
{
    [Export] public Werewolf? Player { get; set; }

    private Label _areaLabel = null!;
    private OrbGauge _healthOrb = null!;
    private OrbGauge _powerOrb = null!;
    private StatsPanel _statsPanel = null!;
    private TargetPanel _targetPanel = null!;
    private ActionBar _actionBar = null!;
    private PouchWindow _pouchWindow = null!;

    public override void _Ready()
    {
        // 1. Area Label (Top Center)
        _areaLabel = GetNodeOrNull<Label>("AreaLabel");
        if (_areaLabel == null)
        {
            _areaLabel = new Label
            {
                Name = "AreaLabel",
                Text = $"Area: [{GameState.Instance.CurrentMapPosition.X}, {GameState.Instance.CurrentMapPosition.Y}]",
                HorizontalAlignment = HorizontalAlignment.Center,
                Size = new Vector2(200, 30),
                Position = new Vector2(GetViewport().GetVisibleRect().Size.X / 2 - 100, 15)
            };
            _areaLabel.AddThemeFontSizeOverride("font_size", 16);
            _areaLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            AddChild(_areaLabel);
        }

        GameState.Instance.OnMapChanged += (coords) =>
        {
            _areaLabel.Text = $"Area: [{coords.X}, {coords.Y}]";
        };

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
                Position = new Vector2(20, GetViewport().GetVisibleRect().Size.Y - 160)
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

        GameState.Instance.OnHealthChanged += (current, max) =>
        {
            _healthOrb.CurrentValue = current;
            _healthOrb.MaxValue = max;
        };

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
                Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 160, GetViewport().GetVisibleRect().Size.Y - 160)
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

        GameState.Instance.OnPowerChanged += (current, max) =>
        {
            _powerOrb.CurrentValue = current;
            _powerOrb.MaxValue = max;
        };

        // 5. Stats Panel (Bottom Center)
        _statsPanel = GetNodeOrNull<StatsPanel>("StatsPanel");
        if (_statsPanel == null)
        {
            _statsPanel = new StatsPanel
            {
                Name = "StatsPanel",
                Player = Player,
                Position = new Vector2(GetViewport().GetVisibleRect().Size.X / 2 - 140, GetViewport().GetVisibleRect().Size.Y - 80)
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
                Position = new Vector2(GetViewport().GetVisibleRect().Size.X / 2 + 150, GetViewport().GetVisibleRect().Size.Y - 80)
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
                Position = new Vector2(GetViewport().GetVisibleRect().Size.X / 2 - 170, GetViewport().GetVisibleRect().Size.Y / 2 - 180)
            };
            AddChild(_pouchWindow);
        }

        // Responsive repositioning on window resize
        GetViewport().SizeChanged += OnViewportSizeChanged;
        OnViewportSizeChanged();
    }

    private void OnViewportSizeChanged()
    {
        Vector2 size = GetViewport().GetVisibleRect().Size;
        _areaLabel.Position = new Vector2(size.X / 2 - 100, 15);
        _healthOrb.Position = new Vector2(20, size.Y - 160);
        _powerOrb.Position = new Vector2(size.X - 160, size.Y - 160);
        _statsPanel.Position = new Vector2(size.X / 2 - 140, size.Y - 80);
        _actionBar.Position = new Vector2(size.X / 2 + 150, size.Y - 80);
    }
}
