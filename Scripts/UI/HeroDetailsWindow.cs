using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class HeroDetailsWindow : Control
{
    // Werewolf Portrait (Left Side)
    private TextureRect _werewolfTexture = null!;

    // Wooden Sign Board (Right Side / Details Panel)
    private NinePatchRect _woodBoard = null!;
    private Label _nameLabel = null!;
    private Label _subtitleLabel = null!;
    private Button _closeButton = null!;

    // Vitals
    private ProgressBar _healthBar = null!;
    private Label _healthLabel = null!;
    private ProgressBar _powerBar = null!;
    private Label _powerLabel = null!;

    // Stat value labels
    private Label _damageValueLabel = null!;
    private Label _defenseValueLabel = null!;
    private Label _speedValueLabel = null!;
    private Label _accuracyValueLabel = null!;
    private Label _evasionValueLabel = null!;
    private Label _hpRegenValueLabel = null!;
    private Label _powerRegenValueLabel = null!;
    private Label _buffStatusLabel = null!;

    // Dragging window state
    private bool _isDraggingWindow = false;
    private Vector2 _windowDragOffset = Vector2.Zero;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1040, 650);
        Size = new Vector2(1040, 650);
        MouseFilter = MouseFilterEnum.Stop;

        BuildUiStructure();

        _closeButton.Pressed += OnCloseButtonPressed;

        Visible = false;
        GameState.Instance.OnHeroDetailsToggled += OnHeroDetailsToggled;
        GameState.Instance.OnHealthChanged += OnHealthChanged;
        GameState.Instance.OnPowerChanged += OnPowerChanged;

        RefreshStats();
    }

    private void BuildUiStructure()
    {
        // 1. Werewolf Figure on the Left Side (600 width x 650 height)
        _werewolfTexture = GetNodeOrNull<TextureRect>("WerewolfTexture");
        if (_werewolfTexture == null)
        {
            string portraitPath = "res://assets/werewolf/solo_600.png";
            if (!ResourceLoader.Exists(portraitPath))
            {
                portraitPath = "res://assets/werewolf/solo.png";
            }

            _werewolfTexture = new TextureRect
            {
                Name = "WerewolfTexture",
                Texture = GD.Load<Texture2D>(portraitPath),
                Position = new Vector2(0, 0),
                CustomMinimumSize = new Vector2(540, 650),
                Size = new Vector2(540, 650),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Stop
            };
            _werewolfTexture.GuiInput += OnDragGuiInput;
            AddChild(_werewolfTexture);
        }

        // 2. Wooden Sign Board Background on the Right Side (wooden_sign_flat.png)
        _woodBoard = GetNodeOrNull<NinePatchRect>("WoodBoard");
        if (_woodBoard == null)
        {
            // AtlasTexture crops the transparent outer canvas of wooden_sign_flat.png (472x288 wood rect)
            var baseTex = GD.Load<Texture2D>("res://assets/wooden_sign_flat.png");
            var woodAtlas = new AtlasTexture
            {
                Atlas = baseTex,
                Region = new Rect2(64, 160, 472, 288)
            };

            _woodBoard = new NinePatchRect
            {
                Name = "WoodBoard",
                Texture = woodAtlas,
                Position = new Vector2(500, 15),
                CustomMinimumSize = new Vector2(520, 620),
                Size = new Vector2(520, 620),
                PatchMarginLeft = 45,
                PatchMarginRight = 45,
                PatchMarginTop = 45,
                PatchMarginBottom = 45,
                MouseFilter = MouseFilterEnum.Stop
            };
            _woodBoard.GuiInput += OnDragGuiInput;
            AddChild(_woodBoard);
        }

        // 3. Player Name Header (Centered at top of wooden board)
        _nameLabel = _woodBoard.GetNodeOrNull<Label>("NameLabel");
        if (_nameLabel == null)
        {
            _nameLabel = new Label
            {
                Name = "NameLabel",
                Text = GameState.Instance.PlayerName.ToUpper(),
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(40, 22),
                Size = new Vector2(440, 32)
            };
            _nameLabel.AddThemeFontSizeOverride("font_size", 24);
            _nameLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.90f, 0.72f));
            _nameLabel.AddThemeColorOverride("font_shadow_color", new Color(0.10f, 0.05f, 0.02f, 0.95f));
            _nameLabel.AddThemeConstantOverride("shadow_offset_x", 2);
            _nameLabel.AddThemeConstantOverride("shadow_offset_y", 2);
            _nameLabel.AddThemeColorOverride("font_outline_color", new Color(0.18f, 0.10f, 0.04f, 0.95f));
            _nameLabel.AddThemeConstantOverride("outline_size", 3);

            var font = GD.Load<Font>("res://assets/fonts/Monster Blood TTF.ttf");
            if (font != null)
            {
                _nameLabel.AddThemeFontOverride("font", font);
            }
            _woodBoard.AddChild(_nameLabel);
        }

        // Subtitle / Form
        _subtitleLabel = _woodBoard.GetNodeOrNull<Label>("SubtitleLabel");
        if (_subtitleLabel == null)
        {
            _subtitleLabel = new Label
            {
                Name = "SubtitleLabel",
                Text = "Alpha Werewolf • Lycanthrope",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(40, 56),
                Size = new Vector2(440, 20)
            };
            _subtitleLabel.AddThemeFontSizeOverride("font_size", 12);
            _subtitleLabel.AddThemeColorOverride("font_color", new Color(0.80f, 0.70f, 0.52f));
            _woodBoard.AddChild(_subtitleLabel);
        }

        // 4. Close Button (Top right corner of the wooden board)
        _closeButton = _woodBoard.GetNodeOrNull<Button>("CloseButton");
        if (_closeButton == null)
        {
            _closeButton = new Button
            {
                Name = "CloseButton",
                Text = "X",
                Position = new Vector2(464, 16),
                Size = new Vector2(36, 36),
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.None
            };
            _closeButton.AddThemeFontSizeOverride("font_size", 18);
            _closeButton.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.65f));
            _closeButton.AddThemeColorOverride("font_hover_color", Colors.White);
            _closeButton.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.35f, 0.35f));

            var btnNormal = new StyleBoxFlat
            {
                BgColor = new Color(0.16f, 0.09f, 0.05f, 0.95f),
                BorderColor = new Color(0.85f, 0.72f, 0.35f, 0.9f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            var btnHover = new StyleBoxFlat
            {
                BgColor = new Color(0.30f, 0.12f, 0.06f, 0.95f),
                BorderColor = new Color(1.0f, 0.90f, 0.50f, 1.0f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            var btnPressed = new StyleBoxFlat
            {
                BgColor = new Color(0.40f, 0.08f, 0.08f, 0.95f),
                BorderColor = new Color(1.0f, 0.40f, 0.40f, 1.0f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            _closeButton.AddThemeStyleboxOverride("normal", btnNormal);
            _closeButton.AddThemeStyleboxOverride("hover", btnHover);
            _closeButton.AddThemeStyleboxOverride("pressed", btnPressed);

            _woodBoard.AddChild(_closeButton);
        }

        // 5. Vitals Section (Health & Power Bars on the wood)
        BuildVitalsSection();

        // 6. Attributes & Combat Ratings Grid
        BuildAttributesGrid();

        // 7. Bottom Tip
        var tipLabel = new Label
        {
            Name = "TipLabel",
            Text = "Press [C] or [Esc] to close • Drag to move",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(30, 584),
            Size = new Vector2(460, 20)
        };
        tipLabel.AddThemeFontSizeOverride("font_size", 11);
        tipLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.62f, 0.45f));
        _woodBoard.AddChild(tipLabel);
    }

    private void BuildVitalsSection()
    {
        float startX = 35f;
        float startY = 86f;
        float barWidth = 450f;

        // --- Health ---
        var hpTitle = new Label
        {
            Text = "HEALTH",
            Position = new Vector2(startX, startY),
            Size = new Vector2(150, 20)
        };
        hpTitle.AddThemeFontSizeOverride("font_size", 13);
        hpTitle.AddThemeColorOverride("font_color", new Color(0.98f, 0.50f, 0.50f));
        _woodBoard.AddChild(hpTitle);

        _healthLabel = new Label
        {
            Text = "100 / 100",
            HorizontalAlignment = HorizontalAlignment.Right,
            Position = new Vector2(startX + 250, startY),
            Size = new Vector2(200, 20)
        };
        _healthLabel.AddThemeFontSizeOverride("font_size", 13);
        _healthLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.88f));
        _woodBoard.AddChild(_healthLabel);

        _healthBar = new ProgressBar
        {
            Position = new Vector2(startX, startY + 22),
            Size = new Vector2(barWidth, 16),
            ShowPercentage = false,
            MaxValue = GameState.Instance.PlayerMaxHealth,
            Value = GameState.Instance.PlayerHealth
        };
        var hpBg = new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.05f, 0.04f, 0.95f),
            BorderColor = new Color(0.35f, 0.15f, 0.10f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hpFill = new StyleBoxFlat
        {
            BgColor = new Color(0.85f, 0.18f, 0.18f, 1.0f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _healthBar.AddThemeStyleboxOverride("background", hpBg);
        _healthBar.AddThemeStyleboxOverride("fill", hpFill);
        _woodBoard.AddChild(_healthBar);

        // --- Power ---
        float powerY = startY + 48f;
        var pwrTitle = new Label
        {
            Text = "POWER",
            Position = new Vector2(startX, powerY),
            Size = new Vector2(150, 20)
        };
        pwrTitle.AddThemeFontSizeOverride("font_size", 13);
        pwrTitle.AddThemeColorOverride("font_color", new Color(0.40f, 0.80f, 0.98f));
        _woodBoard.AddChild(pwrTitle);

        _powerLabel = new Label
        {
            Text = "100 / 100",
            HorizontalAlignment = HorizontalAlignment.Right,
            Position = new Vector2(startX + 250, powerY),
            Size = new Vector2(200, 20)
        };
        _powerLabel.AddThemeFontSizeOverride("font_size", 13);
        _powerLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.94f, 0.98f));
        _woodBoard.AddChild(_powerLabel);

        _powerBar = new ProgressBar
        {
            Position = new Vector2(startX, powerY + 22),
            Size = new Vector2(barWidth, 16),
            ShowPercentage = false,
            MaxValue = GameState.Instance.PlayerMaxPower,
            Value = GameState.Instance.PlayerPower
        };
        var pwrBg = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.10f, 0.15f, 0.95f),
            BorderColor = new Color(0.12f, 0.28f, 0.38f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var pwrFill = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.65f, 0.90f, 1.0f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _powerBar.AddThemeStyleboxOverride("background", pwrBg);
        _powerBar.AddThemeStyleboxOverride("fill", pwrFill);
        _woodBoard.AddChild(_powerBar);
    }

    private void BuildAttributesGrid()
    {
        // Separator line
        var sep = new ColorRect
        {
            Position = new Vector2(35, 185),
            Size = new Vector2(450, 1),
            Color = new Color(0.60f, 0.48f, 0.28f, 0.65f)
        };
        _woodBoard.AddChild(sep);

        var sectionTitle = new Label
        {
            Text = "ATTRIBUTES & COMBAT RATINGS",
            Position = new Vector2(35, 194),
            Size = new Vector2(450, 22),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        sectionTitle.AddThemeFontSizeOverride("font_size", 12);
        sectionTitle.AddThemeColorOverride("font_color", new Color(0.92f, 0.82f, 0.58f));
        _woodBoard.AddChild(sectionTitle);

        // 2 Columns of Stat Cards on the wood board
        float col1X = 35f;
        float col2X = 265f;
        float rowStartY = 224f;
        float rowHeight = 44f;
        float cardWidth = 220f;

        // Column 1
        _damageValueLabel = CreateStatCard(col1X, rowStartY, cardWidth, "Attack Damage", "25", new Color(1.0f, 0.65f, 0.40f));
        _defenseValueLabel = CreateStatCard(col1X, rowStartY + rowHeight, cardWidth, "Defense / Armor", "10", new Color(0.70f, 0.80f, 0.95f));
        _speedValueLabel = CreateStatCard(col1X, rowStartY + rowHeight * 2, cardWidth, "Movement Speed", "260", new Color(0.50f, 0.92f, 0.70f));
        _accuracyValueLabel = CreateStatCard(col1X, rowStartY + rowHeight * 3, cardWidth, "Accuracy", "80%", new Color(0.95f, 0.88f, 0.50f));

        // Column 2
        _evasionValueLabel = CreateStatCard(col2X, rowStartY, cardWidth, "Evasion Rate", "10%", new Color(0.60f, 0.88f, 0.98f));
        _hpRegenValueLabel = CreateStatCard(col2X, rowStartY + rowHeight, cardWidth, "Health Regen", "+0.1/s", new Color(0.98f, 0.60f, 0.60f));
        _powerRegenValueLabel = CreateStatCard(col2X, rowStartY + rowHeight * 2, cardWidth, "Power Regen", "+0.2/s", new Color(0.50f, 0.80f, 1.0f));
        _buffStatusLabel = CreateStatCard(col2X, rowStartY + rowHeight * 3, cardWidth, "Active Buff", "Normal", new Color(0.85f, 0.75f, 0.60f));
    }

    private Label CreateStatCard(float x, float y, float width, string title, string defaultValue, Color valueColor)
    {
        var panel = new Panel
        {
            Position = new Vector2(x, y),
            Size = new Vector2(width, 38),
            MouseFilter = MouseFilterEnum.Pass
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.06f, 0.85f),
            BorderColor = new Color(0.42f, 0.32f, 0.18f, 0.65f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        panel.AddThemeStyleboxOverride("panel", style);
        _woodBoard.AddChild(panel);

        var titleLabel = new Label
        {
            Text = title,
            Position = new Vector2(10, 3),
            Size = new Vector2(width - 20, 16),
            MouseFilter = MouseFilterEnum.Pass
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 10);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.68f, 0.55f));
        panel.AddChild(titleLabel);

        var valueLabel = new Label
        {
            Text = defaultValue,
            Position = new Vector2(10, 18),
            Size = new Vector2(width - 20, 18),
            MouseFilter = MouseFilterEnum.Pass
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 13);
        valueLabel.AddThemeColorOverride("font_color", valueColor);
        panel.AddChild(valueLabel);

        return valueLabel;
    }

    private void OnCloseButtonPressed()
    {
        GameState.Instance.CloseHeroDetails();
        Visible = false;
    }

    private void OnHeroDetailsToggled(bool visible)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Visible = visible;
        if (visible)
        {
            RefreshStats();
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        if (_healthBar != null && GodotObject.IsInstanceValid(_healthBar))
        {
            _healthBar.MaxValue = max;
            _healthBar.Value = current;
        }
        if (_healthLabel != null && GodotObject.IsInstanceValid(_healthLabel))
        {
            _healthLabel.Text = $"{(int)current} / {(int)max}";
        }
    }

    private void OnPowerChanged(float current, float max)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        if (_powerBar != null && GodotObject.IsInstanceValid(_powerBar))
        {
            _powerBar.MaxValue = max;
            _powerBar.Value = current;
        }
        if (_powerLabel != null && GodotObject.IsInstanceValid(_powerLabel))
        {
            _powerLabel.Text = $"{(int)current} / {(int)max}";
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        // Keep buff status dynamically up to date
        if (GameState.Instance.IsBuffed)
        {
            if (_buffStatusLabel != null && GodotObject.IsInstanceValid(_buffStatusLabel))
            {
                _buffStatusLabel.Text = $"Blood Howl ({(int)GameState.Instance.BuffTimeRemaining}s)";
                _buffStatusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.70f, 0.20f));
            }
            if (_damageValueLabel != null && GodotObject.IsInstanceValid(_damageValueLabel))
            {
                _damageValueLabel.Text = $"{GameState.Instance.BaseDamage:0.0}";
                _damageValueLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.40f));
            }
            if (_defenseValueLabel != null && GodotObject.IsInstanceValid(_defenseValueLabel))
            {
                _defenseValueLabel.Text = $"{GameState.Instance.BaseDefense:0.0}";
                _defenseValueLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.40f));
            }
            if (_speedValueLabel != null && GodotObject.IsInstanceValid(_speedValueLabel))
            {
                _speedValueLabel.Text = $"{GameState.Instance.Speed:0.0}";
                _speedValueLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.40f));
            }
        }
        else
        {
            if (_buffStatusLabel != null && GodotObject.IsInstanceValid(_buffStatusLabel))
            {
                _buffStatusLabel.Text = "Normal";
                _buffStatusLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.68f, 0.55f));
            }
            if (_damageValueLabel != null && GodotObject.IsInstanceValid(_damageValueLabel))
            {
                _damageValueLabel.Text = $"{GameState.Instance.BaseDamage:0.0}";
                _damageValueLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.65f, 0.40f));
            }
            if (_defenseValueLabel != null && GodotObject.IsInstanceValid(_defenseValueLabel))
            {
                _defenseValueLabel.Text = $"{GameState.Instance.BaseDefense:0.0}";
                _defenseValueLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.80f, 0.95f));
            }
            if (_speedValueLabel != null && GodotObject.IsInstanceValid(_speedValueLabel))
            {
                _speedValueLabel.Text = $"{GameState.Instance.Speed:0.0}";
                _speedValueLabel.AddThemeColorOverride("font_color", new Color(0.50f, 0.92f, 0.70f));
            }
        }
    }

    public void RefreshStats()
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        var gs = GameState.Instance;
        if (gs == null) return;

        if (_nameLabel != null && GodotObject.IsInstanceValid(_nameLabel))
        {
            _nameLabel.Text = gs.PlayerName.ToUpper();
        }

        OnHealthChanged(gs.PlayerHealth, gs.PlayerMaxHealth);
        OnPowerChanged(gs.PlayerPower, gs.PlayerMaxPower);

        if (_speedValueLabel != null && GodotObject.IsInstanceValid(_speedValueLabel))
            _speedValueLabel.Text = $"{gs.Speed:0.0}";

        if (_accuracyValueLabel != null && GodotObject.IsInstanceValid(_accuracyValueLabel))
            _accuracyValueLabel.Text = $"{Mathf.RoundToInt(gs.Accuracy * 100)}%";

        if (_evasionValueLabel != null && GodotObject.IsInstanceValid(_evasionValueLabel))
            _evasionValueLabel.Text = $"{Mathf.RoundToInt(gs.Evasion * 100)}%";

        if (_hpRegenValueLabel != null && GodotObject.IsInstanceValid(_hpRegenValueLabel))
            _hpRegenValueLabel.Text = $"+{gs.HealthRegenRate:0.0}/s";

        if (_powerRegenValueLabel != null && GodotObject.IsInstanceValid(_powerRegenValueLabel))
            _powerRegenValueLabel.Text = $"+{gs.PowerRegenRate:0.0}/s";
    }

    private void OnDragGuiInput(InputEvent @event)
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
            var viewportSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, viewportSize.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, viewportSize.Y - Size.Y))
            );
        }
        else if (_isDraggingWindow && @event is InputEventMouseButton mouseBtn && !mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            _isDraggingWindow = false;
        }
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnHeroDetailsToggled -= OnHeroDetailsToggled;
            GameState.Instance.OnHealthChanged -= OnHealthChanged;
            GameState.Instance.OnPowerChanged -= OnPowerChanged;
        }
    }
}
