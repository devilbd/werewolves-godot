using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.Effects;

/// <summary>
/// Renders animated dashed borders around world objects and the player hero
/// whenever the player is inside fog clouds, providing atmospheric visibility through the mist.
/// </summary>
public partial class FogDashedBorderOverlay : Node2D
{
    [Export] public float DashLength { get; set; } = 7.5f;
    [Export] public float GapLength { get; set; } = 5.0f;
    [Export] public float DashSpeed { get; set; } = 22.0f;
    [Export] public float BoundsPadding { get; set; } = 4.0f;
    [Export] public float MaxRenderDistance { get; set; } = 1350.0f;

    private float _fadeAlpha = 0.0f;
    private float _dashOffset = 0.0f;
    private float _pulseTimer = 0.0f;

    private Node2D? _entitiesContainer;
    private Werewolf? _cachedPlayer;

    public override void _Ready()
    {
        ZIndex = 20; // Sits directly above fog layer (ZIndex 15) and below HUD/reticles
        ZAsRelative = false;
        Visible = false;

        ResolveContainers();
    }

    private void ResolveContainers()
    {
        var parent = GetParent();
        if (parent != null)
        {
            _entitiesContainer = parent.GetNodeOrNull<Node2D>("Entities");
            _cachedPlayer = parent.GetNodeOrNull<Werewolf>("Entities/Werewolf")
                ?? parent.GetNodeOrNull<Werewolf>("Werewolf");
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool inFog = GameState.Instance.IsPlayerInFog;
        float targetAlpha = inFog ? 1.0f : 0.0f;

        _fadeAlpha = Mathf.MoveToward(_fadeAlpha, targetAlpha, dt * 3.5f);

        if (_fadeAlpha <= 0.001f)
        {
            if (Visible)
            {
                Visible = false;
            }
            return;
        }

        if (!Visible)
        {
            Visible = true;
        }

        _dashOffset = (_dashOffset + dt * DashSpeed) % (DashLength + GapLength);
        _pulseTimer += dt * 2.8f;

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_fadeAlpha <= 0.001f) return;

        if (_entitiesContainer == null || !GodotObject.IsInstanceValid(_entitiesContainer))
        {
            ResolveContainers();
        }

        Vector2 playerPos = GameState.Instance.PlayerPosition;
        float pulse = (Mathf.Sin(_pulseTimer) + 1.0f) * 0.5f;

        // 1. Gather all candidates within render distance that are INTO the fog clouds
        var candidates = new List<(Node2D node, Rect2 bounds, Color color, float fogFactor)>();

        // Personal Hero (Werewolf) — bordered only when into the fog clouds!
        Werewolf? playerNode = _cachedPlayer;
        if (playerNode == null || !GodotObject.IsInstanceValid(playerNode))
        {
            playerNode = _entitiesContainer?.GetNodeOrNull<Werewolf>("Werewolf");
            _cachedPlayer = playerNode;
        }

        if (playerNode != null && GodotObject.IsInstanceValid(playerNode) && playerNode.IsInsideTree() && playerNode.Visible)
        {
            float pFog = Werewolves.World.FogZone.GetFogFactorAt(playerNode.GlobalPosition);
            if (pFog > 0.05f || Werewolves.World.FogZone.IsPositionInFog(playerNode.GlobalPosition, 25f))
            {
                candidates.Add((playerNode, playerNode.FogBounds, playerNode.FogBorderColor, Mathf.Max(pFog, 0.5f)));
            }
        }

        // Surrounding world stuff (Entities in container) — ONLY objects which are into the fog clouds!
        if (_entitiesContainer != null && GodotObject.IsInstanceValid(_entitiesContainer))
        {
            var children = _entitiesContainer.GetChildren();
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is not Node2D child || !child.IsInsideTree() || !child.Visible) continue;
                if (child == playerNode) continue; // Already processed
                if (child is ICombatant combatant && combatant.IsDead) continue;
                if (child is ISelectableTarget selectable && selectable.IsDead) continue;

                // Distance culling from player
                if (child.GlobalPosition.DistanceTo(playerPos) > MaxRenderDistance) continue;

                // User constraint: Must be INTO the fog clouds!
                float objFog = Werewolves.World.FogZone.GetFogFactorAt(child.GlobalPosition);
                bool inFogZone = objFog > 0.05f || Werewolves.World.FogZone.IsPositionInFog(child.GlobalPosition, 25f);
                if (!inFogZone) continue; // Exclude objects outside fog clouds!

                // Resolve bounds and color
                if (child is IFogBorderable fb)
                {
                    candidates.Add((child, fb.FogBounds, fb.FogBorderColor, Mathf.Max(objFog, 0.5f)));
                }
                else if (child is ISelectableTarget st)
                {
                    candidates.Add((child, st.TargetBounds, new Color(0.88f, 0.94f, 1.0f, 0.90f), Mathf.Max(objFog, 0.5f)));
                }
            }
        }

        // 2. Render dashed borders for candidates into the fog clouds
        for (int i = 0; i < candidates.Count; i++)
        {
            var (node, localBounds, baseColor, factor) = candidates[i];
            if (!GodotObject.IsInstanceValid(node)) continue;

            Vector2 entityLocalCenter = ToLocal(node.GlobalPosition);
            Rect2 frameRect = new Rect2(entityLocalCenter + localBounds.Position, localBounds.Size).Grow(BoundsPadding);

            float effectiveAlpha = _fadeAlpha * Mathf.Clamp(factor * 1.25f, 0.45f, 1.0f);
            DrawContinuousDashedBorder(frameRect, baseColor, effectiveAlpha, pulse, _dashOffset);
        }
    }

    /// <summary>
    /// Renders a continuous, seamless dashed border with dark drop shadow and corner accents.
    /// </summary>
    private void DrawContinuousDashedBorder(Rect2 rect, Color baseColor, float alpha, float pulse, float offset)
    {
        float w = rect.Size.X;
        float h = rect.Size.Y;
        if (w <= 2f || h <= 2f) return;

        float perimeter = 2.0f * (w + h);
        float cycle = DashLength + GapLength;

        Color shadowColor = new Color(0f, 0f, 0f, 0.65f * alpha);
        Color mainColor = new Color(
            baseColor.R,
            baseColor.G,
            baseColor.B,
            Mathf.Clamp(baseColor.A * (0.80f + 0.18f * pulse) * alpha, 0f, 1f)
        );

        float x0 = rect.Position.X;
        float y0 = rect.Position.Y;
        float x1 = x0 + w;
        float y1 = y0 + h;

        // Iterate dash intervals along perimeter [0, perimeter]
        float start = offset % cycle;
        if (start > 0f) start -= cycle;

        while (start < perimeter)
        {
            float dStart = Math.Max(0f, start);
            float dEnd = Math.Min(perimeter, start + DashLength);

            if (dEnd > dStart)
            {
                // Draw dash segment (handles wrapping around corners)
                DrawPerimeterSegments(dStart, dEnd, x0, y0, x1, y1, w, h, shadowColor, 3.2f);
                DrawPerimeterSegments(dStart, dEnd, x0, y0, x1, y1, w, h, mainColor, 1.8f);
            }

            start += cycle;
        }

        // Subtle corner L-brackets for crisp framing
        float cornerLen = Math.Min(6.0f, Math.Min(w, h) * 0.25f);
        DrawCornerBrackets(x0, y0, x1, y1, cornerLen, shadowColor, 3.2f);
        DrawCornerBrackets(x0, y0, x1, y1, cornerLen, mainColor, 1.8f);
    }

    private void DrawPerimeterSegments(
        float s1, float s2,
        float x0, float y0, float x1, float y1,
        float w, float h,
        Color color, float width)
    {
        // Corner perimeter coordinates:
        // C0 = 0 (top-left)
        // C1 = w (top-right)
        // C2 = w + h (bottom-right)
        // C3 = 2w + h (bottom-left)
        // C4 = 2w + 2h (top-left again)
        float c1 = w;
        float c2 = w + h;
        float c3 = 2.0f * w + h;

        // Split dash interval if it crosses any corner
        float cur = s1;
        while (cur < s2)
        {
            float nextCorner = (cur < c1) ? c1 : ((cur < c2) ? c2 : ((cur < c3) ? c3 : 2.0f * (w + h)));
            float segEnd = Math.Min(s2, nextCorner);

            Vector2 pA = GetPointOnPerimeter(cur, x0, y0, x1, y1, w, h);
            Vector2 pB = GetPointOnPerimeter(segEnd, x0, y0, x1, y1, w, h);

            if (pA.DistanceSquaredTo(pB) > 0.5f)
            {
                DrawLine(pA, pB, color, width, antialiased: true);
            }

            cur = segEnd;
        }
    }

    private static Vector2 GetPointOnPerimeter(float s, float x0, float y0, float x1, float y1, float w, float h)
    {
        if (s < w)
        {
            // Top edge (left to right)
            return new Vector2(x0 + s, y0);
        }
        if (s < w + h)
        {
            // Right edge (top to bottom)
            return new Vector2(x1, y0 + (s - w));
        }
        if (s < 2.0f * w + h)
        {
            // Bottom edge (right to left)
            return new Vector2(x1 - (s - (w + h)), y1);
        }
        // Left edge (bottom to top)
        return new Vector2(x0, y1 - (s - (2.0f * w + h)));
    }

    private void DrawCornerBrackets(float x0, float y0, float x1, float y1, float len, Color color, float width)
    {
        // Top-Left
        DrawLine(new Vector2(x0, y0), new Vector2(x0 + len, y0), color, width, antialiased: true);
        DrawLine(new Vector2(x0, y0), new Vector2(x0, y0 + len), color, width, antialiased: true);

        // Top-Right
        DrawLine(new Vector2(x1, y0), new Vector2(x1 - len, y0), color, width, antialiased: true);
        DrawLine(new Vector2(x1, y0), new Vector2(x1, y0 + len), color, width, antialiased: true);

        // Bottom-Right
        DrawLine(new Vector2(x1, y1), new Vector2(x1 - len, y1), color, width, antialiased: true);
        DrawLine(new Vector2(x1, y1), new Vector2(x1, y1 - len), color, width, antialiased: true);

        // Bottom-Left
        DrawLine(new Vector2(x0, y1), new Vector2(x0 + len, y1), color, width, antialiased: true);
        DrawLine(new Vector2(x0, y1), new Vector2(x0, y1 - len), color, width, antialiased: true);
    }
}
