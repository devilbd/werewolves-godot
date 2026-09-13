using Godot;
using Werewolves.Core;

namespace Werewolves.Entities;

public partial class DroppedLoot : Area2D, IFogBorderable
{
    [Export] public string ItemType { get; set; } = "Logs"; // "Logs", "Stones", "Meat", "GoldCoins"
    [Export] public int Amount { get; set; } = 1;

    // IFogBorderable implementation
    public Rect2 FogBounds => new Rect2(-24f, -24f, 48f, 48f);
    public Color FogBorderColor => (ItemType is "GoldCoins" or "Gold Coins" or "Gold")
        ? new Color(1.0f, 0.88f, 0.25f, 0.95f) // Treasure gold
        : (ItemType is "Meat"
            ? new Color(1.0f, 0.55f, 0.55f, 0.95f) // Crimson meat
            : (ItemType is "Quartz"
                ? new Color(0.85f, 0.70f, 1.0f, 0.95f) // Crystalline amethyst
                : (ItemType is "EmptyFlask" or "Empty Flask" or "Flask"
                    ? new Color(0.55f, 0.85f, 1.0f, 0.95f) // Glass cyan
                    : new Color(0.65f, 0.95f, 0.65f, 0.95f)))); // Forest loot

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;

    public static DroppedLoot Instantiate(string itemType, Vector2 position, int amount = 1)
    {
        var loot = new DroppedLoot
        {
            ItemType = itemType,
            GlobalPosition = position,
            Amount = amount
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
            "GoldCoins" or "Gold Coins" or "Gold" => GD.Load<Texture2D>("res://assets/gold_coins.png"),
            "Quartz" => GD.Load<Texture2D>("res://assets/resources/quartz/quartz_2.png"),
            "EmptyFlask" or "Empty Flask" or "Flask" => GD.Load<Texture2D>("res://assets/flasks/blood_flask_0.png"),
            _ => GD.Load<Texture2D>("res://assets/logs_o.png")
        };
        _sprite.Texture = tex;

        // User instruction: gold_coins on ground doubled from current size ((0.5f / 3f) * 2f = 1f / 3f)
        if (ItemType is "GoldCoins" or "Gold Coins" or "Gold")
        {
            _sprite.Scale = new Vector2(1f / 3f, 1f / 3f);
        }
        else if (ItemType is "Quartz")
        {
            _sprite.Scale = new Vector2(0.38f, 0.38f);
        }
        else if (ItemType is "EmptyFlask" or "Empty Flask" or "Flask")
        {
            _sprite.Scale = new Vector2(0.12f, 0.12f);
        }
        else
        {
            _sprite.Scale = new Vector2(0.6f, 0.6f);
        }
        AddChild(_sprite);

        _collision = new CollisionShape2D();
        float radius = (ItemType is "GoldCoins" or "Gold Coins" or "Gold") ? 28f : ((ItemType is "Quartz" or "EmptyFlask" or "Empty Flask" or "Flask") ? 22f : 30f);
        var circle = new CircleShape2D { Radius = radius };
        _collision.Shape = circle;
        AddChild(_collision);

        InputPickable = true;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        var cursor = GD.Load<Resource>("res://assets/cursors/grab_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
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
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            Collect();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Collect()
    {
        GameState.Instance.AddPouchItem(ItemType, Amount);

        string displayName = (ItemType is "GoldCoins" or "Gold Coins" or "Gold")
            ? (Amount > 1 ? $"{Amount} Gold Coins" : "1 Gold Coin")
            : ((ItemType is "EmptyFlask" or "Empty Flask" or "Flask")
                ? (Amount > 1 ? $"{Amount} Empty Flasks" : "1 Empty Flask")
                : (Amount > 1 ? $"{Amount} {ItemType}" : $"1 {ItemType}"));

        Color textColor = (ItemType is "GoldCoins" or "Gold Coins" or "Gold")
            ? new Color(1f, 0.85f, 0.2f)
            : (ItemType is "Quartz"
                ? new Color(0.85f, 0.70f, 1.0f)
                : ((ItemType is "EmptyFlask" or "Empty Flask" or "Flask")
                    ? new Color(0.45f, 0.85f, 1.0f)
                    : new Color(0.2f, 1f, 0.4f)));

        GameState.Instance.TriggerDamageNumber($"+{displayName}", GlobalPosition, textColor);

        // Reset cursor to normal
        var cursor = GD.Load<Resource>("res://assets/cursors/normal_o.png");
        if (cursor != null)
        {
            Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(0, 0));
        }

        QueueFree();
    }
}
