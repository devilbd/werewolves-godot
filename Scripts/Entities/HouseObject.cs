using Godot;

namespace Werewolves.Entities;

public partial class HouseObject : StaticBody2D, IFogBorderable
{
    [Export] public int HouseVariant { get; set; } = 1;

    // IFogBorderable implementation
    public Rect2 FogBounds => new Rect2(-155f, -225f, 310f, 235f);
    public Color FogBorderColor => new Color(0.92f, 0.78f, 0.55f, 0.90f); // Warm timber gold

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
        _sprite.Scale = new Vector2(1.25f, 1.25f);
        _sprite.Offset = new Vector2(0, -_sprite.Texture.GetHeight() * 0.35f);

        // Houses allow the werewolf to pass through freely without stopping
        CollisionLayer = 0;
        CollisionMask = 0;

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision != null)
        {
            _collision.Disabled = true;
        }
    }
}
