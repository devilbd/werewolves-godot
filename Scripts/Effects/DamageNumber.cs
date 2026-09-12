using Godot;

namespace Werewolves.Effects;

public partial class DamageNumber : Node2D
{
    private Label _label = null!;
    private float _lifetime = 1.0f;
    private float _timer = 0f;
    private float _verticalSpeed = 50f;

    public static DamageNumber Instantiate(string text, Vector2 position, Color color)
    {
        var dn = new DamageNumber();
        dn.GlobalPosition = position;
        dn.Initialize(text, color);
        return dn;
    }

    public void Initialize(string text, Color color)
    {
        _label = new Label();
        _label.Text = text;
        _label.Modulate = color;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.Position = new Vector2(-40, -15);
        _label.Size = new Vector2(80, 30);
        _label.AddThemeFontSizeOverride("font_size", 22);
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _timer += dt;

        // Upward drift
        Position += new Vector2(0, -_verticalSpeed * dt);

        // Alpha fade out
        float alpha = Mathf.Clamp(1.0f - (_timer / _lifetime), 0f, 1.0f);
        Modulate = new Color(1, 1, 1, alpha);

        if (_timer >= _lifetime)
        {
            QueueFree();
        }
    }
}
