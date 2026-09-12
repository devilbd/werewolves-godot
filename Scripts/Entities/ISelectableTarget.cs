using Godot;

namespace Werewolves.Entities;

public interface ISelectableTarget
{
    string TargetName { get; }
    float Health { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
    Vector2 GlobalPosition { get; }
    void OnSelected();
    void OnDeselected();
    void Interact();
}
