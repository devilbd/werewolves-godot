using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class ChestObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    public string TargetName => "Treasure Chest";
    public float Health { get; set; } = 100f;
    public float MaxHealth => 100f;
    public bool IsDead => _isOpened;

    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -65f);
    public Rect2 TargetBounds => new Rect2(-38f, -60f, 76f, 65f);

    // IFogBorderable implementation (warm treasure gold glow)
    public Rect2 FogBounds => new Rect2(-42f, -65f, 84f, 72f);
    public Color FogBorderColor => new Color(1.0f, 0.85f, 0.30f, 0.90f);

    private bool _isOpened = false;
    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Texture2D _texClosed = null!;
    private Texture2D _texOpened = null!;
    private Tween? _shakeTween;
    private bool _isSelected = false;

    public override void _Ready()
    {
        _texClosed = GD.Load<Texture2D>("res://assets/chests/chest_closed.png");
        _texOpened = GD.Load<Texture2D>("res://assets/chests/chest_opened.png");

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _sprite.Texture = _texClosed;
        _sprite.Scale = new Vector2(0.22f, 0.22f);
        _sprite.Offset = new Vector2(0, -_texClosed.GetHeight() * 0.45f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            _collision.Shape = new CircleShape2D { Radius = 20f };
            _collision.Position = new Vector2(0, -10f);
            AddChild(_collision);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        if (!_isOpened && IsInsideTree())
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
        if (!_isOpened && @event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            GameState.Instance.SelectedTarget = this;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        // TargetReticle handles drawing
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

    public void Vibrate(float intensity = 6f, float duration = 0.20f)
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
        if (_isOpened) return;

        _isOpened = true;
        InputPickable = false;
        Health = 0f;

        Vibrate(6f, 0.2f);
        _sprite.Texture = _texOpened;
        _sprite.Offset = new Vector2(0, -_texOpened.GetHeight() * 0.45f);

        GameState.Instance.TriggerDamageNumber("Chest Opened!", FloatingTextPosition, new Color(1.0f, 0.88f, 0.25f));

        SpawnLootBurst();

        if (GameState.Instance.SelectedTarget == this)
        {
            GameState.Instance.SelectedTarget = null;
        }

        CursorManager.ForceResetNormal();

        // Keep the opened chest visible for 6 seconds, then smoothly fade out
        GetTree().CreateTimer(6.0).Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(this) || _sprite == null) return;
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.0f, 1.2f);
            tween.TweenCallback(Callable.From(QueueFree));
        };
    }

    private void SpawnLootBurst()
    {
        var parent = GetParent();
        if (parent == null) return;

        var lootCfg = ConfigManager.Chests.Loot;

        // 1. Empty Flask drop chance check
        if (GD.Randf() < lootCfg.FlaskDropChance)
        {
            SpawnLootItem("EmptyFlask", lootCfg.FlaskAmount, parent);
        }

        // 2. Resource pool for random drops
        var candidates = new System.Collections.Generic.List<ChestResourceDrop>(lootCfg.Resources);

        // Determine number of resource types to drop
        int resourceTypeCount = GD.Randf() < lootCfg.SingleResourceTypeChance ? 1 : lootCfg.MaxResourceTypes;

        for (int i = 0; i < resourceTypeCount && candidates.Count > 0; i++)
        {
            int index = GD.RandRange(0, candidates.Count - 1);
            var chosen = candidates[index];
            candidates.RemoveAt(index);

            int amount = GD.RandRange(chosen.MinAmount, chosen.MaxAmount);
            if (amount > 0)
            {
                SpawnLootItem(chosen.ItemType, amount, parent);
            }
        }
    }

    private void SpawnLootItem(string itemType, int count, Node parent)
    {
        float angle = GD.Randf() * Mathf.Pi * 2f;
        float dist = (float)GD.RandRange(35f, 65f);
        Vector2 dropPos = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        var loot = DroppedLoot.Instantiate(itemType, dropPos, count);
        parent.AddChild(loot);
    }
}
