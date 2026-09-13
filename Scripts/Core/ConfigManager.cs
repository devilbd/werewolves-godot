using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Werewolves.Core;

public static class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private static ChestsConfig? _chests;
    public static ChestsConfig Chests => _chests ??= LoadConfig<ChestsConfig>("res://config/chests.json") ?? new ChestsConfig();

    private static ResourcesConfig? _resources;
    public static ResourcesConfig Resources => _resources ??= LoadConfig<ResourcesConfig>("res://config/resources.json") ?? new ResourcesConfig();

    private static CombatConfig? _combat;
    public static CombatConfig Combat => _combat ??= LoadConfig<CombatConfig>("res://config/combat.json") ?? new CombatConfig();

    public static void LoadAll()
    {
        _chests = LoadConfig<ChestsConfig>("res://config/chests.json") ?? new ChestsConfig();
        _resources = LoadConfig<ResourcesConfig>("res://config/resources.json") ?? new ResourcesConfig();
        _combat = LoadConfig<CombatConfig>("res://config/combat.json") ?? new CombatConfig();
    }

    public static void Reload()
    {
        LoadAll();
    }

    private static T? LoadConfig<T>(string path) where T : class
    {
        try
        {
            if (!FileAccess.FileExists(path))
            {
                GD.Print($"[ConfigManager] Config file not found at {path}, using defaults.");
                return null;
            }

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[ConfigManager] Failed to open {path}, using defaults.");
                return null;
            }

            string json = file.GetAsText();
            var result = JsonSerializer.Deserialize<T>(json, JsonOpts);
            if (result != null)
            {
                GD.Print($"[ConfigManager] Loaded configuration from {path}");
                return result;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ConfigManager] Error reading {path}: {ex.Message}. Falling back to defaults.");
        }

        return null;
    }
}

public class ChestsConfig
{
    [JsonPropertyName("spawning")] public ChestSpawningConfig Spawning { get; set; } = new();
    [JsonPropertyName("loot")] public ChestLootConfig Loot { get; set; } = new();
}

public class ChestSpawningConfig
{
    [JsonPropertyName("targetCount")] public int TargetCount { get; set; } = 5;
    [JsonPropertyName("minSeparationDistance")] public float MinSeparationDistance { get; set; } = 2500f;
    [JsonPropertyName("awakeningGroveBuffer")] public float AwakeningGroveBuffer { get; set; } = 900f;
    [JsonPropertyName("lairBuffer")] public float LairBuffer { get; set; } = 700f;
    [JsonPropertyName("villageBuffer")] public float VillageBuffer { get; set; } = 1400f;
    [JsonPropertyName("worldMargin")] public float WorldMargin { get; set; } = 500f;
    [JsonPropertyName("maxAttempts")] public int MaxAttempts { get; set; } = 1000;
}

public class ChestLootConfig
{
    [JsonPropertyName("flaskDropChance")] public float FlaskDropChance { get; set; } = 0.20f;
    [JsonPropertyName("flaskAmount")] public int FlaskAmount { get; set; } = 1;
    [JsonPropertyName("singleResourceTypeChance")] public float SingleResourceTypeChance { get; set; } = 0.65f;
    [JsonPropertyName("maxResourceTypes")] public int MaxResourceTypes { get; set; } = 2;
    [JsonPropertyName("resources")] public List<ChestResourceDrop> Resources { get; set; } = new()
    {
        new() { ItemType = "GoldCoins", MinAmount = 2, MaxAmount = 5 },
        new() { ItemType = "Quartz", MinAmount = 1, MaxAmount = 1 },
        new() { ItemType = "Logs", MinAmount = 1, MaxAmount = 2 },
        new() { ItemType = "Stones", MinAmount = 1, MaxAmount = 2 },
        new() { ItemType = "Meat", MinAmount = 1, MaxAmount = 2 }
    };
}

public class ChestResourceDrop
{
    [JsonPropertyName("itemType")] public string ItemType { get; set; } = "";
    [JsonPropertyName("minAmount")] public int MinAmount { get; set; } = 1;
    [JsonPropertyName("maxAmount")] public int MaxAmount { get; set; } = 2;
}

public class ResourcesConfig
{
    [JsonPropertyName("quartz")] public QuartzConfig Quartz { get; set; } = new();
}

public class QuartzConfig
{
    [JsonPropertyName("wildernessCount")] public int WildernessCount { get; set; } = 35;
    [JsonPropertyName("quarryClusterCount")] public int QuarryClusterCount { get; set; } = 6;
    [JsonPropertyName("variantWeightSmall")] public float VariantWeightSmall { get; set; } = 0.40f;
    [JsonPropertyName("variantWeightMedium")] public float VariantWeightMedium { get; set; } = 0.35f;
    [JsonPropertyName("variantWeightLarge")] public float VariantWeightLarge { get; set; } = 0.25f;
    [JsonPropertyName("dropVariant1SmallMin")] public int DropVariant1SmallMin { get; set; } = 1;
    [JsonPropertyName("dropVariant1SmallMax")] public int DropVariant1SmallMax { get; set; } = 1;
    [JsonPropertyName("dropVariant2MediumMin")] public int DropVariant2MediumMin { get; set; } = 1;
    [JsonPropertyName("dropVariant2MediumMax")] public int DropVariant2MediumMax { get; set; } = 2;
    [JsonPropertyName("dropVariant3LargeMin")] public int DropVariant3LargeMin { get; set; } = 2;
    [JsonPropertyName("dropVariant3LargeMax")] public int DropVariant3LargeMax { get; set; } = 3;
}

public class CombatConfig
{
    [JsonPropertyName("player")] public PlayerCombatConfig Player { get; set; } = new();
    [JsonPropertyName("skills")] public SkillsConfig Skills { get; set; } = new();
    [JsonPropertyName("enemies")] public EnemiesConfig Enemies { get; set; } = new();
    [JsonPropertyName("harvestables")] public HarvestablesConfig Harvestables { get; set; } = new();
}

public class PlayerCombatConfig
{
    [JsonPropertyName("maxHealth")] public float MaxHealth { get; set; } = 100f;
    [JsonPropertyName("maxPower")] public float MaxPower { get; set; } = 100f;
    [JsonPropertyName("healthRegenRate")] public float HealthRegenRate { get; set; } = 0.10f;
    [JsonPropertyName("powerRegenRate")] public float PowerRegenRate { get; set; } = 0.20f;
    [JsonPropertyName("baseDamage")] public float BaseDamage { get; set; } = 25f;
    [JsonPropertyName("baseDefense")] public float BaseDefense { get; set; } = 10f;
    [JsonPropertyName("speed")] public float Speed { get; set; } = 260f;
    [JsonPropertyName("accuracy")] public float Accuracy { get; set; } = 0.80f;
    [JsonPropertyName("evasion")] public float Evasion { get; set; } = 0.10f;
}

public class SkillsConfig
{
    [JsonPropertyName("scratch")] public ScratchSkillConfig Scratch { get; set; } = new();
    [JsonPropertyName("charge")] public ChargeSkillConfig Charge { get; set; } = new();
    [JsonPropertyName("bite")] public BiteSkillConfig Bite { get; set; } = new();
    [JsonPropertyName("howl")] public HowlSkillConfig Howl { get; set; } = new();
}

public class ScratchSkillConfig
{
    [JsonPropertyName("powerCost")] public float PowerCost { get; set; } = 15f;
    [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 5f;
    [JsonPropertyName("bonusDamage")] public float BonusDamage { get; set; } = 12f;
}

public class ChargeSkillConfig
{
    [JsonPropertyName("powerCost")] public float PowerCost { get; set; } = 25f;
    [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 8f;
    [JsonPropertyName("bonusDamage")] public float BonusDamage { get; set; } = 20f;
}

public class BiteSkillConfig
{
    [JsonPropertyName("powerCost")] public float PowerCost { get; set; } = 20f;
    [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 10f;
    [JsonPropertyName("bonusDamage")] public float BonusDamage { get; set; } = 15f;
    [JsonPropertyName("executeHpThreshold")] public float ExecuteHpThreshold { get; set; } = 0.25f;
    [JsonPropertyName("healAmount")] public float HealAmount { get; set; } = 20f;
}

public class HowlSkillConfig
{
    [JsonPropertyName("powerCost")] public float PowerCost { get; set; } = 40f;
    [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 30f;
    [JsonPropertyName("duration")] public float Duration { get; set; } = 10f;
    [JsonPropertyName("statMultiplier")] public float StatMultiplier { get; set; } = 1.30f;
}

public class EnemiesConfig
{
    [JsonPropertyName("deer")] public EnemyStatsConfig Deer { get; set; } = new()
    {
        MaxHealth = 80f,
        BaseDamage = 15f,
        BaseDefense = 5f,
        Speed = 70f,
        Accuracy = 0.75f,
        Evasion = 0.15f,
        AttackCooldown = 3.0f,
        AttackRange = 90f
    };

    [JsonPropertyName("villager")] public EnemyStatsConfig Villager { get; set; } = new()
    {
        MaxHealth = 80f,
        BaseDamage = 15f,
        BaseDefense = 5f,
        Speed = 65f,
        Accuracy = 0.75f,
        Evasion = 0.15f,
        AttackCooldown = 2.5f,
        AttackRange = 95f
    };
}

public class EnemyStatsConfig
{
    [JsonPropertyName("maxHealth")] public float MaxHealth { get; set; } = 80f;
    [JsonPropertyName("baseDamage")] public float BaseDamage { get; set; } = 15f;
    [JsonPropertyName("baseDefense")] public float BaseDefense { get; set; } = 5f;
    [JsonPropertyName("speed")] public float Speed { get; set; } = 70f;
    [JsonPropertyName("accuracy")] public float Accuracy { get; set; } = 0.75f;
    [JsonPropertyName("evasion")] public float Evasion { get; set; } = 0.15f;
    [JsonPropertyName("attackCooldown")] public float AttackCooldown { get; set; } = 3.0f;
    [JsonPropertyName("attackRange")] public float AttackRange { get; set; } = 90f;
}

public class HarvestablesConfig
{
    [JsonPropertyName("tree")] public HarvestableStatsConfig Tree { get; set; } = new() { MaxHealth = 100f, DamagePerHit = 25f };
    [JsonPropertyName("rock")] public HarvestableStatsConfig Rock { get; set; } = new() { MaxHealth = 100f, DamagePerHit = 25f };
    [JsonPropertyName("quartz")] public HarvestableStatsConfig Quartz { get; set; } = new() { MaxHealth = 100f, DamagePerHit = 25f };
}

public class HarvestableStatsConfig
{
    [JsonPropertyName("maxHealth")] public float MaxHealth { get; set; } = 100f;
    [JsonPropertyName("damagePerHit")] public float DamagePerHit { get; set; } = 25f;
}
