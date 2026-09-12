using System;
using Godot;

namespace Werewolves.Core;

public static class Formulas
{
    public static float CalculateDamage(ICombatant dealer, ICombatant target)
    {
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f;
        return Math.Max(0f, damageDealt);
    }

    public static float CalculateScratchHitDamage(ICombatant dealer, ICombatant target)
    {
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + 12.0f;
        return Math.Max(0f, damageDealt);
    }

    public static float CalculateChargeAttackDamage(ICombatant dealer, ICombatant target)
    {
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + 20.0f;
        return Math.Max(0f, damageDealt);
    }

    public static float CalculateBiteDamage(ICombatant dealer, ICombatant target)
    {
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + 15.0f;
        return Math.Max(0f, damageDealt);
    }

    public static bool IsHitSuccessful(ICombatant dealer, ICombatant target)
    {
        float hitChance = dealer.Accuracy - target.Evasion;
        return GD.Randf() < hitChance;
    }

    [Obsolete("Power is now regenerated passively over time; hits strictly consume power.")]
    public static float CalculatePowerGain()
    {
        // Equivalent to: 12.5 * (Math.random() * 4 + 1) in web game (12.5 to 62.5 power)
        return 12.5f * ((float)GD.RandRange(0.0, 4.0) + 1.0f);
    }
}
