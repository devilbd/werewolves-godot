using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class BloodJuicerWindow : Control
{
    private Panel _mainPanel = null!;
    private Button _closeButton = null!;

    private Label _placedMeatLabel = null!;
    private Label _pouchMeatLabel = null!;
    private Button _placeOneBtn = null!;
    private Button _placeAllBtn = null!;
    private Button _retrieveBtn = null!;

    private TextureRect _flaskIcon = null!;
    private Label _targetFlaskStatusLabel = null!;
    private Button _juiceOneBtn = null!;
    private Button _juiceAllBtn = null!;

    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(560, 420);
        Size = new Vector2(560, 420);

        BuildWindow();

        Visible = false;
        GameState.Instance.OnBloodJuicerToggled += OnBloodJuicerToggled;
        GameState.Instance.OnBloodJuicerStateChanged += RefreshUI;
        GameState.Instance.OnPouchChanged += RefreshUI;

        RefreshUI();
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnBloodJuicerToggled -= OnBloodJuicerToggled;
            GameState.Instance.OnBloodJuicerStateChanged -= RefreshUI;
            GameState.Instance.OnPouchChanged -= RefreshUI;
        }
    }

    private void BuildWindow()
    {
        _mainPanel = new Panel
        {
            Name = "MainPanel",
            CustomMinimumSize = new Vector2(560, 420),
            Size = new Vector2(560, 420),
            MouseFilter = MouseFilterEnum.Stop
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.11f, 0.96f),
            BorderColor = new Color(0.85f, 0.22f, 0.28f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            ShadowColor = new Color(0.4f, 0f, 0.05f, 0.35f),
            ShadowSize = 10
        };
        _mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _mainPanel.GuiInput += OnPanelGuiInput;
        AddChild(_mainPanel);

        // Header
        var header = new HBoxContainer
        {
            Name = "Header",
            Position = new Vector2(16, 12),
            Size = new Vector2(528, 36)
        };
        _mainPanel.AddChild(header);

        var titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "Blood Juicer & Vitae Press",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.85f));
        titleLabel.AddThemeConstantOverride("outline_size", 3);
        titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        header.AddChild(titleLabel);

        _closeButton = new Button
        {
            Name = "CloseButton",
            Text = "X",
            CustomMinimumSize = new Vector2(28, 28),
            Size = new Vector2(28, 28)
        };
        _closeButton.Pressed += () => GameState.Instance.ToggleBloodJuicer(false);
        ApplyCloseButtonStyle(_closeButton);
        header.AddChild(_closeButton);

        // Subtitle
        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = "Extract life vitae from raw meat to replenish empty or partial blood flasks.",
            Position = new Vector2(18, 48),
            Size = new Vector2(524, 22)
        };
        subtitle.AddThemeFontSizeOverride("font_size", 12);
        subtitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.70f, 0.72f));
        _mainPanel.AddChild(subtitle);

        // Main Workspace Area (2 Chambers + Conduit)
        var chambersBox = new HBoxContainer
        {
            Position = new Vector2(16, 80),
            Size = new Vector2(528, 220)
        };
        chambersBox.AddThemeConstantOverride("separation", 16);
        _mainPanel.AddChild(chambersBox);

        // 1. Meat Chamber
        var meatPanel = CreateChamberPanel("Meat Chamber", new Color(0.12f, 0.08f, 0.09f, 0.9f));
        chambersBox.AddChild(meatPanel);

        var meatVBox = new VBoxContainer
        {
            Position = new Vector2(10, 10),
            Size = new Vector2(180, 200),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        meatVBox.AddThemeConstantOverride("separation", 8);
        meatPanel.AddChild(meatVBox);

        var meatIcon = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/meat_collected_o.png"),
            CustomMinimumSize = new Vector2(54, 54),
            Size = new Vector2(54, 54),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        meatVBox.AddChild(meatIcon);

        _placedMeatLabel = new Label
        {
            Text = "Placed Meat: 0",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _placedMeatLabel.AddThemeFontSizeOverride("font_size", 14);
        _placedMeatLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.85f));
        meatVBox.AddChild(_placedMeatLabel);

        _pouchMeatLabel = new Label
        {
            Text = "(In Pouch: 0)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _pouchMeatLabel.AddThemeFontSizeOverride("font_size", 11);
        _pouchMeatLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.65f, 0.65f));
        meatVBox.AddChild(_pouchMeatLabel);

        var meatBtnsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        meatBtnsRow.AddThemeConstantOverride("separation", 6);

        _placeOneBtn = new Button { Text = "+1 Meat", CustomMinimumSize = new Vector2(52, 26) };
        ApplySmallButtonStyle(_placeOneBtn);
        _placeOneBtn.Pressed += () => GameState.Instance.PlaceMeatInJuicer(1);
        meatBtnsRow.AddChild(_placeOneBtn);

        _placeAllBtn = new Button { Text = "+All", CustomMinimumSize = new Vector2(44, 26) };
        ApplySmallButtonStyle(_placeAllBtn);
        _placeAllBtn.Pressed += () =>
        {
            int pMeat = GameState.Instance.GetPouchItemCount("Meat");
            if (pMeat > 0) GameState.Instance.PlaceMeatInJuicer(pMeat);
        };
        meatBtnsRow.AddChild(_placeAllBtn);

        _retrieveBtn = new Button { Text = "Take", CustomMinimumSize = new Vector2(44, 26) };
        ApplySmallButtonStyle(_retrieveBtn);
        _retrieveBtn.Pressed += () => GameState.Instance.RetrieveMeatFromJuicer(1);
        meatBtnsRow.AddChild(_retrieveBtn);

        meatVBox.AddChild(meatBtnsRow);

        // 2. Extraction Center Conduit
        var conduitVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(80, 200),
            Size = new Vector2(80, 200),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        conduitVBox.AddThemeConstantOverride("separation", 6);
        chambersBox.AddChild(conduitVBox);

        var arrowLbl = new Label
        {
            Text = ">> >> >>",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        arrowLbl.AddThemeFontSizeOverride("font_size", 14);
        arrowLbl.AddThemeColorOverride("font_color", new Color(0.95f, 0.30f, 0.35f));
        conduitVBox.AddChild(arrowLbl);

        var rateLbl = new Label
        {
            Text = "1 Meat\n=\n+50% Blood",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        rateLbl.AddThemeFontSizeOverride("font_size", 11);
        rateLbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.70f));
        conduitVBox.AddChild(rateLbl);

        // 3. Flask Chamber
        var flaskPanel = CreateChamberPanel("Target Flask", new Color(0.09f, 0.09f, 0.13f, 0.9f));
        chambersBox.AddChild(flaskPanel);

        var flaskVBox = new VBoxContainer
        {
            Position = new Vector2(10, 10),
            Size = new Vector2(180, 200),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        flaskVBox.AddThemeConstantOverride("separation", 8);
        flaskPanel.AddChild(flaskVBox);

        _flaskIcon = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/flasks/blood_flask_0.png"),
            CustomMinimumSize = new Vector2(54, 54),
            Size = new Vector2(54, 54),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        flaskVBox.AddChild(_flaskIcon);

        _targetFlaskStatusLabel = new Label
        {
            Text = "Ready to fill",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _targetFlaskStatusLabel.AddThemeFontSizeOverride("font_size", 12);
        _targetFlaskStatusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.85f));
        flaskVBox.AddChild(_targetFlaskStatusLabel);

        // Action Buttons Row (Bottom)
        var actionsRow = new HBoxContainer
        {
            Position = new Vector2(16, 315),
            Size = new Vector2(528, 42),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actionsRow.AddThemeConstantOverride("separation", 16);
        _mainPanel.AddChild(actionsRow);

        _juiceOneBtn = new Button
        {
            Text = "Extract Blood (-1 Meat, +50%)",
            CustomMinimumSize = new Vector2(230, 40)
        };
        ApplyPrimaryButtonStyle(_juiceOneBtn);
        _juiceOneBtn.Pressed += OnJuiceOnePressed;
        actionsRow.AddChild(_juiceOneBtn);

        _juiceAllBtn = new Button
        {
            Text = "Extract All Available",
            CustomMinimumSize = new Vector2(180, 40)
        };
        ApplySecondaryButtonStyle(_juiceAllBtn);
        _juiceAllBtn.Pressed += OnJuiceAllPressed;
        actionsRow.AddChild(_juiceAllBtn);

        // Footer hint
        var footerHint = new Label
        {
            Text = "Hint: You can also right-click Meat directly in your Pouch while this window is open.",
            Position = new Vector2(18, 368),
            Size = new Vector2(524, 24),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        footerHint.AddThemeFontSizeOverride("font_size", 11);
        footerHint.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.70f));
        _mainPanel.AddChild(footerHint);
    }

    private Panel CreateChamberPanel(string title, Color bg)
    {
        var panel = new Panel
        {
            CustomMinimumSize = new Vector2(200, 220),
            Size = new Vector2(200, 220)
        };
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = new Color(0.55f, 0.35f, 0.35f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var titleLbl = new Label
        {
            Text = title,
            Position = new Vector2(10, 8),
            Size = new Vector2(180, 20),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLbl.AddThemeFontSizeOverride("font_size", 12);
        titleLbl.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.75f));
        panel.AddChild(titleLbl);

        return panel;
    }

    private void OnJuiceOnePressed()
    {
        GameState.Instance.JuiceMeat(preferPlaced: true);
        RefreshUI();
    }

    private void OnJuiceAllPressed()
    {
        int count = 0;
        while (count < 20 && GameState.Instance.JuiceMeat(preferPlaced: true))
        {
            count++;
        }
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (!GodotObject.IsInstanceValid(this) || _placedMeatLabel == null) return;

        int placed = GameState.Instance.BloodJuicerMeats;
        int inPouch = GameState.Instance.GetPouchItemCount("Meat");

        _placedMeatLabel.Text = $"Placed Meat: {placed}";
        _pouchMeatLabel.Text = $"(In Pouch: {inPouch})";

        _placeOneBtn.Disabled = inPouch <= 0;
        _placeAllBtn.Disabled = inPouch <= 0;
        _retrieveBtn.Disabled = placed <= 0;

        // Find candidate flask
        string? candidateKey = null;
        PouchItemData? candidateFlask = null;
        foreach (var kvp in GameState.Instance.PouchItems)
        {
            if (kvp.Key.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase) && kvp.Value.BloodPercent < 100)
            {
                if (candidateFlask == null || kvp.Value.BloodPercent > candidateFlask.BloodPercent)
                {
                    candidateKey = kvp.Key;
                    candidateFlask = kvp.Value;
                }
            }
        }

        bool hasEmptyFlask = GameState.Instance.PouchItems.TryGetValue("EmptyFlask", out var empty) && empty.Count > 0;
        bool hasMeat = placed > 0 || inPouch > 0;

        if (candidateFlask != null)
        {
            int currentPct = candidateFlask.BloodPercent;
            int nextPct = Math.Min(100, currentPct + 50);
            string texPath = GetFlaskTexture(currentPct);
            _flaskIcon.Texture = GD.Load<Texture2D>(texPath);
            _targetFlaskStatusLabel.Text = $"Partial Blood Flask\n{currentPct}%  ➔  {nextPct}%";
            _targetFlaskStatusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.45f));
            _juiceOneBtn.Disabled = !hasMeat;
            _juiceAllBtn.Disabled = !hasMeat;
        }
        else if (hasEmptyFlask)
        {
            _flaskIcon.Texture = GD.Load<Texture2D>("res://assets/flasks/blood_flask_0.png");
            _targetFlaskStatusLabel.Text = $"Empty Flask\n0%  ➔  50%";
            _targetFlaskStatusLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.95f, 0.70f));
            _juiceOneBtn.Disabled = !hasMeat;
            _juiceAllBtn.Disabled = !hasMeat;
        }
        else
        {
            _flaskIcon.Texture = GD.Load<Texture2D>("res://assets/flasks/blood_flask_0.png");
            _targetFlaskStatusLabel.Text = "No Empty or Partial\nFlasks in Pouch!";
            _targetFlaskStatusLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.45f, 0.45f));
            _juiceOneBtn.Disabled = true;
            _juiceAllBtn.Disabled = true;
        }
    }

    private static string GetFlaskTexture(int pct)
    {
        if (pct >= 85) return "res://assets/flasks/blood_flask_100.png";
        if (pct >= 60) return "res://assets/flasks/blood_flask_75.png";
        if (pct >= 35) return "res://assets/flasks/blood_flask_50.png";
        if (pct >= 10) return "res://assets/flasks/blood_flask_25.png";
        return "res://assets/flasks/blood_flask_0.png";
    }

    private void OnBloodJuicerToggled(bool visible)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Visible = visible;
        if (visible)
        {
            if (!GameState.Instance.IsPouchOpen)
            {
                GameState.Instance.TogglePouch();
            }
            RefreshUI();
        }
    }

    private void OnPanelGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - Position;
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDragging && @event is InputEventMouseMotion)
        {
            Position = GetGlobalMousePosition() - _dragOffset;
            var vpSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, vpSize.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, vpSize.Y - Size.Y))
            );
        }
    }

    private static void ApplyPrimaryButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.55f, 0.12f, 0.16f, 1.0f),
            BorderColor = new Color(0.95f, 0.35f, 0.40f, 0.95f),
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
            BgColor = new Color(0.70f, 0.16f, 0.22f, 1.0f),
            BorderColor = new Color(1.0f, 0.60f, 0.65f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var disabled = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.08f, 0.09f, 0.7f),
            BorderColor = new Color(0.35f, 0.25f, 0.25f, 0.5f),
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
        btn.AddThemeStyleboxOverride("disabled", disabled);
        btn.AddThemeColorOverride("font_color", new Color(1.0f, 0.95f, 0.90f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.45f, 0.45f));
        btn.AddThemeFontSizeOverride("font_size", 13);
    }

    private static void ApplySecondaryButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.20f, 0.16f, 0.18f, 1.0f),
            BorderColor = new Color(0.65f, 0.45f, 0.40f, 0.85f),
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
            BgColor = new Color(0.32f, 0.22f, 0.25f, 1.0f),
            BorderColor = new Color(0.90f, 0.65f, 0.55f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var disabled = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.11f, 0.7f),
            BorderColor = new Color(0.30f, 0.25f, 0.25f, 0.5f),
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
        btn.AddThemeStyleboxOverride("disabled", disabled);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.85f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.50f, 0.45f, 0.45f));
        btn.AddThemeFontSizeOverride("font_size", 12);
    }

    private static void ApplySmallButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.22f, 0.14f, 0.16f, 1.0f),
            BorderColor = new Color(0.60f, 0.35f, 0.35f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.18f, 0.22f, 1.0f),
            BorderColor = new Color(0.85f, 0.50f, 0.50f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        var disabled = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.10f, 0.6f),
            BorderColor = new Color(0.30f, 0.25f, 0.25f, 0.4f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("disabled", disabled);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.85f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.45f, 0.45f, 0.45f));
        btn.AddThemeFontSizeOverride("font_size", 10);
    }

    private static void ApplyCloseButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.08f, 0.08f, 0.8f),
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
        btn.AddThemeFontSizeOverride("font_size", 13);
    }
}
