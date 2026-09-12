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

                var btn = new TextureButton
                {
                    Name = "Button",
                    TextureNormal = GD.Load<Texture2D>(_iconPaths[i]),
                    IgnoreTextureSize = true,
                    StretchMode = TextureButton.StretchModeEnum.Scale,
                    Size = new Vector2(56, 56)
                };
                btn.Pressed += () => OnSkillClicked(index);
                slotContainer.AddChild(btn);
                _buttons.Add(btn);

                // Hotkey label
                var keyLabel = new Label
                {
                    Name = "KeyLabel",
                    Text = (i + 1).ToString(),
                    Position = new Vector2(4, 2),
                    Size = new Vector2(20, 20)
                };
                keyLabel.AddThemeFontSizeOverride("font_size", 12);
                keyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
                slotContainer.AddChild(keyLabel);

                // Cooldown overlay
                var overlay = new ColorRect
                {
                    Name = "CooldownOverlay",
                    Color = new Color(0, 0, 0, 0.7f),
                    Size = new Vector2(56, 56),
                    Visible = false
                };
                slotContainer.AddChild(overlay);
                _cooldownOverlays.Add(overlay);

                // Cooldown countdown text
                var cdLabel = new Label
                {
                    Name = "CooldownLabel",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Size = new Vector2(56, 56),
                    Visible = false
                };
                cdLabel.AddThemeFontSizeOverride("font_size", 14);
                cdLabel.AddThemeColorOverride("font_color", Colors.White);
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
                var btn = slot.GetNodeOrNull<TextureButton>("Button") ?? slot.GetChild<TextureButton>(0);
                if (btn != null)
                {
                    btn.Pressed += () => OnSkillClicked(index);
                    _buttons.Add(btn);
                }
                var overlay = slot.GetNodeOrNull<ColorRect>("CooldownOverlay");
                if (overlay != null) _cooldownOverlays.Add(overlay);
                var cdLabel = slot.GetNodeOrNull<Label>("CooldownLabel");
                if (cdLabel != null) _cooldownLabels.Add(cdLabel);
            }
        }

        GameState.Instance.OnCooldownUpdated += OnCooldownUpdated;
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
                _cooldownLabels[index].Text = remaining >= 1.0f ? $"{Mathf.CeilToInt(remaining)}s" : $"{remaining:0.0}s";
            }
            else
            {
                _cooldownOverlays[index].Visible = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        // Update button modulate based on available power
        for (int i = 0; i < _buttons.Count; i++)
        {
            bool hasPower = GameState.Instance.PlayerPower >= _powerCosts[i];
            bool onCooldown = GameState.Instance.IsSkillOnCooldown(i);
            _buttons[i].Modulate = (hasPower && !onCooldown) ? Colors.White : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
    }
}
