using System;
using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class CaveStaticObject : StaticBody2D, ISelectableTarget, IFogBorderable
{
    [Export] public string ObjectType { get; set; } = "CraftingTable";

    public string TargetName => ObjectType switch
    {
        "CraftingTable" => "Crafting Table",
        "BloodJuicer" => "Blood Juicer",
        "Laboratory" => "Alchemical Laboratory",
        _ => "Cavern Installation"
    };

    public float Health { get; set; } = 100f;
    public float MaxHealth => 100f;
    public bool IsDead => false;

    public Vector2 FloatingTextPosition => ObjectType switch
    {
        "BloodJuicer" => GlobalPosition + new Vector2(0, -220f),
        "Laboratory" => GlobalPosition + new Vector2(0, -150f),
        _ => GlobalPosition + new Vector2(0, -160f)
    };

    public Rect2 TargetBounds => ObjectType switch
    {
        "BloodJuicer" => new Rect2(-100f, -230f, 200f, 235f),
        "Laboratory" => new Rect2(-100f, -160f, 200f, 165f),
        _ => new Rect2(-110f, -170f, 220f, 175f)
    };

    public Rect2 FogBounds => TargetBounds;

    public Color FogBorderColor => ObjectType switch
    {
        "BloodJuicer" => new Color(0.95f, 0.20f, 0.25f, 0.90f),
        "Laboratory" => new Color(0.40f, 0.85f, 0.70f, 0.90f),
        _ => new Color(0.85f, 0.65f, 0.35f, 0.90f)
    };

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private bool _isSelected = false;

    public static CaveStaticObject Instantiate(string objectType, Vector2 position)
    {
        return new CaveStaticObject
        {
            ObjectType = objectType,
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        string texturePath = ObjectType switch
        {
            "BloodJuicer" => "res://assets/cave-objects/blood-juicer.png",
            "Laboratory" => "res://assets/cave-objects/laboratory.png",
            _ => "res://assets/cave-objects/crafting-table.png"
        };

        var tex = GD.Load<Texture2D>(texturePath);

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _sprite.Texture = tex;
        _sprite.Scale = new Vector2(0.35f, 0.35f);
        if (tex != null)
        {
            _sprite.Offset = new Vector2(0, -tex.GetHeight() * 0.45f);
        }

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            Vector2 colSize = ObjectType switch
            {
                "BloodJuicer" => new Vector2(110f, 40f),
                "Laboratory" => new Vector2(130f, 40f),
                _ => new Vector2(130f, 40f)
            };
            _collision.Shape = new RectangleShape2D { Size = colSize };
            _collision.Position = new Vector2(0, -15f);
            AddChild(_collision);
        }

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _ExitTree()
    {
        CursorManager.ResetNormal();
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
            if (dist <= 220f)
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
        Vibrate(4f, 0.15f);

        if (ObjectType == "CraftingTable")
        {
            GameState.Instance.OpenCraftingStation("CraftingTable");
        }
        else if (ObjectType == "BloodJuicer")
        {
            GameState.Instance.ToggleBloodJuicer(true);
        }
        else if (ObjectType == "Laboratory")
        {
            GameState.Instance.OpenCraftingStation("Laboratory");
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
