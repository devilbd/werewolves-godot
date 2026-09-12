using Godot;

namespace Werewolves.Entities;

public partial class LanternObject : StaticBody2D
{
    [Export] public float LanternScale { get; set; } = 0.18f;

    private Sprite2D _sprite = null!;
    private CollisionShape2D _collision = null!;

    public static LanternObject Instantiate(Vector2 position, float scale = 0.18f)
    {
        var lantern = new LanternObject
        {
            GlobalPosition = position,
            LanternScale = scale
        };
        return lantern;
    }

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _sprite.Texture = GD.Load<Texture2D>("res://assets/houses/lantern_light.png");
        _sprite.Scale = new Vector2(LanternScale, LanternScale);
        // Base of the lamp post is centered at X ~ 327, Y ~ 550 within the 600x600 sprite
        _sprite.Offset = new Vector2(-27f, -250f);

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collision == null)
        {
            _collision = new CollisionShape2D { Name = "CollisionShape2D" };
            var circle = new CircleShape2D { Radius = 8f };
            _collision.Shape = circle;
            _collision.Position = new Vector2(0, -6f);
            AddChild(_collision);
        }
    }
}
