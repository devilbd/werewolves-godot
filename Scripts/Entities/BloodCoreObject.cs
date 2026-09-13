using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class BloodCoreObject : StaticBody2D, IFogBorderable
{
    public const float InteractionDistance = 220f;
    public const float RestoreCost = 250f;
    public const float RestoreHealthAmount = 12f;
    public const float RestorePowerAmount = 13f;

    // IFogBorderable
    public Rect2 FogBounds => new Rect2(-110f, -310f, 220f, 330f);
    public Color FogBorderColor => new Color(0.95f, 0.15f, 0.20f, 0.95f);

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Control _uiContainer = null!;
    private ProgressBar _progressBar = null!;
    private Label _percentLabel = null!;
    private VBoxContainer _promptContainer = null!;
    private Label _restorePromptLabel = null!;
    private Label _fillPromptLabel = null!;

    private float _pulseTimer = 0f;
    private bool _isPlayerNear = false;
    private Tween? _pulseTween;

    public static BloodCoreObject Instantiate(Vector2 position)
    {
        return new BloodCoreObject
        {
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        // 1. Sprite
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            _sprite.Texture = GD.Load<Texture2D>("res://assets/cave-objects/blood-core.png");
        }
        _sprite.Scale = new Vector2(0.38f, 0.38f);
        _sprite.Offset = new Vector2(0f, -360f);

        // 2. Pedestal Physical Collision
        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            _collision.Shape = new CircleShape2D { Radius = 45f };
            _collision.Position = new Vector2(0f, 20f);
            AddChild(_collision);
        }

        // 3. UI Container (Progress bar and prompts above the sprite)
        BuildUI();

        // 4. Listen for reserves changes
        GameState.Instance.OnBloodCoreReservesChanged += OnReservesChanged;
        UpdateProgressBar();
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnBloodCoreReservesChanged -= OnReservesChanged;
        }
    }

    private void BuildUI()
    {
        _uiContainer = GetNodeOrNull<Control>("UIContainer");
        if (_uiContainer == null)
        {
            _uiContainer = new Control
            {
                Name = "UIContainer",
                ZIndex = 20,
                Position = Vector2.Zero
            };
            AddChild(_uiContainer);
        }

        // 3.1 Progress Bar at the top of the sprite
        _progressBar = new ProgressBar
        {
            Name = "BloodProgressBar",
            MinValue = 0,
            MaxValue = GameState.Instance.BloodCoreMaxReserves,
            Value = GameState.Instance.BloodCoreReserves,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(190f, 18f),
            Size = new Vector2(190f, 18f),
            Position = new Vector2(-95f, -330f)
        };

        // Dark background style
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.03f, 0.05f, 0.90f),
            BorderColor = new Color(0.65f, 0.25f, 0.30f, 1f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        _progressBar.AddThemeStyleboxOverride("background", bgStyle);

        // Crimson fill style
        var fillStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.85f, 0.12f, 0.18f, 1f),
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2
        };
        _progressBar.AddThemeStyleboxOverride("fill", fillStyle);
        _uiContainer.AddChild(_progressBar);

        // Percentage & points text overlay on progress bar
        _percentLabel = new Label
        {
            Name = "PercentLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(190f, 18f),
            Position = Vector2.Zero
        };
        _percentLabel.AddThemeFontSizeOverride("font_size", 11);
        _percentLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.95f));
        _percentLabel.AddThemeConstantOverride("outline_size", 2);
        _percentLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _progressBar.AddChild(_percentLabel);

        // 3.2 Action prompts container below progress bar
        _promptContainer = new VBoxContainer
        {
            Name = "PromptContainer",
            Position = new Vector2(-150f, -300f),
            Size = new Vector2(300f, 50f),
            Visible = false
        };
        _promptContainer.AddThemeConstantOverride("separation", 2);

        _restorePromptLabel = new Label
        {
            Name = "RestorePrompt",
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "Press [E] to Restore (250 Blood)"
        };
        _restorePromptLabel.AddThemeFontSizeOverride("font_size", 13);
        _restorePromptLabel.AddThemeConstantOverride("outline_size", 3);
        _restorePromptLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _promptContainer.AddChild(_restorePromptLabel);

        _fillPromptLabel = new Label
        {
            Name = "FillPrompt",
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "Press [R] to Fill Core with Blood Flask"
        };
        _fillPromptLabel.AddThemeFontSizeOverride("font_size", 12);
        _fillPromptLabel.AddThemeConstantOverride("outline_size", 3);
        _fillPromptLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _promptContainer.AddChild(_fillPromptLabel);

        _uiContainer.AddChild(_promptContainer);
    }

    private void OnReservesChanged(float current, float max)
    {
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (_progressBar == null || _percentLabel == null) return;

        float current = GameState.Instance.BloodCoreReserves;
        float max = GameState.Instance.BloodCoreMaxReserves;
        _progressBar.MaxValue = max;
        _progressBar.Value = current;

        int percent = max > 0 ? (int)Math.Round((current / max) * 100f) : 0;
        _percentLabel.Text = $"{percent}% ({(int)current} / {(int)max})";
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var player = GameState.Instance.PlayerPosition;
        float distSq = GlobalPosition.DistanceSquaredTo(player);
        _isPlayerNear = distSq <= (InteractionDistance * InteractionDistance);

        if (_isPlayerNear)
        {
            _promptContainer.Visible = true;
            _pulseTimer += dt * 4f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer);
            float alpha = 0.55f + 0.45f * pulse;

            bool canRestore = GameState.Instance.BloodCoreReserves >= RestoreCost;
            if (canRestore)
            {
                _restorePromptLabel.Text = "Press [E] to Restore Health & Power (250 Blood)";
                _restorePromptLabel.Modulate = new Color(1.0f, 0.93f, 0.50f, alpha);
            }
            else
            {
                _restorePromptLabel.Text = "Blood Core Depleted (Needs 250 Blood)";
                _restorePromptLabel.Modulate = new Color(0.85f, 0.40f, 0.40f, alpha * 0.85f);
            }

            bool hasFlask = HasFilledBloodFlask();
            if (hasFlask)
            {
                _fillPromptLabel.Text = "Press [R] to Fill Core with Blood Flask";
                _fillPromptLabel.Modulate = new Color(0.95f, 0.65f, 0.45f, alpha);
            }
            else
            {
                _fillPromptLabel.Text = "Press [R] to Fill Core (No flasks in pouch)";
                _fillPromptLabel.Modulate = new Color(0.65f, 0.65f, 0.65f, alpha * 0.7f);
            }
        }
        else
        {
            _promptContainer.Visible = false;
            _pulseTimer = 0f;
        }
    }

    private bool HasFilledBloodFlask()
    {
        foreach (var kvp in GameState.Instance.PouchItems)
        {
            if (kvp.Key.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase) && kvp.Value.BloodPercent > 0)
            {
                return true;
            }
        }
        return false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isPlayerNear) return;

        if (@event.IsActionPressed("interact_core") ||
            (@event is InputEventKey keyE && keyE.Pressed && !keyE.Echo && keyE.Keycode == Key.E))
        {
            GetViewport().SetInputAsHandled();
            PerformRestore();
        }
        else if (@event.IsActionPressed("fill_core") ||
            (@event is InputEventKey keyR && keyR.Pressed && !keyR.Echo && keyR.Keycode == Key.R))
        {
            GetViewport().SetInputAsHandled();
            PerformFill();
        }
    }

    public void PerformRestore()
    {
        if (GameState.Instance.BloodCoreReserves < RestoreCost)
        {
            GameState.Instance.TriggerDamageNumber("Not enough blood in Core! (Needs 250)", GlobalPosition + new Vector2(0, -180), new Color(1f, 0.35f, 0.35f));
            return;
        }

        if (GameState.Instance.PlayerHealth >= GameState.Instance.PlayerMaxHealth &&
            GameState.Instance.PlayerPower >= GameState.Instance.PlayerMaxPower)
        {
            GameState.Instance.TriggerDamageNumber("Health and Power already full!", GlobalPosition + new Vector2(0, -180), new Color(1f, 0.90f, 0.45f));
            return;
        }

        if (GameState.Instance.TryDrainBloodCore(RestoreCost))
        {
            GameState.Instance.ModifyHealth(RestoreHealthAmount);
            GameState.Instance.ModifyPower(RestorePowerAmount);

            GameState.Instance.TriggerDamageNumber($"+{(int)RestoreHealthAmount} HP  +{(int)RestorePowerAmount} Power", GlobalPosition + new Vector2(0, -180), new Color(0.40f, 1.0f, 0.45f));
            PlayPulseAnimation();
        }
    }

    public void PerformFill()
    {
        string? targetKey = null;
        int targetPercent = 0;

        foreach (var kvp in GameState.Instance.PouchItems)
        {
            if (kvp.Key.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase) && kvp.Value.BloodPercent > 0)
            {
                targetKey = kvp.Key;
                targetPercent = kvp.Value.BloodPercent;
                break;
            }
        }

        if (targetKey == null)
        {
            GameState.Instance.TriggerDamageNumber("No blood flasks in pouch!", GlobalPosition + new Vector2(0, -180), new Color(1f, 0.40f, 0.40f));
            return;
        }

        if (GameState.Instance.BloodCoreReserves >= GameState.Instance.BloodCoreMaxReserves)
        {
            GameState.Instance.TriggerDamageNumber("Blood Core is already full!", GlobalPosition + new Vector2(0, -180), new Color(1f, 0.90f, 0.45f));
            return;
        }

        // Each 1% blood gives 2.5 points in the core (100% flask = 250 blood points)
        float points = targetPercent * 2.5f;
        float added = GameState.Instance.AddBloodCoreReserves(points);

        // Remove the filled flask
        GameState.Instance.PouchItems.Remove(targetKey);

        // Recover 1 EmptyFlask in stack
        if (GameState.Instance.PouchItems.TryGetValue("EmptyFlask", out var empty))
        {
            empty.Count++;
        }
        else
        {
            GameState.Instance.PouchItems["EmptyFlask"] = new PouchItemData { Count = 1 };
        }

        GameState.Instance.TriggerDamageNumber($"+{(int)added} Blood (Empty Flask)", GlobalPosition + new Vector2(0, -180), new Color(0.95f, 0.35f, 0.45f));
        SaveManager.SaveGame();
        PlayPulseAnimation();
    }

    private void PlayPulseAnimation()
    {
        if (_sprite == null) return;
        _pulseTween?.Kill();
        _pulseTween = CreateTween();
        _pulseTween.TweenProperty(_sprite, "scale", new Vector2(0.40f, 0.40f), 0.12f);
        _pulseTween.TweenProperty(_sprite, "scale", new Vector2(0.38f, 0.38f), 0.20f);
    }
}
