using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class DroppedLoot : Area2D
{
    [Export] public string ItemType { get; set; } = "Logs"; // "Logs", "Stones", "Meat"

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;
    private bool _isHovered = false;

    public static DroppedLoot Instantiate(string itemType, Vector2 position)
    {
        var loot = new DroppedLoot
        {
            ItemType = itemType,
            GlobalPosition = position
        };
        return loot;
    }

    public override void _Ready()
    {
        _sprite = new Sprite2D();
        Texture2D? tex = ItemType switch
        {
            "Logs" => GD.Load<Texture2D>("res://assets/logs_o.png"),
            "Stones" => GD.Load<Texture2D>("res://assets/rock_stones_loot_o.png"),
            "Meat" => GD.Load<Texture2D>("res://assets/meat_o.png"),
            _ => GD.Load<Texture2D>("res://assets/logs_o.png")
        };
        _sprite.Texture = tex;
        _sprite.Scale = new Vector2(0.6f, 0.6f);
        AddChild(_sprite);

        _collision = new CollisionShape2D();
        var circle = new CircleShape2D { Radius = 30f };
        _collision.Shape = circle;
        AddChild(_collision);

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        var cursor = GD.Load<Resource>("res://assets/cursors/grab_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
        }
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
        }
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            Collect();
        }
    }

    public void Collect()
    {
        GameState.Instance.AddPouchItem(ItemType, 1);
        GameState.Instance.TriggerDamageNumber($"+1 {ItemType}", GlobalPosition, new Color(0.2f, 1f, 0.4f));

        // Reset cursor to normal
        var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
        }

        QueueFree();
    }
}
