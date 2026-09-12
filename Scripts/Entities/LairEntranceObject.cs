using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

/// <summary>
/// Landmark for the Werewolf Lair entrance in the outside world.
/// Currently non-targetable; displays a blinking prompt above the cave when the Werewolf is near,
/// instructing the player to hit Enter to enter the cave.
/// </summary>
public partial class LairEntranceObject : StaticBody2D, IFogBorderable
{
    // IFogBorderable implementation
    public Rect2 FogBounds => new Rect2(-170f, -175f, 340f, 180f);
    public Color FogBorderColor => new Color(0.78f, 0.68f, 1.0f, 0.95f); // Mystic cavern purple

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Area2D _proximityArea = null!;
    private Label _promptLabel = null!;
    private Tween? _shakeTween;

    private bool _isPlayerNear = false;
    private bool _isPlayerInArea = false;
    private Werewolf? _nearbyPlayer = null;
    private bool _isTransitioning = false;
    private float _blinkTimer = 0f;

    public static LairEntranceObject Instantiate(Vector2 position)
    {
        return new LairEntranceObject
        {
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        // Entrance is deliberately not mouse-targetable for now
        InputPickable = false;

        // 1. Sprite
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            _sprite.Texture = GD.Load<Texture2D>("res://assets/liar/liar_entrance.png");
        }
        _sprite.Scale = new Vector2(0.75f, 0.75f);
        _sprite.Offset = new Vector2(0, -120);

        // 2. Physical Rock Collision
        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var rockShape = new RectangleShape2D { Size = new Vector2(380f, 90f) };
            _collision.Shape = rockShape;
            _collision.Position = new Vector2(0, -70);
            AddChild(_collision);
        }

        // 3. Proximity detection Area2D
        _proximityArea = GetNodeOrNull<Area2D>("TriggerArea");
        if (_proximityArea == null)
        {
            _proximityArea = new Area2D { Name = "TriggerArea" };
            var triggerShape = new CollisionShape2D
            {
                Shape = new CircleShape2D { Radius = 190f },
                Position = new Vector2(0, -20)
            };
            _proximityArea.AddChild(triggerShape);
            AddChild(_proximityArea);
        }
        else
        {
            // Expand existing trigger shape to a comfortable proximity radius
            var shapeNode = _proximityArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (shapeNode != null && shapeNode.Shape is CircleShape2D circle)
            {
                circle.Radius = 190f;
            }
        }
        _proximityArea.BodyEntered += OnProximityBodyEntered;
        _proximityArea.BodyExited += OnProximityBodyExited;

        // 4. Disable ClickArea from scene if present so mouse clicks are never captured
        var clickArea = GetNodeOrNull<Area2D>("ClickArea");
        if (clickArea != null)
        {
            clickArea.InputPickable = false;
            clickArea.Monitoring = false;
        }

        // 5. Blinking text prompt above the cave
        _promptLabel = GetNodeOrNull<Label>("PromptLabel");
        if (_promptLabel == null)
        {
            _promptLabel = new Label
            {
                Name = "PromptLabel",
                Text = "Hit Enter to enter the cave",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ZIndex = 25,
                Visible = false
            };

            _promptLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
            _promptLabel.OffsetLeft = -250f;
            _promptLabel.OffsetRight = 250f;
            _promptLabel.OffsetTop = -285f;
            _promptLabel.OffsetBottom = -245f;

            _promptLabel.AddThemeFontSizeOverride("font_size", 18);
            _promptLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.93f, 0.45f, 1f)); // Warm gold
            _promptLabel.AddThemeConstantOverride("outline_size", 4);
            _promptLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 1f));
            _promptLabel.AddThemeConstantOverride("shadow_offset_x", 2);
            _promptLabel.AddThemeConstantOverride("shadow_offset_y", 2);
            _promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));

            AddChild(_promptLabel);
        }
    }

    private void OnProximityBodyEntered(Node2D body)
    {
        if (body is Werewolf werewolf)
        {
            _isPlayerInArea = true;
            _nearbyPlayer = werewolf;
        }
    }

    private void OnProximityBodyExited(Node2D body)
    {
        if (body is Werewolf)
        {
            _isPlayerInArea = false;
            _nearbyPlayer = null;
        }
    }

    public override void _Process(double delta)
    {
        // Dual check: Area2D body monitoring + distance check against GameState.PlayerPosition
        float distSq = GlobalPosition.DistanceSquaredTo(GameState.Instance.PlayerPosition);
        bool nearByDistance = distSq <= (210f * 210f);
        _isPlayerNear = _isPlayerInArea || nearByDistance;

        if (_isPlayerNear)
        {
            _promptLabel.Visible = true;
            _blinkTimer += (float)delta * 5.0f;
            // Smooth sinusoidal blinking between 0.20 and 1.0
            float alpha = 0.20f + 0.80f * (0.5f + 0.5f * Mathf.Sin(_blinkTimer));
            _promptLabel.Modulate = new Color(1f, 1f, 1f, alpha);

            // Check if player presses Enter to enter the cave
            if (!_isTransitioning && (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.KpEnter) || Input.IsActionJustPressed("ui_accept")))
            {
                EnterLair(_nearbyPlayer);
            }
        }
        else
        {
            _promptLabel.Visible = false;
            _blinkTimer = 0f;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isPlayerNear && !_isTransitioning)
        {
            if (@event is InputEventKey key && key.Pressed && !key.Echo &&
                (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))
            {
                GetViewport().SetInputAsHandled();
                EnterLair(_nearbyPlayer);
            }
            else if (@event.IsActionPressed("ui_accept"))
            {
                GetViewport().SetInputAsHandled();
                EnterLair(_nearbyPlayer);
            }
        }
    }

    public void EnterLair(Werewolf? werewolf)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        if (GameState.Instance.SelectedTarget == this)
        {
            GameState.Instance.SelectedTarget = null;
        }

        // Save outside return position right outside the cave entrance threshold
        Vector2 returnPos = GlobalPosition + new Vector2(0f, 90f);
        SaveManager.LoadedPlayerPosition = returnPos;
        GameState.Instance.PlayerPosition = returnPos;
        SaveManager.SaveGame();

        // Switch to the Lair scene
        GetTree().ChangeSceneToFile("res://scenes/Lair.tscn");
    }

    public void Vibrate(float intensity = 4f, float duration = 0.15f)
    {
        if (_sprite == null) return;
        _shakeTween?.Kill();
        _sprite.Position = Vector2.Zero;
        _shakeTween = CreateTween();

        float stepTime = duration / 5f;
        _shakeTween.TweenProperty(_sprite, "position", new Vector2(-intensity, 0), stepTime);
        _shakeTween.TweenProperty(_sprite, "position", new Vector2(intensity * 0.8f, 0), stepTime);
        _shakeTween.TweenProperty(_sprite, "position", new Vector2(-intensity * 0.5f, 0), stepTime);
        _shakeTween.TweenProperty(_sprite, "position", new Vector2(intensity * 0.25f, 0), stepTime);
        _shakeTween.TweenProperty(_sprite, "position", Vector2.Zero, stepTime);
    }
}
