using System;
using System.Collections.Generic;
using Godot;

namespace Werewolves.World;

public partial class FogZone : Node2D
{
    [Export] public float ZoneRadius { get; set; } = 600f;
    [Export] public Color FogColor { get; set; } = new Color(0.88f, 0.94f, 1.0f, 0.92f);
    [Export] public float Density { get; set; } = 0.60f;
    [Export] public float Coverage { get; set; } = 0.55f;
    [Export] public float DisappearCycleSpeed { get; set; } = 0.04f;
    [Export] public float TimeOffset { get; set; } = 0.0f;
    [Export] public float RadialFalloff { get; set; } = 0.45f;

    private static readonly List<FogZone> _activeZones = new();
    public static IReadOnlyList<FogZone> ActiveZones => _activeZones;

    private static Shader? _fogShader;
    private static NoiseTexture2D? _noiseTexture;

    private ColorRect _fogRect = null!;
    private ShaderMaterial _material = null!;

    public override void _EnterTree()
    {
        if (!_activeZones.Contains(this))
        {
            _activeZones.Add(this);
        }
    }

    public override void _ExitTree()
    {
        _activeZones.Remove(this);
    }

    /// <summary>
    /// Checks if a world position is inside any active fog zone.
    /// </summary>
    public static bool IsPositionInFog(Vector2 worldPos, float buffer = 0f)
    {
        for (int i = 0; i < _activeZones.Count; i++)
        {
            var zone = _activeZones[i];
            if (GodotObject.IsInstanceValid(zone) && zone.IsInsideTree() && zone.Visible)
            {
                if (worldPos.DistanceTo(zone.GlobalPosition) <= zone.ZoneRadius + buffer)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Calculates fog density factor (0.0 to 1.0) at the given world position with radial falloff.
    /// </summary>
    public static float GetFogFactorAt(Vector2 worldPos)
    {
        float maxFactor = 0f;
        for (int i = 0; i < _activeZones.Count; i++)
        {
            var zone = _activeZones[i];
            if (GodotObject.IsInstanceValid(zone) && zone.IsInsideTree() && zone.Visible)
            {
                float dist = worldPos.DistanceTo(zone.GlobalPosition);
                if (dist <= zone.ZoneRadius)
                {
                    float normDist = dist / zone.ZoneRadius;
                    float falloffStart = 1.0f - zone.RadialFalloff;
                    float edgeFade = normDist <= falloffStart ? 1.0f : Mathf.SmoothStep(1.0f, falloffStart, normDist);
                    float factor = Mathf.Clamp(edgeFade * zone.Density, 0f, 1f);
                    if (factor > maxFactor)
                    {
                        maxFactor = factor;
                    }
                }
            }
        }
        return maxFactor;
    }

    public static FogZone Instantiate(
        Vector2 position,
        float radius,
        Color color,
        float density = 0.60f,
        float coverage = 0.55f,
        float cycleSpeed = 0.04f,
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
