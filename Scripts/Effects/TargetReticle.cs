using System;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.Effects;

public partial class TargetReticle : Node2D
{
    [Export] public float CornerRadius { get; set; } = 7f;
    [Export] public float ArmLength { get; set; } = 11f;
    [Export] public float Padding { get; set; } = 4f;

    public override void _Ready()
    {
        ZIndex = 50;
        ZAsRelative = false;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        var target = GameState.Instance.SelectedTarget;
        if (target != null && GodotObject.IsInstanceValid(target) && target is ISelectableTarget selectable && !selectable.IsDead)
        {
            GlobalPosition = target.GlobalPosition;
            Visible = true;
            QueueRedraw();
        }
        else
        {
            if (Visible)
            {
                Visible = false;
            }
        }
    }

    public override void _Draw()
    {
        var target = GameState.Instance.SelectedTarget as ISelectableTarget;
        if (target == null || target.IsDead) return;

        Rect2 bounds = target.TargetBounds.Grow(Padding);
        float left = bounds.Position.X;
        float top = bounds.Position.Y;
        float right = left + bounds.Size.X;
        float bottom = top + bounds.Size.Y;

        float r = Math.Min(CornerRadius, Math.Min(bounds.Size.X, bounds.Size.Y) * 0.35f);
        float arm = ArmLength;

        // Animated subtle breathing shimmer: pulse value between 0.0 and 1.0
        float timeMs = Time.GetTicksMsec();
        float pulse = (Mathf.Sin(timeMs * 0.005f) + 1.0f) * 0.5f;

        // Shiny luminous yet sleek color palette
        Color glowColor = new Color(1.0f, 0.85f, 0.25f, 0.20f + 0.10f * pulse);
        Color coreColor = new Color(1.0f, 0.96f, 0.70f, 0.88f + 0.12f * pulse);

        float glowWidth = 2.8f;
        float coreWidth = 1.5f;

        // Arc Centers
        Vector2 cTL = new Vector2(left + r, top + r);
        Vector2 cTR = new Vector2(right - r, top + r);
        Vector2 cBR = new Vector2(right - r, bottom - r);
        Vector2 cBL = new Vector2(left + r, bottom - r);

        // 1. Subtle soft glow halo line pass
        DrawReticlePass(cTL, cTR, cBR, cBL, left, top, right, bottom, r, arm, glowColor, glowWidth);

        // 2. Crisp, slender shiny core line pass
        DrawReticlePass(cTL, cTR, cBR, cBL, left, top, right, bottom, r, arm, coreColor, coreWidth);
    }

    private void DrawReticlePass(
        Vector2 cTL, Vector2 cTR, Vector2 cBR, Vector2 cBL,
        float left, float top, float right, float bottom,
        float r, float arm, Color color, float width)
    {
        // Top-Left corner (Arc: 180 to 270 deg)
        DrawArc(cTL, r, Mathf.Pi, Mathf.Pi * 1.5f, 16, color, width, antialiased: true);
        DrawLine(new Vector2(left + r, top), new Vector2(left + r + arm, top), color, width, antialiased: true);
        DrawLine(new Vector2(left, top + r), new Vector2(left, top + r + arm), color, width, antialiased: true);

        // Top-Right corner (Arc: 270 to 360 deg)
        DrawArc(cTR, r, Mathf.Pi * 1.5f, Mathf.Pi * 2.0f, 16, color, width, antialiased: true);
        DrawLine(new Vector2(right - r, top), new Vector2(right - r - arm, top), color, width, antialiased: true);
        DrawLine(new Vector2(right, top + r), new Vector2(right, top + r + arm), color, width, antialiased: true);

        // Bottom-Right corner (Arc: 0 to 90 deg)
        DrawArc(cBR, r, 0f, Mathf.Pi * 0.5f, 16, color, width, antialiased: true);
        DrawLine(new Vector2(right - r, bottom), new Vector2(right - r - arm, bottom), color, width, antialiased: true);
        DrawLine(new Vector2(right, bottom - r), new Vector2(right, bottom - r - arm), color, width, antialiased: true);

        // Bottom-Left corner (Arc: 90 to 180 deg)
        DrawArc(cBL, r, Mathf.Pi * 0.5f, Mathf.Pi, 16, color, width, antialiased: true);
        DrawLine(new Vector2(left + r, bottom), new Vector2(left + r + arm, bottom), color, width, antialiased: true);
        DrawLine(new Vector2(left, bottom - r), new Vector2(left, bottom - r - arm), color, width, antialiased: true);
    }
}
