using System;
using Godot;

namespace Werewolves.UI;

public partial class ItemSplitModal : Control
{
    public static ItemSplitModal? Instance { get; private set; }

    private Panel _dialogPanel = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private TextureRect _itemIcon = null!;
    private Label _itemNameLabel = null!;
    private Label _availableLabel = null!;
    private HSlider _slider = null!;
    private Label _amountLabel = null!;
    private Button _minusBtn = null!;
    private Button _plusBtn = null!;
    private Button _minPresetBtn = null!;
    private Button _halfPresetBtn = null!;
    private Button _allPresetBtn = null!;
    private Button _confirmBtn = null!;
    private Button _cancelBtn = null!;

    private int _maxCount = 1;
    private int _currentAmount = 1;
    private Action<int>? _onConfirmCallback = null;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        // Fill full viewport so background clicks are intercepted
        SetAnchorsPreset(LayoutPreset.FullRect);

        BuildUI();
    }

    private void BuildUI()
    {
        // 1. Dim background
        var dimBg = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.5f),
            MouseFilter = MouseFilterEnum.Stop
        };
        dimBg.SetAnchorsPreset(LayoutPreset.FullRect);
        dimBg.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                Close();
            }
        };
        AddChild(dimBg);

        // 2. Central Dialog Panel (330 x 230)
        _dialogPanel = new Panel
        {
            CustomMinimumSize = new Vector2(330, 230),
            Size = new Vector2(330, 230),
            MouseFilter = MouseFilterEnum.Stop
        };
        var panelBox = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.15f, 0.98f),
            BorderColor = new Color(0.72f, 0.58f, 0.30f, 1f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.6f),
            ShadowSize = 8
        };
        _dialogPanel.AddThemeStyleboxOverride("panel", panelBox);
        AddChild(_dialogPanel);

        // Center on screen
        _dialogPanel.SetAnchorsPreset(LayoutPreset.Center);

        // Header Title
        _titleLabel = new Label
        {
            Text = "Choose Quantity",
            Position = new Vector2(16, 12),
            Size = new Vector2(250, 24)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.45f));
        _dialogPanel.AddChild(_titleLabel);

        // Close X Button
        _closeButton = new Button
        {
            Text = "X",
            Position = new Vector2(292, 10),
            Size = new Vector2(26, 26),
            Flat = true
        };
        _closeButton.AddThemeFontSizeOverride("font_size", 14);
        _closeButton.AddThemeColorOverride("font_color", new Color(0.85f, 0.4f, 0.4f));
        _closeButton.AddThemeColorOverride("font_hover_color", Colors.White);
        _closeButton.Pressed += Close;
        _dialogPanel.AddChild(_closeButton);

        // Item Icon & Name Info
        _itemIcon = new TextureRect
        {
            Position = new Vector2(18, 44),
            Size = new Vector2(40, 40),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _dialogPanel.AddChild(_itemIcon);

        _itemNameLabel = new Label
        {
            Position = new Vector2(66, 42),
            Size = new Vector2(245, 22),
            Text = "Item Name"
        };
        _itemNameLabel.AddThemeFontSizeOverride("font_size", 14);
        _itemNameLabel.AddThemeColorOverride("font_color", Colors.White);
        _dialogPanel.AddChild(_itemNameLabel);

        _availableLabel = new Label
        {
            Position = new Vector2(66, 62),
            Size = new Vector2(245, 18),
            Text = "Available: 0"
        };
        _availableLabel.AddThemeFontSizeOverride("font_size", 11);
        _availableLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
        _dialogPanel.AddChild(_availableLabel);

        // Stepper Row: [-] [ Quantity ] [+]
        _minusBtn = new Button
        {
            Text = "-",
            Position = new Vector2(20, 92),
            Size = new Vector2(36, 30)
        };
        ApplySmallButtonStyle(_minusBtn, new Color(0.20f, 0.22f, 0.28f));
        _minusBtn.Pressed += () => SetAmount(_currentAmount - 1);
        _dialogPanel.AddChild(_minusBtn);

        _amountLabel = new Label
        {
            Position = new Vector2(64, 92),
            Size = new Vector2(202, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "1"
        };
        _amountLabel.AddThemeFontSizeOverride("font_size", 19);
        _amountLabel.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.4f));
        _dialogPanel.AddChild(_amountLabel);

        _plusBtn = new Button
        {
            Text = "+",
            Position = new Vector2(274, 92),
            Size = new Vector2(36, 30)
        };
        ApplySmallButtonStyle(_plusBtn, new Color(0.20f, 0.22f, 0.28f));
        _plusBtn.Pressed += () => SetAmount(_currentAmount + 1);
        _dialogPanel.AddChild(_plusBtn);

        // Slider Row
        _slider = new HSlider
        {
            Position = new Vector2(20, 128),
            Size = new Vector2(290, 20),
            MinValue = 1,
            MaxValue = 10,
            Step = 1,
            Value = 1
        };
        _slider.ValueChanged += (val) => SetAmount((int)val);
        _dialogPanel.AddChild(_slider);

        // Quick Preset Buttons: [ 1 ] [ Half ] [ All ]
        var presetBox = new HBoxContainer
        {
            Position = new Vector2(20, 154),
            Size = new Vector2(290, 24)
        };
        presetBox.AddThemeConstantOverride("separation", 8);
        _dialogPanel.AddChild(presetBox);

        _minPresetBtn = new Button
        {
            Text = "1",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        ApplySmallButtonStyle(_minPresetBtn, new Color(0.16f, 0.18f, 0.24f));
        _minPresetBtn.Pressed += () => SetAmount(1);
        presetBox.AddChild(_minPresetBtn);

        _halfPresetBtn = new Button
        {
            Text = "Half",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        ApplySmallButtonStyle(_halfPresetBtn, new Color(0.16f, 0.18f, 0.24f));
        _halfPresetBtn.Pressed += () => SetAmount(Math.Max(1, _maxCount / 2));
        presetBox.AddChild(_halfPresetBtn);

        _allPresetBtn = new Button
        {
            Text = "All",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        ApplySmallButtonStyle(_allPresetBtn, new Color(0.16f, 0.18f, 0.24f));
        _allPresetBtn.Pressed += () => SetAmount(_maxCount);
        presetBox.AddChild(_allPresetBtn);

        // Bottom Action Buttons: [ Cancel ] [ Confirm / Grab ]
        var actionBox = new HBoxContainer
        {
            Position = new Vector2(20, 186),
            Size = new Vector2(290, 30)
        };
        actionBox.AddThemeConstantOverride("separation", 10);
        _dialogPanel.AddChild(actionBox);

        _cancelBtn = new Button
        {
            Text = "Cancel",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        ApplySmallButtonStyle(_cancelBtn, new Color(0.24f, 0.12f, 0.12f));
        _cancelBtn.Pressed += Close;
        actionBox.AddChild(_cancelBtn);

        _confirmBtn = new Button
        {
            Text = "Grab",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        ApplySmallButtonStyle(_confirmBtn, new Color(0.14f, 0.28f, 0.18f));
        _confirmBtn.Pressed += OnConfirm;
        actionBox.AddChild(_confirmBtn);
    }

    public void Open(string itemName, string displayName, Texture2D? icon, int maxCount, string actionText, Callable onConfirm)
    {
        Open(itemName, displayName, icon, maxCount, actionText, (amount) => onConfirm.Call(amount));
    }

    public void Open(string itemName, string displayName, Texture2D? icon, int maxCount, string actionText, Action<int> onConfirm)
    {
        if (maxCount <= 0) return;

        _maxCount = maxCount;
        _onConfirmCallback = onConfirm;

        _titleLabel.Text = $"{actionText} {displayName}";
        _itemNameLabel.Text = displayName;
        _availableLabel.Text = $"Available: {maxCount}";
        _confirmBtn.Text = actionText;

        if (icon != null)
        {
            _itemIcon.Texture = icon;
            _itemIcon.Visible = true;
        }
        else
        {
            _itemIcon.Visible = false;
        }

        _slider.MinValue = 1;
        _slider.MaxValue = maxCount;
        _halfPresetBtn.Text = $"Half ({Math.Max(1, maxCount / 2)})";
        _allPresetBtn.Text = $"All ({maxCount})";

        // Default to half stack (or 1 if small)
        int defaultVal = Math.Max(1, maxCount / 2);
        SetAmount(defaultVal);

        Visible = true;
        MoveToFront();
    }

    private void SetAmount(int amount)
    {
        _currentAmount = Mathf.Clamp(amount, 1, _maxCount);
        _amountLabel.Text = _currentAmount.ToString();
        if ((int)_slider.Value != _currentAmount)
        {
            _slider.Value = _currentAmount;
        }
    }

    public void Confirm() => OnConfirm();

    private void OnConfirm()
    {
        int amount = _currentAmount;
        var callback = _onConfirmCallback;
        Close();
        callback?.Invoke(amount);
    }

    public void Close()
    {
        Visible = false;
        _onConfirmCallback = null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
            {
                OnConfirm();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Escape)
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
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
}
