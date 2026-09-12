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
        dn.ZIndex = 100;
        dn.ZAsRelative = false;
        dn.Initialize(text, color);
        return dn;
    }

    public void Initialize(string text, Color color)
    {
        ZIndex = 100;
        ZAsRelative = false;

        _label = new Label();
        _label.Text = text;
        _label.Modulate = color;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.Position = new Vector2(-75, -16);
        _label.Size = new Vector2(150, 32);
        _label.AddThemeFontSizeOverride("font_size", 22);
        _label.AddThemeConstantOverride("outline_size", 4);
        _label.AddThemeColorOverride("font_outline_color", new Color(0.04f, 0.04f, 0.04f, 0.95f));
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
