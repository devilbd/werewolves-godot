using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class ActionBar : PanelContainer
{
    private TextureButton _heroButton = null!;
    private TextureButton _pouchButton = null!;
    private TextureButton _craftButton = null!;
    private TextureButton _mapButton = null!;
    private Panel _heroSlotBg = null!;
    private Panel _pouchSlotBg = null!;
    private Panel _craftSlotBg = null!;
    private Panel _mapSlotBg = null!;

    private StyleBoxFlat _slotNormalStyle = null!;
    private StyleBoxFlat _slotHoverStyle = null!;

    public const float HeroButtonWidth = 67.5f; // 50% of 135
    public const float HeroButtonHeight = 102.5f; // 50% of 205
    public const float SlotWidth = 74f;
    public const float SlotHeight = 108f;

    public override void _Ready()
    {
        ApplyPanelStyle();
        InitSlotStyles();

        CustomMinimumSize = new Vector2(344, 120);

        var hbox = GetNodeOrNull<HBoxContainer>("HBoxContainer");
        if (hbox == null)
        {
            hbox = new HBoxContainer
            {
                Name = "HBoxContainer",
                Alignment = BoxContainer.AlignmentMode.Center
            };
            hbox.AddThemeConstantOverride("separation", 10);
            AddChild(hbox);

            // --- Slot 1: Hero Details (C) ---
            var heroSlot = new Control
            {
                Name = "HeroSlot",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight)
            };

            _heroSlotBg = new Panel
            {
                Name = "SlotBg",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Ignore
            };
            _heroSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            heroSlot.AddChild(_heroSlotBg);

            _heroButton = new TextureButton
            {
                Name = "HeroButton",
                TextureNormal = GD.Load<Texture2D>("res://assets/werewolf/werewolf_head.png"),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(HeroButtonWidth, HeroButtonHeight),
                Size = new Vector2(HeroButtonWidth, HeroButtonHeight),
                Position = new Vector2((SlotWidth - HeroButtonWidth) / 2f, (SlotHeight - HeroButtonHeight) / 2f),
                TooltipText = "Hero Details (C)"
            };
            _heroButton.Pressed += () => GameState.Instance.ToggleHeroDetails();
            _heroButton.MouseEntered += () => _heroSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
            _heroButton.MouseExited += () => _heroSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            heroSlot.AddChild(_heroButton);

            var heroKeyLabel = new Label
            {
                Name = "KeyLabel",
                Text = "C",
                Position = new Vector2(5, 3),
                Size = new Vector2(20, 20),
                MouseFilter = MouseFilterEnum.Ignore
            };
            heroKeyLabel.AddThemeFontSizeOverride("font_size", 12);
            heroKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
            heroKeyLabel.AddThemeConstantOverride("outline_size", 3);
            heroKeyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            heroSlot.AddChild(heroKeyLabel);

            hbox.AddChild(heroSlot);

            // --- Slot 2: Inventory Pouch (P) ---
            var pouchSlot = new Control
            {
                Name = "PouchSlot",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight)
            };

            _pouchSlotBg = new Panel
            {
                Name = "SlotBg",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Ignore
            };
            _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            pouchSlot.AddChild(_pouchSlotBg);

            _pouchButton = new TextureButton
            {
                Name = "PouchButton",
                TextureNormal = GD.Load<Texture2D>("res://assets/pouch_bag_o.png"),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(56, 56),
                Size = new Vector2(56, 56),
                Position = new Vector2((SlotWidth - 56) / 2f, (SlotHeight - 56) / 2f),
                TooltipText = "Inventory Pouch (P)"
            };
            _pouchButton.Pressed += () => GameState.Instance.TogglePouch();
            _pouchButton.MouseEntered += () => _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
            _pouchButton.MouseExited += () => _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            pouchSlot.AddChild(_pouchButton);

            var pouchKeyLabel = new Label
            {
                Name = "KeyLabel",
                Text = "P",
                Position = new Vector2(5, 3),
                Size = new Vector2(20, 20),
                MouseFilter = MouseFilterEnum.Ignore
            };
            pouchKeyLabel.AddThemeFontSizeOverride("font_size", 12);
            pouchKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
            pouchKeyLabel.AddThemeConstantOverride("outline_size", 3);
            pouchKeyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            pouchSlot.AddChild(pouchKeyLabel);

            hbox.AddChild(pouchSlot);

            // --- Slot 3: Crafting (B) ---
            var craftSlot = new Control
            {
                Name = "CraftSlot",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight)
            };

            _craftSlotBg = new Panel
            {
                Name = "SlotBg",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Ignore
            };
            _craftSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            craftSlot.AddChild(_craftSlotBg);

            _craftButton = new TextureButton
            {
                Name = "CraftButton",
                TextureNormal = GD.Load<Texture2D>("res://assets/cave-objects/crafting-table.png"),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(56, 56),
                Size = new Vector2(56, 56),
                Position = new Vector2((SlotWidth - 56) / 2f, (SlotHeight - 56) / 2f),
                TooltipText = "Cave Crafting (B)"
            };
            _craftButton.Pressed += () => GameState.Instance.ToggleCrafting();
            _craftButton.MouseEntered += () => _craftSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
            _craftButton.MouseExited += () => _craftSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            craftSlot.AddChild(_craftButton);

            var craftKeyLabel = new Label
            {
                Name = "KeyLabel",
                Text = "B",
                Position = new Vector2(5, 3),
                Size = new Vector2(20, 20),
                MouseFilter = MouseFilterEnum.Ignore
            };
            craftKeyLabel.AddThemeFontSizeOverride("font_size", 12);
            craftKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
            craftKeyLabel.AddThemeConstantOverride("outline_size", 3);
            craftKeyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            craftSlot.AddChild(craftKeyLabel);

            hbox.AddChild(craftSlot);

            // --- Slot 4: Map (M) ---
            var mapSlot = new Control
            {
                Name = "MapSlot",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight)
            };

            _mapSlotBg = new Panel
            {
                Name = "SlotBg",
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                Size = new Vector2(SlotWidth, SlotHeight),
                MouseFilter = MouseFilterEnum.Ignore
            };
            _mapSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            mapSlot.AddChild(_mapSlotBg);

            _mapButton = new TextureButton
            {
                Name = "MapButton",
                TextureNormal = GD.Load<Texture2D>("res://assets/icons/map_icon.png"),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(56, 56),
                Size = new Vector2(56, 56),
                Position = new Vector2((SlotWidth - 56) / 2f, (SlotHeight - 56) / 2f),
                TooltipText = "World & Cavern Map (M)"
            };
            _mapButton.Pressed += () => GameState.Instance.ToggleMap();
            _mapButton.MouseEntered += () => _mapSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
            _mapButton.MouseExited += () => _mapSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
            mapSlot.AddChild(_mapButton);

            var mapKeyLabel = new Label
            {
                Name = "KeyLabel",
                Text = "M",
                Position = new Vector2(5, 3),
                Size = new Vector2(20, 20),
                MouseFilter = MouseFilterEnum.Ignore
            };
            mapKeyLabel.AddThemeFontSizeOverride("font_size", 12);
            mapKeyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
            mapKeyLabel.AddThemeConstantOverride("outline_size", 3);
            mapKeyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            mapSlot.AddChild(mapKeyLabel);

            hbox.AddChild(mapSlot);
        }
        else
        {
            // Connect existing node tree from PackedScene
            var heroSlot = hbox.GetNodeOrNull<Control>("HeroSlot");
            if (heroSlot != null)
            {
                _heroSlotBg = heroSlot.GetNodeOrNull<Panel>("SlotBg")!;
                if (_heroSlotBg != null) _heroSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);

                _heroButton = heroSlot.GetNodeOrNull<TextureButton>("HeroButton")!;
                if (_heroButton != null)
                {
                    _heroButton.Pressed += () => GameState.Instance.ToggleHeroDetails();
                    if (_heroSlotBg != null)
                    {
                        _heroButton.MouseEntered += () => _heroSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                        _heroButton.MouseExited += () => _heroSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                    }
                }
            }

            var pouchSlot = hbox.GetNodeOrNull<Control>("PouchSlot");
            if (pouchSlot != null)
            {
                _pouchSlotBg = pouchSlot.GetNodeOrNull<Panel>("SlotBg")!;
                if (_pouchSlotBg != null) _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);

                _pouchButton = pouchSlot.GetNodeOrNull<TextureButton>("PouchButton")!;
                if (_pouchButton != null)
                {
                    _pouchButton.Pressed += () => GameState.Instance.TogglePouch();
                    if (_pouchSlotBg != null)
                    {
                        _pouchButton.MouseEntered += () => _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                        _pouchButton.MouseExited += () => _pouchSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                    }
                }
            }

            var craftSlot = hbox.GetNodeOrNull<Control>("CraftSlot");
            if (craftSlot != null)
            {
                _craftSlotBg = craftSlot.GetNodeOrNull<Panel>("SlotBg")!;
                if (_craftSlotBg != null) _craftSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);

                _craftButton = craftSlot.GetNodeOrNull<TextureButton>("CraftButton")!;
                if (_craftButton != null)
                {
                    _craftButton.Pressed += () => GameState.Instance.ToggleCrafting();
                    if (_craftSlotBg != null)
                    {
                        _craftButton.MouseEntered += () => _craftSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                        _craftButton.MouseExited += () => _craftSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                    }
                }
            }

            var mapSlot = hbox.GetNodeOrNull<Control>("MapSlot");
            if (mapSlot != null)
            {
                _mapSlotBg = mapSlot.GetNodeOrNull<Panel>("SlotBg")!;
                if (_mapSlotBg != null) _mapSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);

                _mapButton = mapSlot.GetNodeOrNull<TextureButton>("MapButton")!;
                if (_mapButton != null)
                {
                    _mapButton.Pressed += () => GameState.Instance.ToggleMap();
                    if (_mapSlotBg != null)
                    {
                        _mapButton.MouseEntered += () => _mapSlotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                        _mapButton.MouseExited += () => _mapSlotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                    }
                }
            }
        }
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
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.55f),
            ShadowSize = 6
        };
        AddThemeStyleboxOverride("panel", style);
    }

    private void InitSlotStyles()
    {
        _slotNormalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 1f),
            BorderColor = new Color(0.45f, 0.36f, 0.22f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };

        _slotHoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 1f),
            BorderColor = new Color(0.95f, 0.80f, 0.35f, 1.0f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ShadowColor = new Color(1.0f, 0.65f, 0.1f, 0.4f),
            ShadowSize = 4
        };
    }
}
