using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class TreeObject : StaticBody2D, ISelectableTarget
{
    [Export] public bool IsSelectable { get; set; } = false;
    public string TargetName => "Tree";
    public float Health { get; set; } = 100f;
    public float MaxHealth => 100f;
    public bool IsDead => Health <= 0;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private bool _isSelected = false;
    private bool _isBlinking = false;
    private float _blinkTimer = 0f;
    private float _blinkDuration = 5f;

    public override void _Ready()
    {
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

        _sprite.Scale = new Vector2(0.5f, 0.5f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.4f);

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
        if (_isSelected && !IsDead)
        {
            // Draw yellow corner markers around the tree base
            float s = 30f;
            float len = 10f;
            Color yellow = new Color(1f, 1f, 0.4f, 0.9f);
            float width = 3f;

            // Top-left
            DrawLine(new Vector2(-s, -s), new Vector2(-s + len, -s), yellow, width);
            DrawLine(new Vector2(-s, -s), new Vector2(-s, -s + len), yellow, width);
            // Top-right
            DrawLine(new Vector2(s, -s), new Vector2(s - len, -s), yellow, width);
            DrawLine(new Vector2(s, -s), new Vector2(s, -s + len), yellow, width);
            // Bottom-left
            DrawLine(new Vector2(-s, s), new Vector2(-s + len, s), yellow, width);
            DrawLine(new Vector2(-s, s), new Vector2(-s, s - len), yellow, width);
            // Bottom-right
            DrawLine(new Vector2(s, s), new Vector2(s - len, s), yellow, width);
            DrawLine(new Vector2(s, s), new Vector2(s, s - len), yellow, width);
        }
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

    public void Interact()
    {
        if (!IsSelectable || IsDead) return;

        _isBlinking = true;
        _blinkTimer = _blinkDuration;
        Health -= 25f; // Each chop deals 25 damage

        GameState.Instance.TriggerDamageNumber("Chop!", GlobalPosition + new Vector2(0, -50), new Color(1f, 0.8f, 0.2f));

        if (Health <= 0f)
        {
            Health = 0f;
            // Spawn dropped log
            var loot = DroppedLoot.Instantiate("Logs", GlobalPosition);
            GetParent()?.AddChild(loot);

            if (GameState.Instance.SelectedTarget == this)
            {
                GameState.Instance.SelectedTarget = null;
            }

            // Quick fade and remove
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 0.5f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
