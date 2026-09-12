using Godot;

namespace Werewolves.Entities;

public interface ISelectableTarget
{
    string TargetName { get; }
    float Health { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
    Vector2 GlobalPosition { get; }
    Vector2 FloatingTextPosition { get; }
    void OnSelected();
    void OnDeselected();
    void Interact();
    void Vibrate(float intensity = 4f, float duration = 0.15f);
}
