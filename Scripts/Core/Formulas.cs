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
        float bonus = ConfigManager.Combat.Skills.Scratch.BonusDamage;
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + bonus;
        return Math.Max(0f, damageDealt);
    }

    public static float CalculateChargeAttackDamage(ICombatant dealer, ICombatant target)
    {
        float bonus = ConfigManager.Combat.Skills.Charge.BonusDamage;
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + bonus;
        return Math.Max(0f, damageDealt);
    }

    public static float CalculateBiteDamage(ICombatant dealer, ICombatant target)
    {
        float bonus = ConfigManager.Combat.Skills.Bite.BonusDamage;
        float damageDealt = dealer.BaseDamage - target.BaseDefense / 2.0f + bonus;
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

    public struct VillagerLootResult
    {
        public int GoldCoins;
        public int Meat;
    }

    /// <summary>
    /// Version 2.0+ Villager loot roll:
    /// Drops Gold, Meat, or Both based on random principle.
    /// Supports Small Loot vs More/Large Loot tiers.
    /// </summary>
    public static VillagerLootResult RollVillagerLoot()
    {
        float catRoll = GD.Randf();
        bool dropGold = catRoll < 0.70f;   // 40% Gold only (0..0.40), 30% Both (0.40..0.70)
        bool dropMeat = catRoll >= 0.40f;  // 30% Meat only (0.70..1.00), 30% Both (0.40..0.70)

        float tierRoll = GD.Randf();
        bool isMoreLoot = tierRoll >= 0.65f; // 35% chance for more loot, 65% for small loot

        int gold = 0;
        if (dropGold)
        {
            gold = isMoreLoot ? GD.RandRange(6, 12) : GD.RandRange(2, 5);
        }

        int meat = 0;
        if (dropMeat)
        {
            meat = isMoreLoot ? GD.RandRange(2, 3) : 1;
        }

        return new VillagerLootResult { GoldCoins = gold, Meat = meat };
    }
}

