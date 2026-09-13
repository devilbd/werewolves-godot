using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class QuartzObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    [Export] public int Variant { get; set; } = 0; // 1: small, 2: medium, 3: large (0 = random)
    public string TargetName => "Quartz";
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public bool IsDead => Health <= 0;

    public Vector2 FloatingTextPosition => Variant switch
    {
        1 => GlobalPosition + new Vector2(0, -65f),
        2 => GlobalPosition + new Vector2(0, -80f),
        _ => GlobalPosition + new Vector2(0, -105f)
    };

    public Rect2 TargetBounds => Variant switch
    {
        1 => new Rect2(-20f, -55f, 40f, 58f),
        2 => new Rect2(-30f, -70f, 60f, 74f),
        _ => new Rect2(-36f, -90f, 72f, 95f)
    };

    // IFogBorderable implementation (crystalline amethyst glow)
    public Rect2 FogBounds => Variant switch
    {
        1 => new Rect2(-24f, -60f, 48f, 65f),
        2 => new Rect2(-34f, -75f, 68f, 80f),
        _ => new Rect2(-40f, -95f, 80f, 102f)
    };

    public Color FogBorderColor => new Color(0.85f, 0.70f, 1.0f, 0.90f);

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private bool _isBlinking = false;
    private float _blinkTimer = 0f;
    private float _blinkDuration = 5f;

    public override void _Ready()
    {
        MaxHealth = ConfigManager.Combat.Harvestables.Quartz.MaxHealth;
        Health = MaxHealth;

        if (Variant < 1 || Variant > 3)
        {
            var qCfg = ConfigManager.Resources.Quartz;
            float roll = GD.Randf();
            float wSmall = qCfg.VariantWeightSmall;
            float wMed = wSmall + qCfg.VariantWeightMedium;
            Variant = roll < wSmall ? 1 : (roll < wMed ? 2 : 3);
        }

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            string path = Variant switch
            {
                1 => "res://assets/resources/quartz/quartz_1.png",
                2 => "res://assets/resources/quartz/quartz_2.png",
                _ => "res://assets/resources/quartz/quartz_3.png"
            };
            _sprite.Texture = GD.Load<Texture2D>(path);
        }

        _sprite.Scale = new Vector2(0.65f, 0.65f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.45f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        float colRadius = Variant switch
        {
            1 => 14f,
            2 => 18f,
            _ => 22f
        };
        Vector2 colPos = Variant switch
        {
            1 => new Vector2(0, -6),
            2 => new Vector2(0, -8),
            _ => new Vector2(0, -10)
        };

        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            _collision.Shape = new CircleShape2D { Radius = colRadius };
            _collision.Position = colPos;
            AddChild(_collision);
        }
        else
        {
            _collision.Shape = new CircleShape2D { Radius = colRadius };
            _collision.Position = colPos;
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
        Health -= ConfigManager.Combat.Harvestables.Quartz.DamagePerHit;

        GameState.Instance.TriggerDamageNumber("Chop!", FloatingTextPosition, new Color(0.85f, 0.70f, 1.0f));

        if (Health <= 0f)
        {
            Health = 0f;

            var qCfg = ConfigManager.Resources.Quartz;
            int dropCount = Variant switch
            {
                1 => GD.RandRange(qCfg.DropVariant1SmallMin, qCfg.DropVariant1SmallMax),
                2 => GD.RandRange(qCfg.DropVariant2MediumMin, qCfg.DropVariant2MediumMax),
                _ => GD.RandRange(qCfg.DropVariant3LargeMin, qCfg.DropVariant3LargeMax)
            };

            var loot = DroppedLoot.Instantiate("Quartz", GlobalPosition, dropCount);
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

            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 0.5f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
