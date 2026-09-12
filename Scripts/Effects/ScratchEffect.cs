using Godot;

namespace Werewolves.Effects;

public partial class ScratchEffect : Node2D
{
    private Sprite2D _sprite = null!;
    private int _currentFrame = 0;
    private const int TotalFrames = 4;
    private float _frameTimer = 0f;
    private const float FrameDuration = 0.06f; // ~4 frames in 0.24s

    public static ScratchEffect Instantiate(Vector2 position, bool flip = false)
    {
        var effect = new ScratchEffect();
        effect.GlobalPosition = position;
        effect.Setup(flip);
        return effect;
    }

    public void Setup(bool flip)
    {
        _sprite = new Sprite2D();
        var texture = GD.Load<Texture2D>("res://assets/scratch_hit.png");
        _sprite.Texture = texture;
        _sprite.Hframes = TotalFrames;
        _sprite.Vframes = 1;
        _sprite.Frame = 0;

        // In web game: rotated 90 degrees (Math.PI / 2), and optional horizontal flip
        _sprite.Rotation = Mathf.Pi / 2f;
        if (flip)
        {
            _sprite.Scale = new Vector2(-1, 1);
        }

        AddChild(_sprite);
    }

    public override void _Process(double delta)
    {
        _frameTimer += (float)delta;
        if (_frameTimer >= FrameDuration)
        {
            _frameTimer = 0f;
            _currentFrame++;
            if (_currentFrame >= TotalFrames)
            {
                QueueFree();
                return;
            }
            _sprite.Frame = _currentFrame;
        }
    }
}
