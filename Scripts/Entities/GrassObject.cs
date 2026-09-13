using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class GrassObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    [Export] public int Variant { get; set; } = 0; // 1 to 4 (0 = random)
    public string TargetName => "Grass";
    public float Health { get; set; } = 50f;
    public float MaxHealth { get; set; } = 50f;
    public bool IsDead => Health <= 0;

    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -45f);

    public Rect2 TargetBounds => new Rect2(-45f, -50f, 90f, 55f);

    // IFogBorderable implementation (verdant green glow)
    public Rect2 FogBounds => new Rect2(-48f, -52f, 96f, 58f);
    public Color FogBorderColor => new Color(0.40f, 0.85f, 0.35f, 0.90f);

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private bool _isBlinking = false;
    private float _blinkTimer = 0f;
    private float _blinkDuration = 4f;

    public static GrassObject Instantiate(int variant, Vector2 position)
    {
        return new GrassObject
        {
            Variant = variant,
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        MaxHealth = ConfigManager.Combat.Harvestables.Grass.MaxHealth;
        Health = MaxHealth;

        if (Variant < 1 || Variant > 4)
        {
            Variant = GD.RandRange(1, 4);
        }

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            string path = $"res://assets/grass/{Variant}.png";
            _sprite.Texture = GD.Load<Texture2D>(path);
        }

        _sprite.Scale = new Vector2(0.42f, 0.42f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.40f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            _collision.Shape = new CircleShape2D { Radius = 16f };
            _collision.Position = new Vector2(0, -8f);
            AddChild(_collision);
        }
        else
        {
            _collision.Shape = new CircleShape2D { Radius = 16f };
            _collision.Position = new Vector2(0, -8f);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _ExitTree()
    {
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
    }

    private void OnMouseEntered()
    {
        if (!IsDead && IsInsideTree())
        {
            CursorManager.SetInteraction();
        }
    }

    private void OnMouseExited()
    {
        CursorManager.ResetNormal();
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
        // TargetReticle handles target brackets and animation
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

    public void Interact()
    {
        if (IsDead) return;

        Vibrate(4f, 0.15f);
        _isBlinking = true;
        _blinkTimer = _blinkDuration;
        Health -= ConfigManager.Combat.Harvestables.Grass.DamagePerHit;

        GameState.Instance.TriggerDamageNumber("Chop!", FloatingTextPosition, new Color(0.45f, 0.90f, 0.35f));

        if (Health <= 0f)
        {
            Health = 0f;

            var gCfg = ConfigManager.Resources.Grass;
            int dropCount = GD.RandRange(gCfg.DropMin, gCfg.DropMax);

            var loot = DroppedLoot.Instantiate("Grass", GlobalPosition, dropCount);
            GetParent()?.AddChild(loot);

            InputPickable = false;

            if (GameState.Instance.SelectedTarget == this)
            {
                GameState.Instance.SelectedTarget = null;
            }

            CursorManager.ForceResetNormal();

            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 0.35f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
