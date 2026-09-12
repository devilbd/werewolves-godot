using Godot;

namespace Werewolves.Core;

public interface ICombatant
{
    float BaseDamage { get; }
    float Accuracy { get; }
    float BaseDefense { get; }
    float Evasion { get; }
    float Health { get; set; }
    float MaxHealth { get; }
    bool IsDead { get; }
    Vector2 GlobalPosition { get; }

    void TakeDamage(float amount, bool isSkill = false);
}
