using Godot;

namespace Werewolves.Entities;

/// <summary>
/// Defines bounds and color for rendering dashed borders when the player is inside fog clouds.
/// </summary>
public interface IFogBorderable
{
    Rect2 FogBounds { get; }
    Color FogBorderColor { get; }
}
