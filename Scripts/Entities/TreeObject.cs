using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class TreeObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    [Export] public bool IsSelectable { get; set; } = true;
    [Export] public float TreeScale { get; set; } = 0f;
    public string TargetName => "Tree";
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public bool IsDead => Health <= 0;
    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -440f * _currentScale);
    public Rect2 TargetBounds => new Rect2(-64f * _currentScale, -100f * _currentScale, 128f * _currentScale, 108f * _currentScale);

    // IFogBorderable implementation (ethereal mist border scaled with tree canopy)
    public Rect2 FogBounds => new Rect2(-80f * _currentScale, -300f * _currentScale, 160f * _currentScale, 310f * _currentScale);
    public Color FogBorderColor => new Color(0.85f, 0.92f, 0.98f, 0.90f); // Ethereal silver mist

    private float _currentScale = 0.5f;
    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private bool _isBlinking = false;
    private float _blinkTimer = 0f;
    private float _blinkDuration = 5f;

    public override void _Ready()
    {
        MaxHealth = ConfigManager.Combat.Harvestables.Tree.MaxHealth;
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
                1 => "res://assets/trees/pine_tree_1.png",
                2 => "res://assets/trees/pine_tree_2.png",
                _ => "res://assets/trees/pine_tree.png"
            };
            _sprite.Texture = GD.Load<Texture2D>(path);
        }

        if (TreeScale > 0.01f)
        {
            _currentScale = TreeScale;
        }
        else
        {
            // Randomized tree sizing principle:
            // 25% Small Pines (0.45 - 0.60)
            // 50% Medium / Standard Pines (0.65 - 0.85)
            // 25% Large / Ancient Pines (0.90 - 1.15)
            float roll = GD.Randf();
            if (roll < 0.25f)
            {
                _currentScale = (float)GD.RandRange(0.45, 0.60);
            }
            else if (roll < 0.75f)
            {
                _currentScale = (float)GD.RandRange(0.65, 0.85);
            }
            else
            {
                _currentScale = (float)GD.RandRange(0.90, 1.15);
            }
        }

        _sprite.Scale = new Vector2(_currentScale, _currentScale);
        _sprite.FlipH = GD.Randf() < 0.5f;
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.4f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var circle = new CircleShape2D { Radius = 30f * _currentScale };
            _collision.Shape = circle;
            _collision.Position = new Vector2(0, -10f * _currentScale);
            AddChild(_collision);
        }
        else
        {
            if (_collision.Shape is CircleShape2D circle)
            {
                var newCircle = (CircleShape2D)circle.Duplicate();
                newCircle.Radius = 30f * _currentScale;
                _collision.Shape = newCircle;
            }
            else
            {
                _collision.Shape = new CircleShape2D { Radius = 30f * _currentScale };
            }
            _collision.Position = new Vector2(0, -10f * _currentScale);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        if (IsSelectable && !IsDead)
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
        if (IsSelectable && !IsDead && @event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
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
        if (!IsSelectable || IsDead) return;

        Vibrate(6f, 0.2f);
        _isBlinking = true;
        _blinkTimer = _blinkDuration;
        Health -= ConfigManager.Combat.Harvestables.Tree.DamagePerHit;

        GameState.Instance.TriggerDamageNumber("Chop!", FloatingTextPosition, new Color(1f, 0.85f, 0.25f));

        if (Health <= 0f)
        {
            Health = 0f;
            // Spawn dropped log scaled with tree size tier
            int logCount = _currentScale < 0.65f ? 1 : (_currentScale < 0.90f ? GD.RandRange(1, 2) : GD.RandRange(2, 3));
            var loot = DroppedLoot.Instantiate("Logs", GlobalPosition, logCount);
            GetParent()?.AddChild(loot);

            if (GameState.Instance.SelectedTarget == this)
            {
                GameState.Instance.SelectedTarget = null;
            }

            var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
            if (cursor != null)
            {
                Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
            }

            // Quick fade and remove
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 0.5f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
