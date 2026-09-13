using Godot;

namespace Werewolves.Effects;

/// <summary>
/// Combat hit effect displaying randomized blood splatters from assets/blood-hits/ (1..3)
/// scaled to 1/3 size with smooth rapid fade-out.
/// </summary>
public partial class ScratchEffect : Node2D
{
    private Sprite2D _sprite = null!;
    private float _timer = 0f;
    private const float Duration = 0.22f;

    public static ScratchEffect Instantiate(Vector2 position, bool flip = false)
    {
        var effect = new ScratchEffect();
        effect.GlobalPosition = position;
        effect.Setup(flip);
        return effect;
    }

    public void Setup(bool flip)
    {
        _sprite = new Sprite2D { Name = "Sprite2D" };

        // Randomly choose blood hit texture 1, 2, or 3
        int variant = GD.RandRange(1, 3);
        var texture = GD.Load<Texture2D>($"res://assets/blood-hits/{variant}.png");
        _sprite.Texture = texture;

        // Size divided by 3 per design specification
        float baseScale = 1f / 3f;
        _sprite.Scale = new Vector2(flip ? -baseScale : baseScale, baseScale);
        _sprite.Rotation = (float)GD.RandRange(-0.2f, 0.2f);
        _sprite.ZIndex = 20; // Ensure visible above character sprites

        AddChild(_sprite);
    }

    public override void _Process(double delta)
    {
        _timer += (float)delta;
        float alpha = Mathf.Clamp(1f - (_timer / Duration), 0f, 1f);
        _sprite.Modulate = new Color(1f, 1f, 1f, alpha);

        if (_timer >= Duration)
        {
            QueueFree();
        }
    }
}

/// <summary>
/// Alias for ScratchEffect reflecting semantic blood splatter hit behavior.
/// </summary>
public partial class BloodHitEffect : ScratchEffect
{
}
