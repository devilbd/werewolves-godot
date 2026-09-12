using System;
using Godot;

namespace Werewolves.World;

public partial class FogZone : Node2D
{
    [Export] public float ZoneRadius { get; set; } = 600f;
    [Export] public Color FogColor { get; set; } = new Color(0.88f, 0.94f, 1.0f, 0.92f);
    [Export] public float Density { get; set; } = 1.15f;
    [Export] public float Coverage { get; set; } = 0.58f;
    [Export] public float DisappearCycleSpeed { get; set; } = 0.08f;
    [Export] public float TimeOffset { get; set; } = 0.0f;
    [Export] public float RadialFalloff { get; set; } = 0.45f;

    private static Shader? _fogShader;
    private static NoiseTexture2D? _noiseTexture;

    private ColorRect _fogRect = null!;
    private ShaderMaterial _material = null!;

    public static FogZone Instantiate(
        Vector2 position,
        float radius,
        Color color,
        float density = 1.15f,
        float coverage = 0.58f,
        float cycleSpeed = 0.08f,
        float timeOffset = -1f)
    {
        return new FogZone
        {
            GlobalPosition = position,
            ZoneRadius = radius,
            FogColor = color,
            Density = density,
            Coverage = coverage,
            DisappearCycleSpeed = cycleSpeed,
            TimeOffset = timeOffset < 0f ? (float)GD.RandRange(0.0, 100.0) : timeOffset
        };
    }

    public override void _Ready()
    {
        EnsureResourcesLoaded();

        float diameter = ZoneRadius * 2.0f;

        _fogRect = GetNodeOrNull<ColorRect>("FogRect");
        if (_fogRect == null)
        {
            _fogRect = new ColorRect
            {
                Name = "FogRect",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_fogRect);
        }

        _fogRect.Size = new Vector2(diameter, diameter);
        _fogRect.Position = new Vector2(-ZoneRadius, -ZoneRadius);

        _material = new ShaderMaterial
        {
            Shader = _fogShader
        };

        if (_noiseTexture != null)
        {
            _material.SetShaderParameter("noise_texture", _noiseTexture);
        }
        _material.SetShaderParameter("fog_color", FogColor);
        _material.SetShaderParameter("density", Density);
        _material.SetShaderParameter("coverage", Coverage);
        _material.SetShaderParameter("disappear_cycle_speed", DisappearCycleSpeed);
        _material.SetShaderParameter("time_offset", TimeOffset);
        _material.SetShaderParameter("radial_falloff", RadialFalloff);

        _fogRect.Material = _material;
    }

    private static void EnsureResourcesLoaded()
    {
        _fogShader ??= GD.Load<Shader>("res://shaders/fog.gdshader");

        if (_noiseTexture == null)
        {
            _noiseTexture = GD.Load<NoiseTexture2D>("res://assets/fog_noise.tres");
            if (_noiseTexture == null)
            {
                var noise = new FastNoiseLite
                {
                    NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
                    Frequency = 0.007f,
                    FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
                    FractalOctaves = 4,
                    FractalLacunarity = 2.0f,
                    FractalGain = 0.5f
                };

                _noiseTexture = new NoiseTexture2D
                {
                    Width = 512,
                    Height = 512,
                    Seamless = true,
                    Noise = noise
                };
            }
        }
    }
}
