using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

/// <summary>
/// Collectible blood spot pool spawned upon the death of living creatures.
/// Persists for 40 seconds before fading away. Requires an empty or non-full flask to collect.
/// </summary>
public partial class BloodSpot : Area2D, IFogBorderable
{
    public const float MaxLifetime = 40f;
    private const float FadeOutDuration = 5f;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private float _lifetime = 0f;
    private bool _isHovered = false;
    private bool _isCollected = false;

    // IFogBorderable implementation
    public Rect2 FogBounds => new Rect2(-30f, -15f, 60f, 30f);
    public Color FogBorderColor => new Color(0.85f, 0.12f, 0.15f, 0.95f); // Cavern crimson

    public static BloodSpot Instantiate(Vector2 position)
    {
        return new BloodSpot
        {
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        // 1. Sprite with random blood spot texture (1..3)
        _sprite = new Sprite2D { Name = "Sprite2D" };
        int variant = GD.RandRange(1, 3);
        var texture = GD.Load<Texture2D>($"res://assets/blood-spots/{variant}.png");
        _sprite.Texture = texture;
        _sprite.Scale = new Vector2(0.4f, 0.4f);
        _sprite.ZIndex = -2; // Lying flat on ground beneath entities
        AddChild(_sprite);

        // 2. Collision Shape for clicking/interaction
        _collision = new CollisionShape2D { Name = "CollisionShape2D" };
        _collision.Shape = new CircleShape2D { Radius = 30f };
        AddChild(_collision);

        // 3. Input & Hover hooks
        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        BuildBloodLabel();
    }

    private Button _bloodLabel = null!;

    private void BuildBloodLabel()
    {
        _bloodLabel = new Button
        {
            Name = "BloodLabel",
            Text = $"[ Blood Spot ({MaxLifetime:F0}s) ]",
            Position = new Vector2(-55f, -62f),
            CustomMinimumSize = new Vector2(110f, 22f),
            Size = new Vector2(110f, 22f),
            ZIndex = 50,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = "Click to collect blood with flask"
        };

        var styleNormal = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.05f, 0.05f, 0.92f),
            BorderColor = new Color(0.95f, 0.25f, 0.25f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        var styleHover = new StyleBoxFlat
        {
            BgColor = new Color(0.25f, 0.10f, 0.10f, 1.0f),
            BorderColor = Colors.White,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };

        _bloodLabel.AddThemeStyleboxOverride("normal", styleNormal);
        _bloodLabel.AddThemeStyleboxOverride("hover", styleHover);
        _bloodLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.45f));
        _bloodLabel.AddThemeColorOverride("font_hover_color", Colors.White);
        _bloodLabel.AddThemeFontSizeOverride("font_size", 11);

        _bloodLabel.Pressed += () =>
        {
            TryCollect();
            GetViewport().SetInputAsHandled();
        };
        _bloodLabel.MouseEntered += () => CursorManager.SetGrab();
        _bloodLabel.MouseExited += () => CursorManager.ResetNormal();

        _bloodLabel.Visible = GameState.Instance != null && GameState.Instance.IsLootLabelsVisible;
        if (GameState.Instance != null)
        {
            GameState.Instance.OnLootLabelsToggled += OnLootLabelsToggled;
        }

        AddChild(_bloodLabel);
    }

    private void OnLootLabelsToggled(bool visible)
    {
        if (GodotObject.IsInstanceValid(this) && _bloodLabel != null && GodotObject.IsInstanceValid(_bloodLabel))
        {
            _bloodLabel.Visible = visible;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _lifetime += dt;

        if (_bloodLabel != null && GodotObject.IsInstanceValid(_bloodLabel) && _bloodLabel.Visible)
        {
            float rem = Mathf.Max(0f, MaxLifetime - _lifetime);
            _bloodLabel.Text = $"[ Blood Spot ({rem:F0}s) ]";
        }

        // Smooth fade out during the final 5 seconds of the 40s lifetime
        if (_lifetime >= MaxLifetime - FadeOutDuration)
        {
            float remaining = Mathf.Max(0f, MaxLifetime - _lifetime);
            float alpha = remaining / FadeOutDuration;
            _sprite.Modulate = new Color(1f, 1f, 1f, alpha);
        }

        if (_lifetime >= MaxLifetime)
        {
            _isCollected = true;
            InputPickable = false;
            if (_isHovered)
            {
                CursorManager.ForceResetNormal();
                _isHovered = false;
            }
            QueueFree();
        }
    }

    private void OnMouseEntered()
    {
        if (_isCollected || !IsInsideTree()) return;
        _isHovered = true;
        CursorManager.SetGrab();
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        CursorManager.ResetNormal();
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (_isCollected) return;
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            TryCollect();
            GetViewport().SetInputAsHandled();
        }
    }

    public void TryCollect()
    {
        if (_isCollected) return;

        if (GameState.Instance.TryCollectBlood(out int filledPercent, out int currentTotal, out bool isNewFlask))
        {
            _isCollected = true;
            _isHovered = false;
            InputPickable = false;
            if (_collision != null)
            {
                _collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            }
            CollisionLayer = 0;
            CollisionMask = 0;

            string text = isNewFlask
                ? $"+Blood Flask ({currentTotal}%)"
                : $"+Blood (+{filledPercent}%) -> {currentTotal}%";

            GameState.Instance.TriggerDamageNumber(text, GlobalPosition + new Vector2(0f, -20f), new Color(0.95f, 0.22f, 0.22f));
            CursorManager.ForceResetNormal();
            QueueFree();
        }
        else
        {
            GameState.Instance.TriggerDamageNumber("Need an empty flask!", GlobalPosition + new Vector2(0f, -20f), new Color(0.85f, 0.85f, 0.85f));
        }
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnLootLabelsToggled -= OnLootLabelsToggled;
        }

        if (_isHovered || _isCollected)
        {
            CursorManager.ForceResetNormal();
            _isHovered = false;
        }
    }
}
