using Godot;

namespace Werewolves.Effects;

public partial class BuffAura : Node2D
{
    private static Texture2D? _circleTexture;
    private CpuParticles2D _particles = null!;

    public override void _Ready()
    {
        if (_circleTexture == null)
        {
            _circleTexture = CreateCircleTexture(32);
        }

        // Additive blending material for glowing shine
        var mat = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };

        _particles = new CpuParticles2D
        {
            Material = mat,
            Texture = _circleTexture,
            Amount = 35,
            Lifetime = 1.1,
            Preprocess = 0.5,
            SpeedScale = 1.0f,
            Explosiveness = 0.05f,
            Randomness = 0.5f,

            // Position around werewolf torso
            Position = new Vector2(0, -35),

            // Emission & Motion
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 30f,
            Gravity = new Vector2(0, -60),
            Direction = new Vector2(0, -1),
            Spread = 35f,
            InitialVelocityMin = 20f,
            InitialVelocityMax = 50f,

            // Small, shiny circle scale (base 32px scaled to ~8-18px)
            ScaleAmountMin = 0.25f,
            ScaleAmountMax = 0.55f,

            // Vibrant glowing violet / magenta
            Color = new Color(0.9f, 0.4f, 1.0f, 0.85f)
        };

        // Smooth alpha fade over lifetime
        var grad = new Gradient();
        grad.SetColor(0, new Color(1f, 0.7f, 1f, 0.2f));
        grad.AddPoint(0.25f, new Color(0.95f, 0.45f, 1f, 0.95f));
        grad.AddPoint(0.7f, new Color(0.7f, 0.15f, 0.95f, 0.7f));
        grad.SetColor(grad.GetPointCount() - 1, new Color(0.5f, 0.0f, 0.8f, 0.0f));
        _particles.ColorRamp = grad;

        AddChild(_particles);
    }

    private static Texture2D CreateCircleTexture(int size)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new Vector2(size / 2.0f, size / 2.0f);
        float radius = size / 2.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = new Vector2(x + 0.5f, y + 0.5f).DistanceTo(center);
                if (dist < radius)
                {
                    float norm = dist / radius;
                    float alpha = Mathf.Clamp(1.0f - norm * norm, 0.0f, 1.0f);
                    float core = Mathf.Clamp(1.0f - (dist / (radius * 0.45f)), 0.0f, 1.0f);

                    Color col = new Color(
                        Mathf.Lerp(0.85f, 1.0f, core),
                        Mathf.Lerp(0.4f, 1.0f, core),
                        1.0f,
                        alpha
                    );
                    img.SetPixel(x, y, col);
                }
                else
                {
                    img.SetPixel(x, y, Colors.Transparent);
                }
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
