using System;
using Godot;
using Werewolves.Core;
using Werewolves.Effects;

namespace Werewolves.Entities;

public partial class Deer : CharacterBody2D, ICombatant, ISelectableTarget, IFogBorderable
{
    public string TargetName => "Deer";
    public float BaseDamage { get; set; } = 15f;
    public float Accuracy { get; set; } = 0.75f;
    public float BaseDefense { get; set; } = 5f;
    public float Evasion { get; set; } = 0.15f;
    public float Health { get; set; } = 80f;
    public float MaxHealth { get; set; } = 80f;
    public bool IsDead => Health <= 0f;
    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -75);

    private float _speed = 70f;
    private bool _isAggro = false;
    private float _attackCooldown = 0f;
    private float _attackCooldownTotal = 3.0f;
    private float _attackRange = 90f;

    private Vector2 _wanderVelocity = Vector2.Zero;
    private float _wanderTimer = 0f;
    private float _pauseTimer = 0f;
    public Rect2 TargetBounds => new Rect2(-51f, -99f, 102f, 96f);

    // IFogBorderable implementation
    public Rect2 FogBounds => TargetBounds;
    public Color FogBorderColor => new Color(0.55f, 0.95f, 0.65f, 0.95f); // Forest mint

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private float _animTimer = 0f;
    private int _animFrame = 0;
    private const int TotalAnimFrames = 8;
    private const float AnimSpeed = 0.12f;

    public Werewolf? TargetWerewolf { get; set; }
    [Export] public Vector2 HomePosition { get; set; } = Vector2.Zero;
    private const float MaxWanderDistance = 450f;

    public override void _Ready()
    {
        var dCfg = ConfigManager.Combat.Enemies.Deer;
        BaseDamage = dCfg.BaseDamage;
        Accuracy = dCfg.Accuracy;
        BaseDefense = dCfg.BaseDefense;
        Evasion = dCfg.Evasion;
        MaxHealth = dCfg.MaxHealth;
        Health = dCfg.MaxHealth;
        _speed = dCfg.Speed;
        _attackCooldownTotal = dCfg.AttackCooldown;
        _attackRange = dCfg.AttackRange;

        if (HomePosition == Vector2.Zero)
        {
            HomePosition = GlobalPosition;
        }

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _sprite.Texture = GD.Load<Texture2D>("res://assets/deer.png");
        _sprite.Hframes = 4;
        _sprite.Vframes = 4; // 150x150 cells in 600x600 sheet (4 cols, 4 rows)
        _sprite.Frame = 0;
        _sprite.Scale = new Vector2(0.75f, 0.75f);
        _sprite.Offset = new Vector2(0, -40);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var shape = new CircleShape2D { Radius = 27f };
            _collision.Shape = shape;
            _collision.Position = new Vector2(0, -7);
            AddChild(_collision);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        PickNewWanderDirection();
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

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (IsDead)
        {
            Velocity = Vector2.Zero;
            _sprite.Modulate = new Color(1, 1, 1, Mathf.MoveToward(_sprite.Modulate.A, 0.0f, dt * 1.5f));
            if (_sprite.Modulate.A <= 0.01f)
            {
                // Spawn meat loot
                var loot = DroppedLoot.Instantiate("Meat", GlobalPosition);
                GetParent()?.AddChild(loot);

                if (GameState.Instance.SelectedTarget == this)
                {
                    GameState.Instance.SelectedTarget = null;
                }
                QueueFree();
            }
            return;
        }

        if (_attackCooldown > 0f)
        {
            _attackCooldown -= dt;
        }

        if (_isAggro && TargetWerewolf != null && !TargetWerewolf.IsDead)
        {
            // Aggro behaviour
            Vector2 diff = TargetWerewolf.GlobalPosition - GlobalPosition;
            float dist = diff.Length();

            if (dist > _attackRange)
            {
                Velocity = diff.Normalized() * _speed * 1.4f;
                _sprite.FlipH = Velocity.X < 0;
            }
            else
            {
                Velocity = Vector2.Zero;
                if (_attackCooldown <= 0f)
                {
                    // Attack Werewolf
                    _attackCooldown = _attackCooldownTotal;
                    if (Formulas.IsHitSuccessful(this, TargetWerewolf))
                    {
                        float dmg = Formulas.CalculateDamage(this, TargetWerewolf);
                        TargetWerewolf.TakeDamage(dmg);
                        GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), TargetWerewolf.GlobalPosition + new Vector2(0, -85), new Color(1f, 0.3f, 0.3f));
                    }
                    else
                    {
                        GameState.Instance.TriggerDamageNumber("Miss", TargetWerewolf.GlobalPosition + new Vector2(0, -85), new Color(0.8f, 0.8f, 0.8f));
                    }
                }
            }
        }
        else
        {
            // Passive wandering
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= dt;
                Velocity = Vector2.Zero;
                if (_pauseTimer <= 0f)
                {
                    PickNewWanderDirection();
                }
            }
            else
            {
                _wanderTimer -= dt;
                Velocity = _wanderVelocity;
                _sprite.FlipH = Velocity.X < 0;

                // Tethered wandering relative to HomePosition
                if (GlobalPosition.DistanceTo(HomePosition) > MaxWanderDistance)
                {
                    Vector2 returnDir = (HomePosition - GlobalPosition).Normalized();
                    _wanderVelocity = returnDir * _speed;
                }

                if (_wanderTimer <= 0f)
                {
                    _pauseTimer = (float)GD.RandRange(1.5, 3.0);
                }
            }
        }

        MoveAndSlide();

        // Animation update
        if (IsDead)
        {
            _sprite.Frame = 11; // Dead frame (row 2, col 3)
        }
        else if (_attackCooldown > _attackCooldownTotal - 0.4f)
        {
            _sprite.Frame = 10; // Attack frame (row 2, col 2)
        }
        else if (Velocity.LengthSquared() > 10f)
        {
            _animTimer += dt;
            if (_animTimer >= AnimSpeed)
            {
                _animTimer = 0f;
                _animFrame = (_animFrame + 1) % TotalAnimFrames;
                _sprite.Frame = _animFrame;
            }
        }
        else
        {
            _sprite.Frame = 0;
        }
    }

    private void PickNewWanderDirection()
    {
        float angle = (float)GD.RandRange(0.0, Math.PI * 2.0);
        _wanderVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _speed;
        _wanderTimer = (float)GD.RandRange(2.0, 5.0);
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

    public void TakeDamage(float amount, bool isSkill = false)
    {
        if (IsDead) return;

        Vibrate(6f, 0.2f);
        Health -= amount;
        _isAggro = true;

        // Spawn scratch effect
        var scratch = ScratchEffect.Instantiate(GlobalPosition + new Vector2(0, -20), flip: GD.Randf() > 0.5f);
        GetParent()?.AddChild(scratch);

        if (Health <= 0f)
        {
            Health = 0f;
            _isAggro = false;
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

    public void Interact()
    {
        // When user clicks "Attack" on the target panel
        // Handled through Werewolf.TriggerAttackOnTarget
    }
}
