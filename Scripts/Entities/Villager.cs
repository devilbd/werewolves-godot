using System;
using Godot;
using Werewolves.Core;
using Werewolves.Effects;

namespace Werewolves.Entities;

public partial class Villager : CharacterBody2D, ICombatant, ISelectableTarget
{
    public string TargetName => "Villager";
    public float BaseDamage { get; set; } = 15f;
    public float Accuracy { get; set; } = 0.75f;
    public float BaseDefense { get; set; } = 5f;
    public float Evasion { get; set; } = 0.15f;
    public float Health { get; set; } = 80f;
    public float MaxHealth => 80f;
    public bool IsDead => Health <= 0f;
    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -100);
    public Rect2 TargetBounds => new Rect2(-42f, -102f, 84f, 102f);

    private float _speed = 65f;
    private bool _isAggro = false;
    private float _attackCooldown = 0f;
    private const float AttackCooldownTotal = 2.5f;
    private const float AttackRange = 95f;
    private const float AttackDuration = 0.45f;
    private float _attackAnimTimer = 0f;
    private bool _isAttacking = false;

    private Vector2 _wanderVelocity = Vector2.Zero;
    private float _wanderTimer = 0f;
    private float _pauseTimer = 0f;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Area2D? _clickArea;
    private Tween? _shakeTween;
    private bool _isSelected = false;
    private bool _isHovered = false;

    private float _animTimer = 0f;
    private int _animFrame = 0;
    private const int TotalRunFrames = 3;
    private const float AnimSpeed = 0.12f;

    public Werewolf? TargetWerewolf { get; set; }
    [Export] public Vector2 HomePosition { get; set; } = Vector2.Zero;
    private const float MaxWanderDistance = 350f;

    public override void _Ready()
    {
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

        if (_sprite.Texture == null)
        {
            _sprite.Texture = GD.Load<Texture2D>("res://assets/villager/villager.png");
        }
        _sprite.Hframes = 3;
        _sprite.Vframes = 3;
        _sprite.Frame = 0;
        _sprite.Scale = new Vector2(0.75f, 0.75f);
        _sprite.Offset = new Vector2(0, -45);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var shape = new CircleShape2D { Radius = 27f };
            _collision.Shape = shape;
            _collision.Position = new Vector2(0, -7);
            AddChild(_collision);
        }

        _clickArea = GetNodeOrNull<Area2D>("ClickArea");
        if (_clickArea == null)
        {
            _clickArea = new Area2D { Name = "ClickArea" };
            var clickShape = new CollisionShape2D
            {
                Shape = new CapsuleShape2D { Radius = 36f, Height = 105f },
                Position = new Vector2(0, -52)
            };
            _clickArea.AddChild(clickShape);
            AddChild(_clickArea);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        _clickArea.InputPickable = true;
        _clickArea.MouseEntered += OnMouseEntered;
        _clickArea.MouseExited += OnMouseExited;
        _clickArea.InputEvent += OnClickAreaInputEvent;

        PickNewWanderDirection();
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        if (!IsDead)
        {
            var cursor = GD.Load<Resource>("res://assets/cursors/interaction_o.png");
            if (cursor != null)
            {
                Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, Vector2.Zero);
            }
        }
    }

    private void OnMouseExited()
    {
        ResetCursor();
    }

    private void ResetCursor()
    {
        if (_isHovered)
        {
            _isHovered = false;
            var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
            if (cursor != null)
            {
                Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, Vector2.Zero);
            }
        }
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        HandleSelectionClick(@event);
    }

    private void OnClickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        HandleSelectionClick(@event);
    }

    private void HandleSelectionClick(InputEvent @event)
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
                ResetCursor();

                var lootResult = Formulas.RollVillagerLoot();
                if (lootResult.GoldCoins > 0 && lootResult.Meat > 0)
                {
                    var goldLoot = DroppedLoot.Instantiate("GoldCoins", GlobalPosition + new Vector2(-18f, 0f), lootResult.GoldCoins);
                    GetParent()?.AddChild(goldLoot);

                    var meatLoot = DroppedLoot.Instantiate("Meat", GlobalPosition + new Vector2(18f, 0f), lootResult.Meat);
                    GetParent()?.AddChild(meatLoot);
                }
                else if (lootResult.GoldCoins > 0)
                {
                    var goldLoot = DroppedLoot.Instantiate("GoldCoins", GlobalPosition, lootResult.GoldCoins);
                    GetParent()?.AddChild(goldLoot);
                }
                else if (lootResult.Meat > 0)
                {
                    var meatLoot = DroppedLoot.Instantiate("Meat", GlobalPosition, lootResult.Meat);
                    GetParent()?.AddChild(meatLoot);
                }

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
            Vector2 diff = TargetWerewolf.GlobalPosition - GlobalPosition;
            float dist = diff.Length();

            if (dist > AttackRange)
            {
                Velocity = diff.Normalized() * _speed * 1.35f;
                _sprite.FlipH = Velocity.X < 0;
            }
            else
            {
                Velocity = Vector2.Zero;
                _sprite.FlipH = diff.X < 0;

                if (_attackCooldown <= 0f && !_isAttacking)
                {
                    _attackCooldown = AttackCooldownTotal;
                    _isAttacking = true;
                    _attackAnimTimer = AttackDuration;

                    if (Formulas.IsHitSuccessful(this, TargetWerewolf))
                    {
                        float dmg = Formulas.CalculateDamage(this, TargetWerewolf);
                        TargetWerewolf.TakeDamage(dmg);
                        GameState.Instance.TriggerDamageNumber(
                            Mathf.FloorToInt(dmg).ToString(),
                            TargetWerewolf.GlobalPosition + new Vector2(0, -85),
                            new Color(1f, 0.3f, 0.3f)
                        );
                    }
                    else
                    {
                        GameState.Instance.TriggerDamageNumber(
                            "Miss",
                            TargetWerewolf.GlobalPosition + new Vector2(0, -85),
                            new Color(0.8f, 0.8f, 0.8f)
                        );
                    }
                }
            }
        }
        else
        {
            // Passive wandering around HomePosition in The Village
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

                // Tether to HomePosition
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

        // If bumped into obstacles during wandering, pick a new direction
        if (!_isAggro && GetSlideCollisionCount() > 0 && _pauseTimer <= 0f)
        {
            PickNewWanderDirection();
        }

        // Animation update
        UpdateAnimation(dt);
    }

    private void UpdateAnimation(float dt)
    {
        if (IsDead)
        {
            // 3rd row, 1st column: frame 6
            _sprite.Frame = 6;
            return;
        }

        if (_isAttacking)
        {
            _attackAnimTimer -= dt;
            if (_attackAnimTimer <= 0f)
            {
                _isAttacking = false;
            }
            else
            {
                // 2nd row: frames 3, 4, 5
                float progress = 1f - Mathf.Clamp(_attackAnimTimer / AttackDuration, 0f, 1f);
                int attackFrame = Mathf.Clamp((int)(progress * 3), 0, 2);
                _sprite.Frame = 3 + attackFrame;
                return;
            }
        }

        if (Velocity.LengthSquared() > 10f)
        {
            // 1st row: frames 0, 1, 2
            _animTimer += dt;
            if (_animTimer >= AnimSpeed)
            {
                _animTimer = 0f;
                _animFrame = (_animFrame + 1) % TotalRunFrames;
                _sprite.Frame = _animFrame;
            }
        }
        else
        {
            // Idle stance
            _sprite.Frame = 0;
        }
    }

    private void PickNewWanderDirection()
    {
        float angle = (float)GD.RandRange(0.0, Math.PI * 2.0);
        _wanderVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _speed;
        _wanderTimer = (float)GD.RandRange(2.0, 4.5);
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

        var scratch = ScratchEffect.Instantiate(GlobalPosition + new Vector2(0, -25), flip: GD.Randf() > 0.5f);
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
        // Target interaction routed through Werewolf auto-interact / melee attack
    }
}
