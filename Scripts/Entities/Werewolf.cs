using System;
using Godot;
using Werewolves.Core;
using Werewolves.Effects;

namespace Werewolves.Entities;

public partial class Werewolf : CharacterBody2D, ICombatant
{
    public float BaseDamage => GameState.Instance.BaseDamage;
    public float Accuracy => GameState.Instance.Accuracy;
    public float BaseDefense => GameState.Instance.BaseDefense;
    public float Evasion => GameState.Instance.Evasion;
    public float Health
    {
        get => GameState.Instance.PlayerHealth;
        set => GameState.Instance.ModifyHealth(value - GameState.Instance.PlayerHealth);
    }
    public float MaxHealth => GameState.Instance.PlayerMaxHealth;
    public bool IsDead => Health <= 0;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private BuffAura? _activeBuffAura = null;

    // Sprite textures
    private Texture2D _texMoving = null!;
    private Texture2D _texIdle = null!;
    private Texture2D _texAttacks = null!;
    private Texture2D _texMagicAttacks = null!;

    // Animation states
    private bool _isAttacking = false;
    private int _attackAnimType = 0; // 0: Scratch, 1: Charge, 2: Bite, 3: Howl
    private int _attackFrame = 0;
    private float _attackTimer = 0f;
    private const float AttackFrameDuration = 0.08f;

    private float _walkAnimTimer = 0f;
    private int _walkFrame = 0;
    private const float WalkFrameDuration = 0.12f;

    private float _idleAnimTimer = 0f;
    private int _idleFrame = 0;
    private int _idleRow = 0;
    private float _idleRowTimer = 4.0f;
    private const float IdleFrameDuration = 0.25f;

    // Charge skill state
    private bool _isCharging = false;
    private Vector2 _chargeTargetPos = Vector2.Zero;
    private float _chargeSpeed = 750f;

    // Auto-attack timer (1 second interval)
    private float _autoAttackTimer = 0f;
    private const float AutoAttackInterval = 1.0f;
    private const float MeleeRange = 110f;

    private Camera2D? _camera;

    public bool IsAutoInteracting { get; private set; } = false;
    public event Action<bool>? OnAutoInteractToggled;

    public override void _Ready()
    {
        _texMoving = GD.Load<Texture2D>("res://assets/werewolf/optimized/w_moving.png");
        _texIdle = GD.Load<Texture2D>("res://assets/werewolf/optimized/w_idle_states.png");
        _texAttacks = GD.Load<Texture2D>("res://assets/werewolf/optimized/w_attacks.png");
        _texMagicAttacks = GD.Load<Texture2D>("res://assets/werewolf/optimized/w_magic_attacks.png");

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _sprite.Texture = _texIdle;
        _sprite.Hframes = 3;
        _sprite.Vframes = 3;
        _sprite.Frame = 0;
        _sprite.Scale = new Vector2(0.5f, 0.5f);
        _sprite.Offset = new Vector2(0, -50);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var shape = new CircleShape2D { Radius = 18f };
            _collision.Shape = shape;
            _collision.Position = new Vector2(0, -5);
            AddChild(_collision);
        }

        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera == null)
        {
            _camera = new Camera2D
            {
                Name = "Camera2D",
                PositionSmoothingEnabled = true,
                PositionSmoothingSpeed = 5.0f,
                LimitLeft = -5000,
                LimitTop = -5000,
                LimitRight = 5000,
                LimitBottom = 5000
            };
            AddChild(_camera);
        }

        GlobalPosition = SaveManager.LoadedPlayerPosition;
        GameState.Instance.PlayerPosition = GlobalPosition;

        GameState.Instance.OnTargetChanged += (target) =>
        {
            if (target == null)
            {
                SetAutoInteract(false);
            }
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Process active buff visual aura
        if (GameState.Instance.IsBuffed && _activeBuffAura == null)
        {
            _activeBuffAura = new BuffAura();
            AddChild(_activeBuffAura);
        }
        else if (!GameState.Instance.IsBuffed && _activeBuffAura != null)
        {
            _activeBuffAura.QueueFree();
            _activeBuffAura = null;
        }

        // Handle charging skill
        if (_isCharging)
        {
            Vector2 dir = (_chargeTargetPos - GlobalPosition);
            if (dir.Length() > 20f)
            {
                Velocity = dir.Normalized() * _chargeSpeed;
                MoveAndSlide();
            }
            else
            {
                _isCharging = false;
            }
        }
        else if (!_isAttacking)
        {
            // Standard Movement
            Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            bool isSprinting = Input.IsActionPressed("sprint");
            float currentSpeed = GameState.Instance.Speed * (isSprinting ? 1.6f : 1.0f);

            if (inputDir != Vector2.Zero)
            {
                Velocity = inputDir * currentSpeed;
                _sprite.FlipH = inputDir.X < 0;
            }
            else
            {
                Velocity = Vector2.Zero;
            }

            MoveAndSlide();
        }

        SaveManager.LoadedPlayerPosition = GlobalPosition;
        GameState.Instance.PlayerPosition = GlobalPosition;

        // Handle auto-attack in warmode
        ProcessAutoAttack(dt);

        // Handle skill inputs
        HandleSkillInputs();

        // Update animation
        UpdateAnimation(dt);
    }

    private void ProcessAutoAttack(float dt)
    {
        if (!IsAutoInteracting)
        {
            _autoAttackTimer = 0f;
            return;
        }

        var target = GameState.Instance.SelectedTarget;
        if (target == null || (target is ISelectableTarget selectable && (selectable.IsDead || selectable.Health <= 0)))
        {
            SetAutoInteract(false);
            return;
        }

        float dist = GlobalPosition.DistanceTo(target.GlobalPosition);

        if (target is Deer deer)
        {
            if (dist <= MeleeRange && !_isAttacking)
            {
                _autoAttackTimer += dt;
                if (_autoAttackTimer >= AutoAttackInterval)
                {
                    _autoAttackTimer = 0f;
                    ExecuteMeleeHit(deer);
                    if (deer.Health <= 0f || deer.IsDead)
                    {
                        SetAutoInteract(false);
                    }
                }
            }
        }
        else if (target is TreeObject tree)
        {
            if (dist <= MeleeRange * 1.5f && !_isAttacking)
            {
                _autoAttackTimer += dt;
                if (_autoAttackTimer >= AutoAttackInterval)
                {
                    _autoAttackTimer = 0f;
                    TriggerAttackAnimation(0);
                    tree.Interact();
                    if (tree.Health <= 0f || tree.IsDead)
                    {
                        SetAutoInteract(false);
                    }
                }
            }
        }
        else if (target is RockObject rock)
        {
            if (dist <= MeleeRange * 1.5f && !_isAttacking)
            {
                _autoAttackTimer += dt;
                if (_autoAttackTimer >= AutoAttackInterval)
                {
                    _autoAttackTimer = 0f;
                    TriggerAttackAnimation(0);
                    rock.Interact();
                    if (rock.Health <= 0f || rock.IsDead)
                    {
                        SetAutoInteract(false);
                    }
                }
            }
        }
    }

    private void ExecuteMeleeHit(Deer deer)
    {
        TriggerAttackAnimation(0);

        if (Formulas.IsHitSuccessful(this, deer))
        {
            float dmg = Formulas.CalculateDamage(this, deer);
            deer.TakeDamage(dmg);
            GameState.Instance.ModifyPower(Formulas.CalculatePowerGain());
            GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), deer.GlobalPosition + new Vector2(0, -50), new Color(1f, 1f, 0.4f));
        }
        else
        {
            GameState.Instance.TriggerDamageNumber("Miss", deer.GlobalPosition + new Vector2(0, -50), new Color(0.8f, 0.8f, 0.8f));
        }
    }

    private void HandleSkillInputs()
    {
        if (Input.IsActionJustPressed("skill_1")) UseSkill(0);
        else if (Input.IsActionJustPressed("skill_2")) UseSkill(1);
        else if (Input.IsActionJustPressed("skill_3")) UseSkill(2);
        else if (Input.IsActionJustPressed("skill_4")) UseSkill(3);
    }

    public void UseSkill(int skillIndex)
    {
        if (_isAttacking || GameState.Instance.IsSkillOnCooldown(skillIndex)) return;

        switch (skillIndex)
        {
            case 0: // Scratch Hit
                if (GameState.Instance.PlayerPower >= 15f)
                {
                    GameState.Instance.ModifyPower(-15f);
                    GameState.Instance.StartSkillCooldown(0);
                    TriggerAttackAnimation(0);

                    if (GameState.Instance.SelectedTarget is Deer target && !target.IsDead && GlobalPosition.DistanceTo(target.GlobalPosition) <= MeleeRange * 1.3f)
                    {
                        if (Formulas.IsHitSuccessful(this, target))
                        {
                            float dmg = Formulas.CalculateScratchHitDamage(this, target);
                            target.TakeDamage(dmg, isSkill: true);
                            GameState.Instance.ModifyPower(Formulas.CalculatePowerGain());
                            GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), target.GlobalPosition + new Vector2(0, -50), new Color(1f, 0.2f, 0.2f));
                        }
                    }
                }
                break;

            case 1: // Charge Attack
                if (GameState.Instance.PlayerPower >= 25f && GameState.Instance.SelectedTarget is Deer chargeTarget && !chargeTarget.IsDead)
                {
                    GameState.Instance.ModifyPower(-25f);
                    GameState.Instance.StartSkillCooldown(1);
                    _isCharging = true;
                    _chargeTargetPos = chargeTarget.GlobalPosition;
                    TriggerAttackAnimation(1);

                    // Execute strike on impact
                    GetTree().CreateTimer(0.2).Timeout += () =>
                    {
                        if (!chargeTarget.IsDead && GlobalPosition.DistanceTo(chargeTarget.GlobalPosition) <= MeleeRange * 1.5f)
                        {
                            if (Formulas.IsHitSuccessful(this, chargeTarget))
                            {
                                float dmg = Formulas.CalculateChargeAttackDamage(this, chargeTarget);
                                chargeTarget.TakeDamage(dmg, isSkill: true);
                                GameState.Instance.ModifyPower(Formulas.CalculatePowerGain());
                                GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), chargeTarget.GlobalPosition + new Vector2(0, -50), new Color(1f, 0.2f, 0.2f));
                            }
                        }
                    };
                }
                break;

            case 2: // Bite (Execute below 25% HP)
                if (GameState.Instance.PlayerPower >= 20f && GameState.Instance.SelectedTarget is Deer biteTarget && !biteTarget.IsDead)
                {
                    if (biteTarget.Health / biteTarget.MaxHealth <= 0.25f && GlobalPosition.DistanceTo(biteTarget.GlobalPosition) <= MeleeRange)
                    {
                        GameState.Instance.ModifyPower(-20f);
                        GameState.Instance.StartSkillCooldown(2);
                        TriggerAttackAnimation(2);

                        if (Formulas.IsHitSuccessful(this, biteTarget))
                        {
                            float dmg = Formulas.CalculateBiteDamage(this, biteTarget);
                            biteTarget.TakeDamage(dmg, isSkill: true);
                            GameState.Instance.ModifyHealth(20f); // Heal werewolf 20 HP
                            GameState.Instance.TriggerDamageNumber("+20 HP", GlobalPosition + new Vector2(0, -60), new Color(0.2f, 1f, 0.4f));
                            GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), biteTarget.GlobalPosition + new Vector2(0, -50), new Color(1f, 0.2f, 0.2f));
                        }
                    }
                }
                break;

            case 3: // Blood Howling (Ultimate buff)
                if (GameState.Instance.PlayerPower >= 40f)
                {
                    GameState.Instance.ModifyPower(-40f);
                    GameState.Instance.StartSkillCooldown(3);
                    TriggerAttackAnimation(3);
                    GameState.Instance.ApplyHowlBuff();
                    GameState.Instance.TriggerDamageNumber("HOWL BUFF!", GlobalPosition + new Vector2(0, -60), new Color(0.8f, 0.2f, 1f));
                }
                break;
        }
    }

    public void ToggleAutoInteract()
    {
        SetAutoInteract(!IsAutoInteracting);
    }

    public void SetAutoInteract(bool enabled)
    {
        var target = GameState.Instance.SelectedTarget;
        if (enabled && (target == null || (target is ISelectableTarget selectable && (selectable.IsDead || selectable.Health <= 0))))
        {
            IsAutoInteracting = false;
            OnAutoInteractToggled?.Invoke(false);
            return;
        }

        IsAutoInteracting = enabled;
        OnAutoInteractToggled?.Invoke(IsAutoInteracting);

        if (IsAutoInteracting)
        {
            PerformSingleInteractAction();
            _autoAttackTimer = 0f;
        }
        else
        {
            _autoAttackTimer = 0f;
        }
    }

    private void PerformSingleInteractAction()
    {
        if (GameState.Instance.SelectedTarget is TreeObject tree)
        {
            if (GlobalPosition.DistanceTo(tree.GlobalPosition) <= MeleeRange * 1.5f && !_isAttacking)
            {
                TriggerAttackAnimation(0);
                tree.Interact();
                if (tree.Health <= 0f || tree.IsDead)
                {
                    SetAutoInteract(false);
                }
            }
            else if (GlobalPosition.DistanceTo(tree.GlobalPosition) > MeleeRange * 1.5f)
            {
                GameState.Instance.TriggerDamageNumber("Auto: approaching...", GlobalPosition + new Vector2(0, -50), new Color(0.9f, 0.85f, 0.5f));
            }
        }
        else if (GameState.Instance.SelectedTarget is RockObject rock)
        {
            if (GlobalPosition.DistanceTo(rock.GlobalPosition) <= MeleeRange * 1.5f && !_isAttacking)
            {
                TriggerAttackAnimation(0);
                rock.Interact();
                if (rock.Health <= 0f || rock.IsDead)
                {
                    SetAutoInteract(false);
                }
            }
            else if (GlobalPosition.DistanceTo(rock.GlobalPosition) > MeleeRange * 1.5f)
            {
                GameState.Instance.TriggerDamageNumber("Auto: approaching...", GlobalPosition + new Vector2(0, -50), new Color(0.9f, 0.85f, 0.5f));
            }
        }
        else if (GameState.Instance.SelectedTarget is Deer deer)
        {
            if (GlobalPosition.DistanceTo(deer.GlobalPosition) <= MeleeRange && !_isAttacking)
            {
                ExecuteMeleeHit(deer);
                if (deer.Health <= 0f || deer.IsDead)
                {
                    SetAutoInteract(false);
                }
            }
            else if (GlobalPosition.DistanceTo(deer.GlobalPosition) > MeleeRange)
            {
                GameState.Instance.TriggerDamageNumber("Auto: approaching...", GlobalPosition + new Vector2(0, -50), new Color(0.9f, 0.85f, 0.5f));
            }
        }
    }

    public void TriggerInteractAction()
    {
        ToggleAutoInteract();
    }

    private void TriggerAttackAnimation(int type)
    {
        _isAttacking = true;
        _attackAnimType = type;
        _attackFrame = 0;
        _attackTimer = 0f;

        if (type == 2 || type == 3)
        {
            _sprite.Texture = _texMagicAttacks;
            _sprite.Hframes = 3;
            _sprite.Vframes = 3;
            _sprite.Frame = (type == 2) ? 0 : 6;
        }
        else
        {
            _sprite.Texture = _texAttacks;
            _sprite.Hframes = 3;
            _sprite.Vframes = 3;
            _sprite.Frame = type * 3;
        }
    }

    private void UpdateAnimation(float dt)
    {
        if (_isAttacking)
        {
            _attackTimer += dt;
            if (_attackTimer >= AttackFrameDuration)
            {
                _attackTimer = 0f;
                _attackFrame++;
                if (_attackFrame >= 3)
                {
                    _isAttacking = false;
                    _sprite.Texture = _texIdle;
                    _sprite.Hframes = 3;
                    _sprite.Vframes = 3;
                    _sprite.Frame = 0;
                    return;
                }

                int baseIndex = (_attackAnimType == 2) ? 0 : (_attackAnimType == 3) ? 6 : (_attackAnimType * 3);
                _sprite.Frame = baseIndex + _attackFrame;
            }
            return;
        }

        if (Velocity.LengthSquared() > 10f)
        {
            _sprite.Texture = _texMoving;
            _sprite.Hframes = 3;
            _sprite.Vframes = 3;

            bool isSprinting = Input.IsActionPressed("sprint");
            int row = isSprinting ? 1 : 0;

            _walkAnimTimer += dt;
            if (_walkAnimTimer >= (isSprinting ? WalkFrameDuration * 0.7f : WalkFrameDuration))
            {
                _walkAnimTimer = 0f;
                _walkFrame = (_walkFrame + 1) % 3;
            }

            _sprite.Frame = row * 3 + _walkFrame;
        }
        else
        {
            _sprite.Texture = _texIdle;
            _sprite.Hframes = 3;
            _sprite.Vframes = 3;

            _idleRowTimer -= dt;
            if (_idleRowTimer <= 0f)
            {
                _idleRowTimer = (float)GD.RandRange(4.0, 8.0);
                _idleRow = GD.RandRange(0, 2);
            }

            _idleAnimTimer += dt;
            if (_idleAnimTimer >= IdleFrameDuration)
            {
                _idleAnimTimer = 0f;
                _idleFrame = (_idleFrame + 1) % 3;
            }

            _sprite.Frame = _idleRow * 3 + _idleFrame;
        }
    }

    public void TakeDamage(float amount, bool isSkill = false)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
        }
    }
}
