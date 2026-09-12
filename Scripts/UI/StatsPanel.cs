using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.UI;

public partial class StatsPanel : PanelContainer
{
    [Export] public Werewolf? Player { get; set; }

    private readonly List<TextureButton> _buttons = new();
    private readonly List<ColorRect> _cooldownOverlays = new();
    private readonly List<Label> _cooldownLabels = new();
    private readonly List<Panel> _slotBgs = new();

    private StyleBoxFlat _slotNormalStyle = null!;
    private StyleBoxFlat _slotHoverStyle = null!;

    private readonly string[] _iconPaths = new[]
    {
        "res://assets/icons/scratch_hit_icon.png",
        "res://assets/icons/charge_attack.png",
        "res://assets/icons/bite.png",
        "res://assets/icons/blood_howling.png"
    };

    private readonly float[] _powerCosts = new[] { 15f, 25f, 20f, 40f };

    public override void _Ready()
    {
        ApplyPanelStyle();
        InitSlotStyles();

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

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var slotContainer = new Control
                {
                    Name = $"Slot{i + 1}",
                    CustomMinimumSize = new Vector2(56, 56)
                };

                var slotBg = new Panel
                {
                    Name = "SlotBg",
                    CustomMinimumSize = new Vector2(56, 56),
                    Size = new Vector2(56, 56),
                    MouseFilter = MouseFilterEnum.Ignore
                };
                slotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                slotContainer.AddChild(slotBg);
                _slotBgs.Add(slotBg);

                var btn = new TextureButton
                {
                    Name = "Button",
                    TextureNormal = GD.Load<Texture2D>(_iconPaths[i]),
                    IgnoreTextureSize = true,
                    StretchMode = TextureButton.StretchModeEnum.Scale,
                    CustomMinimumSize = new Vector2(50, 50),
                    Size = new Vector2(50, 50),
                    Position = new Vector2(3, 3)
                };
                btn.Pressed += () => OnSkillClicked(index);
                btn.MouseEntered += () => slotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                btn.MouseExited += () => slotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                slotContainer.AddChild(btn);
                _buttons.Add(btn);

                // Hotkey label
                var keyLabel = new Label
                {
                    Name = "KeyLabel",
                    Text = (i + 1).ToString(),
                    Position = new Vector2(5, 3),
                    Size = new Vector2(20, 20),
                    MouseFilter = MouseFilterEnum.Ignore
                };
                keyLabel.AddThemeFontSizeOverride("font_size", 12);
                keyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
                keyLabel.AddThemeConstantOverride("outline_size", 3);
                keyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
                slotContainer.AddChild(keyLabel);

                // Cooldown overlay
                var overlay = new ColorRect
                {
                    Name = "CooldownOverlay",
                    Color = new Color(0, 0, 0, 0.75f),
                    Position = new Vector2(3, 3),
                    Size = new Vector2(50, 50),
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                slotContainer.AddChild(overlay);
                _cooldownOverlays.Add(overlay);

                // Cooldown countdown text
                var cdLabel = new Label
                {
                    Name = "CooldownLabel",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Position = new Vector2(3, 3),
                    Size = new Vector2(50, 50),
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                cdLabel.AddThemeFontSizeOverride("font_size", 15);
                cdLabel.AddThemeColorOverride("font_color", Colors.White);
                cdLabel.AddThemeConstantOverride("outline_size", 3);
                cdLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
                slotContainer.AddChild(cdLabel);
                _cooldownLabels.Add(cdLabel);

                hbox.AddChild(slotContainer);
            }
        }
        else
        {
            for (int i = 0; i < 4 && i < hbox.GetChildCount(); i++)
            {
                int index = i;
                var slot = hbox.GetChild<Control>(i);

                var slotBg = slot.GetNodeOrNull<Panel>("SlotBg");
                if (slotBg == null)
                {
                    slotBg = new Panel
                    {
                        Name = "SlotBg",
                        CustomMinimumSize = new Vector2(56, 56),
                        Size = new Vector2(56, 56),
                        MouseFilter = MouseFilterEnum.Ignore
                    };
                    slot.AddChild(slotBg);
                    slot.MoveChild(slotBg, 0);
                }
                slotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                _slotBgs.Add(slotBg);

                var btn = slot.GetNodeOrNull<TextureButton>("Button") ?? slot.GetChild<TextureButton>(0);
                if (btn != null)
                {
                    btn.Position = new Vector2(3, 3);
                    btn.Size = new Vector2(50, 50);
                    btn.Pressed += () => OnSkillClicked(index);
                    btn.MouseEntered += () => slotBg.AddThemeStyleboxOverride("panel", _slotHoverStyle);
                    btn.MouseExited += () => slotBg.AddThemeStyleboxOverride("panel", _slotNormalStyle);
                    _buttons.Add(btn);
                }

                var keyLabel = slot.GetNodeOrNull<Label>("KeyLabel");
                if (keyLabel != null)
                {
                    keyLabel.Position = new Vector2(5, 3);
                    keyLabel.AddThemeFontSizeOverride("font_size", 12);
                    keyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.35f));
                    keyLabel.AddThemeConstantOverride("outline_size", 3);
                    keyLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
                }

                var overlay = slot.GetNodeOrNull<ColorRect>("CooldownOverlay");
                if (overlay != null)
                {
                    overlay.Position = new Vector2(3, 3);
                    overlay.Size = new Vector2(50, 50);
                    overlay.Color = new Color(0, 0, 0, 0.75f);
                    overlay.MouseFilter = MouseFilterEnum.Ignore;
                    _cooldownOverlays.Add(overlay);
                }

                var cdLabel = slot.GetNodeOrNull<Label>("CooldownLabel");
                if (cdLabel != null)
                {
                    cdLabel.Position = new Vector2(3, 3);
                    cdLabel.Size = new Vector2(50, 50);
                    cdLabel.AddThemeFontSizeOverride("font_size", 15);
                    cdLabel.AddThemeColorOverride("font_color", Colors.White);
                    cdLabel.AddThemeConstantOverride("outline_size", 3);
                    cdLabel.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.05f, 0.05f, 0.95f));
                    cdLabel.MouseFilter = MouseFilterEnum.Ignore;
                    _cooldownLabels.Add(cdLabel);
                }
            }
        }

        GameState.Instance.OnCooldownUpdated += OnCooldownUpdated;
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
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
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

    private void OnSkillClicked(int index)
    {
        Player?.UseSkill(index);
    }

    private void OnCooldownUpdated(int index, float remaining, float total)
    {
        if (index >= 0 && index < _cooldownOverlays.Count)
        {
            if (remaining > 0.05f)
            {
                _cooldownOverlays[index].Visible = true;
                _cooldownLabels[index].Visible = true;
                _cooldownLabels[index].Text = remaining >= 1.0f ? $"{Mathf.CeilToInt(remaining)}s" : $"{remaining:0.0}s";
            }
            else
            {
                _cooldownOverlays[index].Visible = false;
                _cooldownLabels[index].Visible = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < _buttons.Count && i < 4; i++)
        {
            float rem = GameState.Instance.SkillCooldownRemaining[i];
            float tot = GameState.Instance.SkillCooldownTotal[i];
            bool onCooldown = rem > 0.05f;

            if (i < _cooldownOverlays.Count && i < _cooldownLabels.Count)
            {
                if (onCooldown)
                {
                    _cooldownOverlays[i].Visible = true;
                    _cooldownLabels[i].Visible = true;
                    _cooldownLabels[i].Text = rem >= 1.0f ? $"{Mathf.CeilToInt(rem)}s" : $"{rem:0.0}s";

                    // Vertical sweep proportional to remaining cooldown
                    float pct = tot > 0f ? Mathf.Clamp(rem / tot, 0f, 1f) : 0f;
                    float h = 50f * pct;
                    _cooldownOverlays[i].Size = new Vector2(50, h);
                    _cooldownOverlays[i].Position = new Vector2(3, 3 + (50 - h));
                }
                else
                {
                    _cooldownOverlays[i].Visible = false;
                    _cooldownLabels[i].Visible = false;
                }
            }

            bool hasPower = GameState.Instance.PlayerPower >= _powerCosts[i];
            _buttons[i].Modulate = (hasPower && !onCooldown)
                ? Colors.White
                : (!hasPower && !onCooldown)
                    ? new Color(0.55f, 0.55f, 0.65f, 0.75f)
                    : new Color(0.35f, 0.35f, 0.35f, 0.65f);
        }
    }
}
