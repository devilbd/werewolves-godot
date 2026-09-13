using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.UI;

public partial class TargetPanel : PanelContainer
{
    [Export] public Werewolf? Player { get; set; }

    private Label _nameLabel = null!;
    private ProgressBar _healthBar = null!;
    private Label _hpLabel = null!;
    private TextureButton _actionButton = null!;
    private Panel _slotBg = null!;
    private Label _actionLabel = null!;
    private Button? _closeButton;

    private StyleBoxFlat _inactiveSlotStyle = null!;
    private StyleBoxFlat _activeSlotStyle = null!;

    private Texture2D _attackIcon = null!;
    private Texture2D _chopIcon = null!;
    private Texture2D _quarryIcon = null!;
    private Texture2D _openIcon = null!;

    public override void _Ready()
    {
        _attackIcon = GD.Load<Texture2D>("res://assets/icons/simple_attack_menu.png");
        _chopIcon = GD.Load<Texture2D>("res://assets/icons/log_chopping.png");
        _quarryIcon = GD.Load<Texture2D>("res://assets/icons/rock_stone_digging.png");
        _openIcon = GD.Load<Texture2D>("res://assets/chests/chest_closed.png");

        CustomMinimumSize = new Vector2(240, 145);
        ApplyPanelStyle();
        InitSlotStyles();

        // Find or create VBoxContainer
        var vbox = GetNodeOrNull<VBoxContainer>("VBoxContainer");
        if (vbox == null)
        {
            vbox = new VBoxContainer { Name = "VBoxContainer" };
            AddChild(vbox);
        }
        vbox.AddThemeConstantOverride("separation", 6);

        // 1. Header (Name + Close Button)
        var header = vbox.GetNodeOrNull<HBoxContainer>("Header");
        if (header == null)
        {
            header = new HBoxContainer { Name = "Header" };
            vbox.AddChild(header);
        }

        _nameLabel = header.GetNodeOrNull<Label>("NameLabel");
        if (_nameLabel == null)
        {
            _nameLabel = new Label { Name = "NameLabel", Text = "Target" };
            header.AddChild(_nameLabel);
        }
        _nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _nameLabel.AddThemeFontSizeOverride("font_size", 15);
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.65f));

        // Hide legacy attack button in header if present
        var oldAttackBtn = header.GetNodeOrNull<Button>("AttackButton");
        if (oldAttackBtn != null)
        {
            oldAttackBtn.Visible = false;
        }

        _closeButton = header.GetNodeOrNull<Button>("CloseButton");
        if (_closeButton == null)
        {
            _closeButton = new Button
            {
                Name = "CloseButton",
                Text = "X",
                CustomMinimumSize = new Vector2(22, 22),
                Size = new Vector2(22, 22)
            };
            header.AddChild(_closeButton);
        }
        ApplyCloseButtonStyle();
        _closeButton.Pressed += () => GameState.Instance.SelectedTarget = null;

        // 2. Health Bar Container (ProgressBar + HP text overlay)
        var hpContainer = vbox.GetNodeOrNull<Control>("HealthBarContainer");
        if (hpContainer == null)
        {
            hpContainer = new Control
            {
                Name = "HealthBarContainer",
                CustomMinimumSize = new Vector2(216, 18)
            };
            vbox.AddChild(hpContainer);
        }

        _healthBar = hpContainer.GetNodeOrNull<ProgressBar>("HealthBar") ?? vbox.GetNodeOrNull<ProgressBar>("HealthBar");
        if (_healthBar == null)
        {
            _healthBar = new ProgressBar
            {
                Name = "HealthBar",
                MinValue = 0,
                MaxValue = 100,
                Value = 100,
                CustomMinimumSize = new Vector2(216, 18),
                Size = new Vector2(216, 18),
                ShowPercentage = false
            };
            hpContainer.AddChild(_healthBar);
        }
        else
        {
            if (_healthBar.GetParent() != hpContainer)
            {
                _healthBar.GetParent()?.RemoveChild(_healthBar);
                hpContainer.AddChild(_healthBar);
            }
            _healthBar.CustomMinimumSize = new Vector2(216, 18);
            _healthBar.ShowPercentage = false;
        }
        ApplyHealthBarStyle();

        _hpLabel = hpContainer.GetNodeOrNull<Label>("HpLabel");
        if (_hpLabel == null)
        {
            _hpLabel = new Label
            {
                Name = "HpLabel",
                Text = "100 / 100",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Size = new Vector2(216, 18)
            };
            _hpLabel.AddThemeFontSizeOverride("font_size", 11);
            _hpLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.95f));
            hpContainer.AddChild(_hpLabel);
        }

        // 3. Action Button Slot Container at Bottom
        var centerContainer = vbox.GetNodeOrNull<CenterContainer>("ButtonCenter");
        if (centerContainer == null)
        {
            centerContainer = new CenterContainer
            {
                Name = "ButtonCenter",
                CustomMinimumSize = new Vector2(216, 56)
            };
            vbox.AddChild(centerContainer);
        }

        var actionBox = centerContainer.GetNodeOrNull<VBoxContainer>("ActionBox");
        if (actionBox == null)
        {
            actionBox = new VBoxContainer
            {
                Name = "ActionBox",
                Alignment = BoxContainer.AlignmentMode.Center
            };
            actionBox.AddThemeConstantOverride("separation", 2);
            centerContainer.AddChild(actionBox);
        }

        var slotRoot = actionBox.GetNodeOrNull<Control>("SlotRoot") ?? actionBox.GetNodeOrNull<Control>("CircleRoot");
        if (slotRoot == null)
        {
            slotRoot = new Control
            {
                Name = "SlotRoot",
                CustomMinimumSize = new Vector2(50, 50),
                Size = new Vector2(50, 50)
            };
            actionBox.AddChild(slotRoot);
        }

        // Hide legacy active ring if present
        var legacyRing = slotRoot.GetNodeOrNull<Panel>("ActiveRing");
        if (legacyRing != null)
        {
            legacyRing.Visible = false;
        }

        // Single border slot background panel
        _slotBg = slotRoot.GetNodeOrNull<Panel>("SlotBg") ?? slotRoot.GetNodeOrNull<Panel>("CircleBg")!;
        if (_slotBg == null)
        {
            _slotBg = new Panel
            {
                Name = "SlotBg",
                CustomMinimumSize = new Vector2(50, 50),
                Size = new Vector2(50, 50)
            };
            slotRoot.AddChild(_slotBg);
        }
        _slotBg.AddThemeStyleboxOverride("panel", _inactiveSlotStyle);

        // Action button texture
        _actionButton = slotRoot.GetNodeOrNull<TextureButton>("ActionButton")!;
        if (_actionButton == null)
        {
            _actionButton = new TextureButton
            {
                Name = "ActionButton",
                TextureNormal = _attackIcon,
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.Scale,
                CustomMinimumSize = new Vector2(44, 44),
                Size = new Vector2(44, 44),
                Position = new Vector2(3, 3)
            };
            slotRoot.AddChild(_actionButton);
        }
        else
        {
            _actionButton.Position = new Vector2(3, 3);
            _actionButton.Size = new Vector2(44, 44);
        }
        _actionButton.Pressed += OnActionPressed;

        // Label below action button
        _actionLabel = actionBox.GetNodeOrNull<Label>("ActionLabel")!;
        if (_actionLabel == null)
        {
            _actionLabel = new Label
            {
                Name = "ActionLabel",
                Text = "Attack",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _actionLabel.AddThemeFontSizeOverride("font_size", 11);
            _actionLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
            actionBox.AddChild(_actionLabel);
        }

        Visible = false;
        GameState.Instance.OnTargetChanged += OnTargetChanged;
    }

    private void ApplyPanelStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 0.92f),
            BorderColor = new Color(0.65f, 0.52f, 0.32f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.55f),
            ShadowSize = 6
        };
        AddThemeStyleboxOverride("panel", style);
    }

    private void ApplyHealthBarStyle()
    {
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.06f, 0.06f, 0.95f),
            BorderColor = new Color(0.35f, 0.18f, 0.18f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var fill = new StyleBoxFlat
        {
            BgColor = new Color(0.80f, 0.16f, 0.16f, 1.0f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _healthBar.AddThemeStyleboxOverride("background", bg);
        _healthBar.AddThemeStyleboxOverride("fill", fill);
    }

    private void ApplyCloseButtonStyle()
    {
        if (_closeButton == null) return;
        _closeButton.Text = "X";
        _closeButton.CustomMinimumSize = new Vector2(22, 22);
        _closeButton.Size = new Vector2(22, 22);

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.14f, 0.14f, 0.75f),
            BorderColor = new Color(0.55f, 0.40f, 0.30f, 0.65f),
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
            BgColor = new Color(0.65f, 0.15f, 0.15f, 0.95f),
            BorderColor = new Color(0.95f, 0.40f, 0.40f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var pressed = new StyleBoxFlat
        {
            BgColor = new Color(0.45f, 0.10f, 0.10f, 0.95f),
            BorderColor = new Color(0.85f, 0.25f, 0.25f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };

        _closeButton.AddThemeStyleboxOverride("normal", normal);
        _closeButton.AddThemeStyleboxOverride("hover", hover);
        _closeButton.AddThemeStyleboxOverride("pressed", pressed);
        _closeButton.AddThemeColorOverride("font_color", new Color(0.85f, 0.80f, 0.75f));
        _closeButton.AddThemeColorOverride("font_hover_color", Colors.White);
        _closeButton.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.8f, 0.8f));
        _closeButton.AddThemeFontSizeOverride("font_size", 12);
    }

    private void InitSlotStyles()
    {
        _inactiveSlotStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.14f, 0.18f, 1.0f),
            BorderColor = new Color(0.45f, 0.38f, 0.25f, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };

        _activeSlotStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.20f, 0.17f, 0.10f, 1.0f),
            BorderColor = new Color(1.0f, 0.82f, 0.25f, 1.0f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ShadowColor = new Color(1.0f, 0.65f, 0.1f, 0.45f),
            ShadowSize = 5
        };
    }

    private void OnTargetChanged(Node2D? target)
    {
        if (!GodotObject.IsInstanceValid(this) || _healthBar == null || !GodotObject.IsInstanceValid(_healthBar))
            return;

        if (target is ISelectableTarget selectable && !selectable.IsDead && selectable.Health > 0f)
        {
            Visible = true;
            if (_nameLabel != null && GodotObject.IsInstanceValid(_nameLabel))
                _nameLabel.Text = selectable.TargetName;
            _healthBar.MaxValue = selectable.MaxHealth;
            _healthBar.Value = selectable.Health;
            if (_hpLabel != null && GodotObject.IsInstanceValid(_hpLabel))
                _hpLabel.Text = $"{(int)selectable.Health} / {(int)selectable.MaxHealth}";
        }
        else
        {
            Visible = false;
        }
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnTargetChanged -= OnTargetChanged;
        }
    }

    public override void _Process(double delta)
    {
        if (Visible && GameState.Instance.SelectedTarget is ISelectableTarget selectable)
        {
            if (selectable.IsDead || selectable.Health <= 0f)
            {
                Visible = false;
                return;
            }

            _healthBar.MaxValue = selectable.MaxHealth;
            _healthBar.Value = selectable.Health;
            _hpLabel.Text = $"{(int)selectable.Health} / {(int)selectable.MaxHealth}";

            string actionName = selectable.TargetName switch
            {
                "Tree" or "Pine Tree" => "Chop",
                "Rock" or "Quarry Boulder" => "Quarry",
                "Quartz" or "Quartz Crystal" => "Chop",
                "Grass" => "Chop",
                "Treasure Chest" or "Chest" => "Open",
                _ => "Attack"
            };

            Texture2D actionTex = selectable.TargetName switch
            {
                "Tree" or "Pine Tree" => _chopIcon,
                "Rock" or "Quarry Boulder" => _quarryIcon,
                "Quartz" or "Quartz Crystal" => _chopIcon,
                "Grass" => _chopIcon,
                "Treasure Chest" or "Chest" => _openIcon,
                _ => _attackIcon
            };
            if (_actionButton.TextureNormal != actionTex)
            {
                _actionButton.TextureNormal = actionTex;
            }

            bool isAuto = Player != null && Player.IsAutoInteracting;
            if (isAuto)
            {
                _slotBg.AddThemeStyleboxOverride("panel", _activeSlotStyle);
                _actionLabel.Text = $"AUTO {actionName.ToUpper()}";
                _actionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
            }
            else
            {
                _slotBg.AddThemeStyleboxOverride("panel", _inactiveSlotStyle);
                _actionLabel.Text = actionName;
                _actionLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
            }
        }
        else if (Visible && GameState.Instance.SelectedTarget == null)
        {
            Visible = false;
        }
    }

    private void OnActionPressed()
    {
        Player?.ToggleAutoInteract();
    }
}
