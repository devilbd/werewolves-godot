using Godot;

namespace Werewolves.Entities;

public partial class HouseObject : StaticBody2D
{
    [Export] public int HouseVariant { get; set; } = 1;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;

    public static HouseObject Instantiate(int variant, Vector2 position)
    {
        var house = new HouseObject
        {
            HouseVariant = variant,
            GlobalPosition = position
        };
        return house;
    }

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        string path = $"res://assets/houses/house_{Mathf.Clamp(HouseVariant, 1, 4)}.png";
        _sprite.Texture = GD.Load<Texture2D>(path);
        _sprite.Scale = new Vector2(0.5f, 0.5f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.35f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var box = new RectangleShape2D
            {
                Size = new Vector2(_sprite.Texture.GetWidth() * 0.45f, _sprite.Texture.GetHeight() * 0.22f)
            };
            _collision.Shape = box;
            _collision.Position = new Vector2(0, -10);
            AddChild(_collision);
        }
    }
}
