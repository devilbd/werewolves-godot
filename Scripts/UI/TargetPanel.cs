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
    private Panel _circleBg = null!;
    private Panel _activeRing = null!;
    private Label _actionLabel = null!;
    private Button? _closeButton;

    private Texture2D _attackIcon = null!;
    private Texture2D _chopIcon = null!;
    private Texture2D _quarryIcon = null!;

    public override void _Ready()
    {
        _attackIcon = GD.Load<Texture2D>("res://assets/icons/simple_attack_menu.png");
        _chopIcon = GD.Load<Texture2D>("res://assets/icons/log_chopping.png");
        _quarryIcon = GD.Load<Texture2D>("res://assets/icons/rock_stone_digging.png");

        CustomMinimumSize = new Vector2(240, 145);
        ApplyPanelStyle();

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
                Text = "✕",
                Flat = true,
                CustomMinimumSize = new Vector2(24, 20)
            };
            _closeButton.AddThemeFontSizeOverride("font_size", 12);
            _closeButton.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f, 0.8f));
            header.AddChild(_closeButton);
        }
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
            _healthBar.Size = new Vector2(216, 18);
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

        // 3. Circled Action Button Container at Bottom
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

        var circleRoot = actionBox.GetNodeOrNull<Control>("CircleRoot");
        if (circleRoot == null)
        {
            circleRoot = new Control
            {
                Name = "CircleRoot",
                CustomMinimumSize = new Vector2(52, 52),
                Size = new Vector2(52, 52)
            };
            actionBox.AddChild(circleRoot);
        }

        // Circular background panel
        _circleBg = circleRoot.GetNodeOrNull<Panel>("CircleBg");
        if (_circleBg == null)
        {
            _circleBg = new Panel
            {
                Name = "CircleBg",
                CustomMinimumSize = new Vector2(52, 52),
                Size = new Vector2(52, 52)
            };
            circleRoot.AddChild(_circleBg);
        }
        ApplyCircleStyle();

        // Circular active glowing ring overlay
        _activeRing = circleRoot.GetNodeOrNull<Panel>("ActiveRing");
        if (_activeRing == null)
        {
            _activeRing = new Panel
            {
                Name = "ActiveRing",
                CustomMinimumSize = new Vector2(52, 52),
                Size = new Vector2(52, 52),
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore
            };
            circleRoot.AddChild(_activeRing);
        }
        ApplyActiveRingStyle();

        // Circular button texture
        _actionButton = circleRoot.GetNodeOrNull<TextureButton>("ActionButton");
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
                Position = new Vector2(4, 4)
            };
            circleRoot.AddChild(_actionButton);
        }
        _actionButton.Pressed += OnActionPressed;

        // Label below circled button
        _actionLabel = actionBox.GetNodeOrNull<Label>("ActionLabel");
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

    private void ApplyCircleStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.14f, 0.18f, 1.0f),
            BorderColor = new Color(0.55f, 0.45f, 0.30f, 1.0f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 26,
            CornerRadiusTopRight = 26,
            CornerRadiusBottomLeft = 26,
            CornerRadiusBottomRight = 26
        };
        _circleBg.AddThemeStyleboxOverride("panel", style);
    }

    private void ApplyActiveRingStyle()
    {
        var style = new StyleBoxFlat
        {
            DrawCenter = false,
            BorderColor = new Color(1.0f, 0.80f, 0.20f, 1.0f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 26,
            CornerRadiusTopRight = 26,
            CornerRadiusBottomLeft = 26,
            CornerRadiusBottomRight = 26,
            ShadowColor = new Color(1.0f, 0.65f, 0.1f, 0.6f),
            ShadowSize = 5
        };
        _activeRing.AddThemeStyleboxOverride("panel", style);
    }

    private void OnTargetChanged(Node2D? target)
    {
        if (target is ISelectableTarget selectable && !selectable.IsDead && selectable.Health > 0f)
        {
            Visible = true;
            _nameLabel.Text = selectable.TargetName;
            _healthBar.MaxValue = selectable.MaxHealth;
            _healthBar.Value = selectable.Health;
            _hpLabel.Text = $"{(int)selectable.Health} / {(int)selectable.MaxHealth}";
        }
        else
        {
            Visible = false;
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
                "Tree" => "Chop",
                "Rock" => "Quarry",
                _ => "Attack"
            };

            Texture2D actionTex = selectable.TargetName switch
            {
                "Tree" => _chopIcon,
                "Rock" => _quarryIcon,
                _ => _attackIcon
            };
            if (_actionButton.TextureNormal != actionTex)
            {
                _actionButton.TextureNormal = actionTex;
            }

            bool isAuto = Player != null && Player.IsAutoInteracting;
            if (isAuto)
            {
                _activeRing.Visible = true;
                float pulse = (Mathf.Sin(Time.GetTicksMsec() * 0.008f) + 1f) * 0.5f;
                _activeRing.Modulate = new Color(1f, 1f, 1f, 0.5f + pulse * 0.5f);
                _actionLabel.Text = $"AUTO {actionName.ToUpper()}";
                _actionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
            }
            else
            {
                _activeRing.Visible = false;
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
