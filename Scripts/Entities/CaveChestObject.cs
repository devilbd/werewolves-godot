using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class CaveChestObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    [Export] public string ChestId { get; set; } = "";

    public string TargetName => "Storage Chest";
    public float Health { get; set; } = 100f;
    public float MaxHealth => 100f;
    public bool IsDead => false;

    public Vector2 FloatingTextPosition => GlobalPosition + new Vector2(0, -65f);
    public Rect2 TargetBounds => new Rect2(-36f, -60f, 72f, 65f);

    public Rect2 FogBounds => new Rect2(-40f, -65f, 80f, 70f);
    public Color FogBorderColor => new Color(0.95f, 0.80f, 0.35f, 0.90f);

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private Texture2D _texClosed = null!;
    private Texture2D _texOpened = null!;
    private bool _isSelected = false;

    public static CaveChestObject Instantiate(string chestId, Vector2 position)
    {
        var chest = new CaveChestObject
        {
            ChestId = chestId,
            GlobalPosition = position
        };
        return chest;
    }

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

        bool isCurrentlyOpen = GameState.Instance.IsChestInventoryOpen && GameState.Instance.ActiveChestId == ChestId;
        _sprite.Texture = isCurrentlyOpen ? _texOpened : _texClosed;
        _sprite.Scale = new Vector2(0.30f, 0.30f);
        _sprite.Offset = new Vector2(0, -_texClosed.GetHeight() * 0.45f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            _collision.Shape = new RectangleShape2D { Size = new Vector2(50f, 26f) };
            _collision.Position = new Vector2(0, -12f);
            AddChild(_collision);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        GameState.Instance.OnChestInventoryToggled += OnChestInventoryToggled;
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnChestInventoryToggled -= OnChestInventoryToggled;
        }
        CursorManager.ResetNormal();
    }

    private void OnChestInventoryToggled(bool isOpen, string? activeId)
    {
        if (!GodotObject.IsInstanceValid(this) || _sprite == null) return;

        if (isOpen && activeId == ChestId)
        {
            _sprite.Texture = _texOpened;
            _sprite.Offset = new Vector2(0, -_texOpened.GetHeight() * 0.45f);
        }
        else
        {
            _sprite.Texture = _texClosed;
            _sprite.Offset = new Vector2(0, -_texClosed.GetHeight() * 0.45f);
        }
    }

    private void OnMouseEntered()
    {
        if (IsInsideTree() && !GameState.Instance.IsPlacingChest)
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
        if (GameState.Instance.IsPlacingChest) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            GameState.Instance.SelectedTarget = this;
            GetViewport().SetInputAsHandled();

            float dist = GlobalPosition.DistanceTo(GameState.Instance.PlayerPosition);
            if (dist <= 200f)
            {
                Interact();
            }
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
        if (string.IsNullOrEmpty(ChestId)) return;

        Vibrate(4f, 0.15f);

        if (GameState.Instance.IsChestInventoryOpen && GameState.Instance.ActiveChestId == ChestId)
        {
            GameState.Instance.CloseChestInventory();
        }
        else
        {
            GameState.Instance.OpenChestInventory(ChestId);
        }
    }

    private Tween? _shakeTween;

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
}
