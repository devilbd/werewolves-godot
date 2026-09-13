using Godot;

namespace Werewolves.Core;

/// <summary>
/// Centralized management for game mouse cursors (Normal, Grab, Interaction).
/// Guarantees safe cursor resets and prevents cursors from getting stuck on collected or freed objects.
/// </summary>
public static class CursorManager
{
    private static Resource? _normalCursor;
    private static Resource? _grabCursor;
    private static Resource? _interactionCursor;

    public static Resource NormalCursor => _normalCursor ??= GD.Load<Resource>("res://assets/cursors/normal_o.png");
    public static Resource GrabCursor => _grabCursor ??= GD.Load<Resource>("res://assets/cursors/grab_o.png");
    public static Resource InteractionCursor => _interactionCursor ??= GD.Load<Resource>("res://assets/cursors/interaction_o.png");

    public static void SetGrab()
    {
        if (GrabCursor != null)
        {
            Input.SetCustomMouseCursor(GrabCursor, Input.CursorShape.Arrow, Vector2.Zero);
        }
    }

    public static void SetInteraction()
    {
        if (InteractionCursor != null)
        {
            Input.SetCustomMouseCursor(InteractionCursor, Input.CursorShape.Arrow, Vector2.Zero);
        }
    }

    public static void ResetNormal()
    {
        if (NormalCursor != null)
        {
            Input.SetCustomMouseCursor(NormalCursor, Input.CursorShape.Arrow, Vector2.Zero);
        }
    }

    /// <summary>
    /// Forces a cursor reset to normal immediately AND on the next frame (deferred)
    /// to guarantee that pending physics/picking events and freeing nodes do not override it.
    /// </summary>
    public static void ForceResetNormal()
    {
        ResetNormal();
        Callable.From(ResetNormal).CallDeferred();
    }
}
