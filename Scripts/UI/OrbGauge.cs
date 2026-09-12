using System;
using System.Collections.Generic;
using Godot;

namespace Werewolves.UI;

[Tool]
public partial class OrbGauge : Control
{
    [Export] public string LabelText { get; set; } = "Health";
    [Export] public Color PrimaryColor { get; set; } = new Color(0.55f, 0f, 0f, 1f);
    [Export] public Color BackgroundColor { get; set; } = new Color(0.2f, 0f, 0f, 1f);
    [Export] public Texture2D? RingTexture { get; set; }

    public float CurrentValue { get; set; } = 100f;
    public float MaxValue { get; set; } = 100f;

    private struct Bubble
    {
        public Vector2 Pos;
        public float Radius;
        public float Speed;
        public float Alpha;
    }

    [Export] public float OrbRadius { get; set; } = 82f;

    private readonly List<Bubble> _bubbles = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(210, 210);
        for (int i = 0; i < 20; i++)
        {
            _bubbles.Add(CreateRandomBubble());
        }
    }

    private Bubble CreateRandomBubble()
    {
        return new Bubble
        {
            Pos = new Vector2((float)GD.RandRange(-OrbRadius * 0.7f, OrbRadius * 0.7f), (float)GD.RandRange(-OrbRadius * 0.7f, OrbRadius * 0.7f)),
            Radius = (float)GD.RandRange(1.5, 3.5),
            Speed = (float)GD.RandRange(20.0, 45.0),
            Alpha = (float)GD.RandRange(0.3, 0.8)
        };
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;

        float dt = (float)delta;
        for (int i = 0; i < _bubbles.Count; i++)
        {
            var b = _bubbles[i];
            b.Pos.Y -= b.Speed * dt;
            if (b.Pos.Y < -OrbRadius * 0.7f)
            {
                b.Pos.Y = OrbRadius * 0.7f;
                b.Pos.X = (float)GD.RandRange(-OrbRadius * 0.6f, OrbRadius * 0.6f);
            }
            _bubbles[i] = b;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size / 2.0f;

        // Draw dark background circle
        DrawCircle(center, OrbRadius, BackgroundColor);

        // Draw liquid fill based on percentage
        float pct = Mathf.Clamp(CurrentValue / (MaxValue > 0 ? MaxValue : 100f), 0f, 1f);
        float fillHeight = OrbRadius * 2f * pct;
        float topY = center.Y + OrbRadius - fillHeight;

        if (pct >= 0.999f)
        {
            DrawCircle(center, OrbRadius, PrimaryColor);
        }
        else if (pct > 0.005f)
        {
            float dy = topY - center.Y;
            float halfW = Mathf.Sqrt(Mathf.Max(0f, OrbRadius * OrbRadius - dy * dy));

            float a1 = Mathf.Atan2(dy, halfW);
            float a2 = Mathf.Atan2(dy, -halfW);
            if (a2 < a1) a2 += Mathf.Tau;

            int segments = 24;
            var points = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = a1 + (a2 - a1) * ((float)i / (segments - 1));
                points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * OrbRadius;
            }

            var indices = Geometry2D.TriangulatePolygon(points);
            if (indices != null && indices.Length > 0)
            {
                DrawPolygon(points, new Color[] { PrimaryColor });
            }
        }

        // Draw bubbles inside liquid
        if (pct > 0.05f)
        {
            foreach (var b in _bubbles)
            {
                Vector2 bPos = center + b.Pos;
                if (bPos.Y >= topY && bPos.DistanceTo(center) <= OrbRadius - 4f)
                {
                    DrawCircle(bPos, b.Radius, new Color(1, 1, 1, b.Alpha));
                }
            }
        }

        // Draw ornamental ring frame texture
        if (RingTexture != null)
        {
            Rect2 ringRect = new Rect2(center - new Vector2(OrbRadius + 22, OrbRadius + 22), new Vector2((OrbRadius + 22) * 2, (OrbRadius + 22) * 2));
            DrawTextureRect(RingTexture, ringRect, false);
        }
        else
        {
            DrawArc(center, OrbRadius + 2, 0, Mathf.Pi * 2, 32, new Color(0.7f, 0.6f, 0.2f), 4f);
        }

        // Text overlay: Label and Value
        var font = ThemeDB.FallbackFont;
        int fontSize = 16;
        string valStr = $"{Mathf.FloorToInt(CurrentValue)} / {Mathf.FloorToInt(MaxValue)}";

        Vector2 labelSize = font.GetStringSize(LabelText, HorizontalAlignment.Center, -1, fontSize);
        Vector2 valSize = font.GetStringSize(valStr, HorizontalAlignment.Center, -1, fontSize);

        DrawString(font, center + new Vector2(-labelSize.X / 2, -6), LabelText, HorizontalAlignment.Center, -1, fontSize, Colors.White);
        DrawString(font, center + new Vector2(-valSize.X / 2, 18), valStr, HorizontalAlignment.Center, -1, fontSize, new Color(0.9f, 0.9f, 0.9f));
    }
}
