using Godot;

namespace Werewolves.Entities;

public partial class TerrainArtifact : Node2D
{
    [Export] public int Variant { get; set; } = 0; // 1 to 8 (0 = random)

    private Sprite2D _sprite = null!;

    public static TerrainArtifact Instantiate(int variant, Vector2 position)
    {
        return new TerrainArtifact
        {
            Variant = variant,
            GlobalPosition = position
        };
    }

    public override void _Ready()
    {
        ZIndex = -5;

        if (Variant < 1 || Variant > 8)
        {
            Variant = GD.RandRange(1, 8);
        }

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        if (_sprite.Texture == null)
        {
            string path = $"res://assets/terrain-artifacts/{Variant}.png";
            _sprite.Texture = GD.Load<Texture2D>(path);
        }

        _sprite.Scale = new Vector2(0.45f, 0.45f);
        _sprite.FlipH = GD.Randf() > 0.5f;
        Rotation = (float)GD.RandRange(-0.08f, 0.08f);
    }
}
