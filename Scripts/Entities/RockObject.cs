using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class RockObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    public string TargetName => "Rock";
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public bool IsDead => Health <= 0;
    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -70);
    public Rect2 TargetBounds => new Rect2(-36f, -38f, 72f, 40f);

    // IFogBorderable implementation
    public Rect2 FogBounds => new Rect2(-40f, -42f, 80f, 46f);
    public Color FogBorderColor => new Color(0.88f, 0.90f, 0.94f, 0.90f); // Granite silver

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private bool _isBlinking = false;
    private float _blinkTimer = 0f;
    private float _blinkDuration = 5f;

    public override void _Ready()
    {
        MaxHealth = ConfigManager.Combat.Harvestables.Rock.MaxHealth;
        Health = MaxHealth;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            int variant = GD.RandRange(0, 2);
            string path = variant switch
            {
                1 => "res://assets/rocks/optimized/rock_stone_2_o.png",
                2 => "res://assets/rocks/optimized/rock_stone_3_o.png",
                _ => "res://assets/rocks/optimized/rock_stone_o.png"
            };
            _sprite.Texture = GD.Load<Texture2D>(path);
        }

        _sprite.Scale = new Vector2(0.5f, 0.5f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.3f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var circle = new CircleShape2D { Radius = 15f };
            _collision.Shape = circle;
            _collision.Position = new Vector2(0, -5);
            AddChild(_collision);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        if (!IsDead)
        {
            var cursor = GD.Load<Resource>("res://assets/cursors/interaction_o.png");
            if (cursor != null)
            {
                Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
            }
        }
    }

    private void OnMouseExited()
    {
        var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
        }
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (!IsDead && @event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            GameState.Instance.SelectedTarget = this;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_isBlinking)
        {
            _blinkTimer -= (float)delta;
            float pulse = (Mathf.Sin(Time.GetTicksMsec() / 200.0f) + 1.0f) / 2.0f;
            _sprite.Modulate = new Color(1, 1, 1, 0.7f + pulse * 0.3f);

            if (_blinkTimer <= 0f)
            {
                _isBlinking = false;
                _sprite.Modulate = new Color(1, 1, 1, 1);
            }
        }
    }

    public override void _Draw()
    {
        // Selection reticle with animated shiny rounded corners is rendered by TargetReticle
    }

    public void OnSelected()
    {
        _isSelected = true;
        QueueRedraw();
    }

    public void OnDeselected()
    {
        _isSelected = false;
        QueueRedraw();
    }

    public void Vibrate(float intensity = 5f, float duration = 0.18f)
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

    public void Interact()
    {
        if (IsDead) return;

        Vibrate(5f, 0.18f);
        _isBlinking = true;
        _blinkTimer = _blinkDuration;
        Health -= ConfigManager.Combat.Harvestables.Rock.DamagePerHit;

        GameState.Instance.TriggerDamageNumber("Quarry!", FloatingTextPosition, new Color(0.9f, 0.9f, 0.9f));

        if (Health <= 0f)
        {
            Health = 0f;
            var loot = DroppedLoot.Instantiate("Stones", GlobalPosition);
            GetParent()?.AddChild(loot);

            if (GameState.Instance.SelectedTarget == this)
            {
                GameState.Instance.SelectedTarget = null;
            }

            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 0.5f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
