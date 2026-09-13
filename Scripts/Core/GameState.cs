using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Entities;

namespace Werewolves.Core;

public partial class GameState : Node
{
    private static GameState? _instance;
    public static GameState Instance
    {
        get
        {
            if (_instance == null)
            {
                var tree = Engine.GetMainLoop() as SceneTree;
                var root = tree?.Root;
                var autoload = root?.GetNodeOrNull<GameState>("GameState");
                if (autoload != null)
                {
                    _instance = autoload;
                }
                else
                {
                    _instance = new GameState { Name = "GameState" };
                    root?.CallDeferred("add_child", _instance);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public static GameState GetInstance() => Instance;

    public const float WorldBoundRadius = 5000f;

    // Player stats
    public float PlayerHealth { get; private set; } = 100f;
    public float PlayerMaxHealth { get; private set; } = 100f;
    public float PlayerPower { get; private set; } = 100f;
    public float PlayerMaxPower { get; private set; } = 100f;
    public float PowerRegenRate { get; set; } = 0.20f;
    public float HealthRegenRate { get; set; } = 0.10f;

    public float BaseDamage { get; set; } = 25f;
    public float BaseDefense { get; set; } = 10f;
    public float Speed { get; set; } = 260f;
    public float Accuracy { get; set; } = 0.8f;
    public float Evasion { get; set; } = 0.1f;

    // Buff state
    public bool IsBuffed { get; private set; } = false;
    public float BuffTimeRemaining { get; private set; } = 0f;
    public const float BuffDuration = 10f;

    // Skill cooldowns (0: Scratch [5s], 1: Charge [8s], 2: Bite [10s], 3: Howl [30s])
    public float[] SkillCooldownRemaining { get; } = new float[4];
    public float[] SkillCooldownTotal { get; } = new float[] { 5.0f, 8.0f, 10.0f, 30.0f };

    // World state
    public bool IsInLair { get; set; } = false;

    // Blood Core state (in the center of the cave)
    public float BloodCoreReserves { get; set; } = 1000f;
    public float BloodCoreMaxReserves { get; set; } = 1000f;
    public event Action<float, float>? OnBloodCoreReservesChanged;

    // Fog state
    public bool IsPlayerInFog { get; private set; } = false;
    public float PlayerFogFactor { get; private set; } = 0f;

    // World position (continuous open world coordinates)
    private Vector2 _playerPosition = Vector2.Zero;
    public Vector2 PlayerPosition
    {
        get => _playerPosition;
        set
        {
            if (_playerPosition != value)
            {
                _playerPosition = value;
                SafeInvoke(OnPositionChanged, _playerPosition);
            }
        }
    }

    // Inventory
    public Dictionary<string, PouchItemData> PouchItems { get; } = new();

    public int GetPouchItemCount(string itemName)
    {
        return PouchItems.TryGetValue(itemName, out var item) ? item.Count : 0;
    }

    // Selected Target
    private Node2D? _selectedTarget;
    public Node2D? SelectedTarget
    {
        get => _selectedTarget;
        set
        {
            if (_selectedTarget != value)
            {
                if (GodotObject.IsInstanceValid(_selectedTarget) && _selectedTarget is ISelectableTarget oldTarget)
                {
                    oldTarget.OnDeselected();
                }

                _selectedTarget = value;

                if (GodotObject.IsInstanceValid(_selectedTarget) && _selectedTarget is ISelectableTarget newTarget)
                {
                    newTarget.OnSelected();
                }

                SafeInvoke(OnTargetChanged, _selectedTarget);
            }
        }
    }

    public string PlayerName { get; set; } = "Werewolf";
    public bool IsPouchOpen { get; private set; } = false;
    public bool IsHeroDetailsOpen { get; private set; } = false;
    public bool IsCraftingOpen { get; private set; } = false;
    public bool IsChestInventoryOpen { get; private set; } = false;
    public string? ActiveChestId { get; private set; } = null;
    public bool IsMapOpen { get; private set; } = false;
    public bool IsLootLabelsVisible { get; private set; } = false;

    // Chest Placement State
    public bool IsPlacingChest { get; private set; } = false;
    public string? ActivePlacementChestId { get; private set; } = null;

    // Cave Workshop & Chests Collections
    public HashSet<string> CraftedStaticObjects { get; } = new();
    public Dictionary<string, CaveChestData> CaveChests { get; } = new();

    // Events
    public event Action<float, float>? OnHealthChanged;
    public event Action<float, float>? OnPowerChanged;
    public event Action<Vector2>? OnPositionChanged;
    public event Action? OnPouchChanged;
    public event Action<Node2D?>? OnTargetChanged;
    public event Action<int, float, float>? OnCooldownUpdated;
    public event Action<bool>? OnPouchToggled;
    public event Action<bool>? OnHeroDetailsToggled;
    public event Action<bool>? OnCraftingToggled;
    public event Action<bool, string?>? OnChestInventoryToggled;
    public event Action<string>? OnChestInventoryChanged;
    public event Action<string, Vector2>? OnChestPlaced;
    public event Action<string>? OnStaticObjectCrafted;
    public event Action<bool, string?>? OnChestPlacementModeChanged;
    public event Action<bool>? OnMapToggled;
    public event Action<bool>? OnLootLabelsToggled;
    public event Action<string, Vector2, Color>? OnSpawnDamageNumber;
    public event Action<bool>? OnPlayerInFogChanged;

    public override void _EnterTree()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        ConfigManager.LoadAll();
        InitStatsFromConfig();
        SaveManager.LoadGame();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest || what == NotificationPredelete)
        {
            SaveCurrentState();
        }
    }

    public void SaveCurrentState()
    {
        SaveManager.LoadedPlayerPosition = PlayerPosition;
        SaveManager.SaveGame();
    }

    public void InitStatsFromConfig()
    {
        var p = ConfigManager.Combat.Player;
        if (!string.IsNullOrWhiteSpace(p.Name))
        {
            PlayerName = p.Name;
        }
        PlayerMaxHealth = p.MaxHealth;
        PlayerHealth = p.MaxHealth;
        PlayerMaxPower = p.MaxPower;
        PlayerPower = p.MaxPower;
        HealthRegenRate = p.HealthRegenRate;
        PowerRegenRate = p.PowerRegenRate;
        BaseDamage = p.BaseDamage;
        BaseDefense = p.BaseDefense;
        Speed = p.Speed;
        Accuracy = p.Accuracy;
        Evasion = p.Evasion;

        var s = ConfigManager.Combat.Skills;
        SkillCooldownTotal[0] = s.Scratch.Cooldown;
        SkillCooldownTotal[1] = s.Charge.Cooldown;
        SkillCooldownTotal[2] = s.Bite.Cooldown;
        SkillCooldownTotal[3] = s.Howl.Cooldown;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Process skill cooldown timers
        for (int i = 0; i < SkillCooldownRemaining.Length; i++)
        {
            if (SkillCooldownRemaining[i] > 0f)
            {
                SkillCooldownRemaining[i] = Math.Max(0f, SkillCooldownRemaining[i] - dt);
                SafeInvoke(OnCooldownUpdated, i, SkillCooldownRemaining[i], SkillCooldownTotal[i]);
            }
        }

        // Process buff timer
        if (IsBuffed)
        {
            BuffTimeRemaining -= dt;
            if (BuffTimeRemaining <= 0f)
            {
                RemoveBuff();
            }
        }

        // Process passive health and power regeneration over time (suppressed inside the cave)
        if (!IsInLair)
        {
            if (PlayerHealth > 0f && PlayerHealth < PlayerMaxHealth)
            {
                float oldHealth = PlayerHealth;
                PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + HealthRegenRate * dt);
                if (PlayerHealth != oldHealth)
                {
                    SafeInvoke(OnHealthChanged, PlayerHealth, PlayerMaxHealth);
                }
            }

            if (PlayerPower < PlayerMaxPower)
            {
                float oldPower = PlayerPower;
                PlayerPower = Mathf.Min(PlayerMaxPower, PlayerPower + PowerRegenRate * dt);
                if (PlayerPower != oldPower)
                {
                    SafeInvoke(OnPowerChanged, PlayerPower, PlayerMaxPower);
                }
            }
        }

        // Validate selected target
        if (_selectedTarget != null)
        {
            if (!GodotObject.IsInstanceValid(_selectedTarget) || _selectedTarget.IsQueuedForDeletion() || (_selectedTarget is ISelectableTarget sel && sel.IsDead))
            {
                SelectedTarget = null;
            }
        }

        // Process ambient fog detection
        float currentFog = Werewolves.World.FogZone.GetFogFactorAt(PlayerPosition);
        bool inFog = currentFog > 0.05f;
        if (inFog != IsPlayerInFog)
        {
            IsPlayerInFog = inFog;
            SafeInvoke(OnPlayerInFogChanged, inFog);
        }
        PlayerFogFactor = currentFog;
    }

    public void ModifyHealth(float delta)
    {
        PlayerHealth = Mathf.Clamp(PlayerHealth + delta, 0f, PlayerMaxHealth);
        SafeInvoke(OnHealthChanged, PlayerHealth, PlayerMaxHealth);
    }

    public void ModifyPower(float delta)
    {
        PlayerPower = Mathf.Clamp(PlayerPower + delta, 0f, PlayerMaxPower);
        SafeInvoke(OnPowerChanged, PlayerPower, PlayerMaxPower);
    }

    public void StartSkillCooldown(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < SkillCooldownRemaining.Length)
        {
            SkillCooldownRemaining[skillIndex] = SkillCooldownTotal[skillIndex];
            SafeInvoke(OnCooldownUpdated, skillIndex, SkillCooldownRemaining[skillIndex], SkillCooldownTotal[skillIndex]);
        }
    }

    public bool IsSkillOnCooldown(int skillIndex)
    {
        return skillIndex >= 0 && skillIndex < SkillCooldownRemaining.Length && SkillCooldownRemaining[skillIndex] > 0f;
    }

    public void ApplyHowlBuff()
    {
        if (IsBuffed) return;

        var howl = ConfigManager.Combat.Skills.Howl;
        IsBuffed = true;
        BuffTimeRemaining = howl.Duration;
        float mult = howl.StatMultiplier;
        BaseDamage *= mult;
        BaseDefense *= mult;
        Speed *= mult;
        Accuracy *= mult;
        Evasion *= mult;
    }

    private void RemoveBuff()
    {
        if (!IsBuffed) return;

        var howl = ConfigManager.Combat.Skills.Howl;
        IsBuffed = false;
        BuffTimeRemaining = 0f;
        float mult = howl.StatMultiplier;
        BaseDamage /= mult;
        BaseDefense /= mult;
        Speed /= mult;
        Accuracy /= mult;
        Evasion /= mult;
    }

    public void SetPlayerPosition(Vector2 pos)
    {
        PlayerPosition = pos;
        SaveManager.SaveGame();
    }

    public void AddPouchItem(string itemName, int count = 1)
    {
        if (!PouchItems.TryGetValue(itemName, out var item))
        {
            item = new PouchItemData { Count = 0, PosX = 0f, PosY = 0f };
            PouchItems[itemName] = item;
        }

        item.Count += count;
        SafeInvoke(OnPouchChanged);
        SaveManager.SaveGame();
    }

    public void UpdatePouchItemPosition(string itemName, Vector2 pos)
    {
        if (PouchItems.TryGetValue(itemName, out var item))
        {
            item.PosX = pos.X;
            item.PosY = pos.Y;
            SaveManager.SaveGame();
        }
    }

    public bool CanCollectBlood()
    {
        if (PouchItems.TryGetValue("EmptyFlask", out var emptyFlask) && emptyFlask.Count > 0)
            return true;

        foreach (var kvp in PouchItems)
        {
            if (kvp.Key.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase) && kvp.Value.BloodPercent < 100)
                return true;
        }

        return false;
    }

    public bool TryCollectBlood(out int filledPercent, out int currentTotal, out bool isNewFlask)
    {
        filledPercent = 0;
        currentTotal = 0;
        isNewFlask = false;

        // 1. Check if there is an existing non-full blood flask in the pouch (< 100%)
        string? candidateKey = null;
        PouchItemData? candidateFlask = null;

        foreach (var kvp in PouchItems)
        {
            if (kvp.Key.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase) && kvp.Value.BloodPercent < 100)
            {
                if (candidateFlask == null || kvp.Value.BloodPercent > candidateFlask.BloodPercent)
                {
                    candidateKey = kvp.Key;
                    candidateFlask = kvp.Value;
                }
            }
        }

        int fillAmount = GD.RandRange(20, 30);

        if (candidateFlask != null && candidateKey != null)
        {
            candidateFlask.BloodPercent = Math.Min(100, candidateFlask.BloodPercent + fillAmount);
            filledPercent = fillAmount;
            currentTotal = candidateFlask.BloodPercent;
            isNewFlask = false;

            SafeInvoke(OnPouchChanged);
            SaveManager.SaveGame();
            return true;
        }

        // 2. Otherwise, check if player has empty flasks in stack
        if (PouchItems.TryGetValue("EmptyFlask", out var emptyFlask) && emptyFlask.Count > 0)
        {
            emptyFlask.Count--;
            if (emptyFlask.Count <= 0)
            {
                PouchItems.Remove("EmptyFlask");
            }

            string newKey = $"BloodFlask_{Guid.NewGuid():N}"[..18];
            var newFlask = new PouchItemData
            {
                Count = 1,
                BloodPercent = fillAmount,
                PosX = 0f,
                PosY = 0f
            };
            PouchItems[newKey] = newFlask;

            filledPercent = fillAmount;
            currentTotal = fillAmount;
            isNewFlask = true;

            SafeInvoke(OnPouchChanged);
            SaveManager.SaveGame();
            return true;
        }

        return false;
    }

    public void TogglePouch() => TogglePouch(null);
    public void TogglePouch(bool? force = null)
    {
        IsPouchOpen = force ?? !IsPouchOpen;
        SafeInvoke(OnPouchToggled, IsPouchOpen);
    }

    public void ToggleHeroDetails() => ToggleHeroDetails(null);
    public void ToggleHeroDetails(bool? force = null)
    {
        IsHeroDetailsOpen = force ?? !IsHeroDetailsOpen;
        SafeInvoke(OnHeroDetailsToggled, IsHeroDetailsOpen);
    }

    public void CloseHeroDetails()
    {
        if (IsHeroDetailsOpen)
        {
            IsHeroDetailsOpen = false;
            SafeInvoke(OnHeroDetailsToggled, false);
        }
    }

    public void ToggleCrafting() => ToggleCrafting(null);
    public void ToggleCrafting(bool? force = null)
    {
        IsCraftingOpen = force ?? !IsCraftingOpen;
        SafeInvoke(OnCraftingToggled, IsCraftingOpen);
    }

    public void OpenChestInventory(string chestId)
    {
        ActiveChestId = chestId;
        IsChestInventoryOpen = true;
        SafeInvoke(OnChestInventoryToggled, true, chestId);
        if (!IsPouchOpen)
        {
            TogglePouch(true);
        }
    }

    public void CloseChestInventory()
    {
        if (IsChestInventoryOpen)
        {
            IsChestInventoryOpen = false;
            string? id = ActiveChestId;
            ActiveChestId = null;
            SafeInvoke(OnChestInventoryToggled, false, id);
        }
    }

    public void ToggleMap() => ToggleMap(null);
    public void ToggleMap(bool? force = null)
    {
        IsMapOpen = force ?? !IsMapOpen;
        SafeInvoke(OnMapToggled, IsMapOpen);
    }

    public void CloseMap()
    {
        if (IsMapOpen)
        {
            IsMapOpen = false;
            SafeInvoke(OnMapToggled, false);
        }
    }

    public void SetLootLabelsVisible(bool visible)
    {
        if (IsLootLabelsVisible != visible)
        {
            IsLootLabelsVisible = visible;
            SafeInvoke(OnLootLabelsToggled, visible);
        }
    }

    public void TriggerDamageNumber(string text, Vector2 position, Color color)
    {
        SafeInvoke(OnSpawnDamageNumber, text, position, color);
    }

    #region Crafting & Chest Management
    public static readonly Dictionary<string, CraftingRecipe> Recipes = new()
    {
        ["Chest"] = new CraftingRecipe
        {
            Id = "Chest",
            Name = "Storage Chest",
            Description = "A sturdy wooden chest for storing materials and pouch items securely.",
            IconPath = "res://assets/chests/chest_closed.png",
            Ingredients = new Dictionary<string, int> { ["Logs"] = 10, ["Stones"] = 5 },
            IsStatic = false,
            LocationDescription = "Freeform Placement in Cavern"
        },
        ["CraftingTable"] = new CraftingRecipe
        {
            Id = "CraftingTable",
            Name = "Crafting Table",
            Description = "Subterranean workbench used to fashion advanced tools and equipment.",
            IconPath = "res://assets/cave-objects/crafting-table.png",
            Ingredients = new Dictionary<string, int> { ["Logs"] = 25, ["Stones"] = 15 },
            IsStatic = true,
            StaticPosition = new Vector2(-700f, -520f),
            LocationDescription = "Fixed Position: Top Left"
        },
        ["BloodJuicer"] = new CraftingRecipe
        {
            Id = "BloodJuicer",
            Name = "Blood Juicer",
            Description = "Refines raw blood and organic remnants into concentrated life fluids.",
            IconPath = "res://assets/cave-objects/blood-juicer.png",
            Ingredients = new Dictionary<string, int> { ["Stones"] = 30, ["Quartz"] = 20 },
            BloodCost = 100f,
            IsStatic = true,
            StaticPosition = new Vector2(0f, -520f),
            LocationDescription = "Fixed Position: Top Center"
        },
        ["Laboratory"] = new CraftingRecipe
        {
            Id = "Laboratory",
            Name = "Alchemical Laboratory",
            Description = "Distillation apparatus for alchemical experiments and potent concoctions.",
            IconPath = "res://assets/cave-objects/laboratory.png",
            Ingredients = new Dictionary<string, int> { ["Stones"] = 25, ["Quartz"] = 25, ["Grass"] = 15 },
            IsStatic = true,
            StaticPosition = new Vector2(700f, -520f),
            LocationDescription = "Fixed Position: Top Right"
        }
    };

    public bool RemovePouchItem(string itemName, int count = 1)
    {
        if (PouchItems.TryGetValue(itemName, out var item) && item.Count >= count)
        {
            item.Count -= count;
            if (item.Count <= 0)
            {
                PouchItems.Remove(itemName);
            }
            SafeInvoke(OnPouchChanged);
            SaveManager.SaveGame();
            return true;
        }
        return false;
    }

    public bool CanCraft(string recipeId)
    {
        if (!Recipes.TryGetValue(recipeId, out var recipe)) return false;
        if (recipe.IsStatic && CraftedStaticObjects.Contains(recipeId)) return false;
        if (recipe.BloodCost > 0f && BloodCoreReserves < recipe.BloodCost) return false;

        foreach (var kvp in recipe.Ingredients)
        {
            int owned = 0;
            if (PouchItems.TryGetValue(kvp.Key, out var item))
            {
                owned = item.Count;
            }
            if (owned < kvp.Value) return false;
        }
        return true;
    }

    public bool CraftStaticObject(string recipeId)
    {
        if (!Recipes.TryGetValue(recipeId, out var recipe) || !recipe.IsStatic) return false;
        if (!CanCraft(recipeId)) return false;

        foreach (var kvp in recipe.Ingredients)
        {
            RemovePouchItem(kvp.Key, kvp.Value);
        }
        if (recipe.BloodCost > 0f)
        {
            TryDrainBloodCore(recipe.BloodCost);
        }

        CraftedStaticObjects.Add(recipeId);
        SafeInvoke(OnStaticObjectCrafted, recipeId);
        SaveManager.SaveGame();
        TriggerDamageNumber($"{recipe.Name} Built!", PlayerPosition + new Vector2(0, -60), new Color(0.4f, 1f, 0.4f));
        return true;
    }

    public void StartChestPlacement(string? existingChestId = null)
    {
        if (existingChestId == null && !CanCraft("Chest"))
        {
            TriggerDamageNumber("Not enough materials!", PlayerPosition + new Vector2(0, -60), new Color(1f, 0.4f, 0.4f));
            return;
        }

        IsPlacingChest = true;
        ActivePlacementChestId = existingChestId;

        if (IsCraftingOpen) ToggleCrafting(false);
        if (IsChestInventoryOpen) CloseChestInventory();

        SafeInvoke(OnChestPlacementModeChanged, true, existingChestId);
    }

    public void CancelChestPlacement()
    {
        IsPlacingChest = false;
        string? id = ActivePlacementChestId;
        ActivePlacementChestId = null;
        SafeInvoke(OnChestPlacementModeChanged, false, id);
    }

    public bool ConfirmChestPlacement(Vector2 worldPos)
    {
        if (!IsPlacingChest) return false;

        if (ActivePlacementChestId == null)
        {
            if (!CanCraft("Chest")) return false;
            var chestRecipe = Recipes["Chest"];
            foreach (var kvp in chestRecipe.Ingredients)
            {
                RemovePouchItem(kvp.Key, kvp.Value);
            }

            string newId = $"chest_{Guid.NewGuid():N}";
            var newChest = new CaveChestData
            {
                Id = newId,
                PosX = worldPos.X,
                PosY = worldPos.Y,
                Items = new Dictionary<string, PouchItemData>()
            };
            CaveChests[newId] = newChest;

            IsPlacingChest = false;
            ActivePlacementChestId = null;
            SafeInvoke(OnChestPlacementModeChanged, false, null);
            SafeInvoke(OnChestPlaced, newId, worldPos);
            SaveManager.SaveGame();
            TriggerDamageNumber("Chest Built!", worldPos + new Vector2(0, -40), new Color(0.4f, 1f, 0.4f));
            return true;
        }
        else
        {
            if (CaveChests.TryGetValue(ActivePlacementChestId, out var existing))
            {
                existing.PosX = worldPos.X;
                existing.PosY = worldPos.Y;
                string chestId = ActivePlacementChestId;

                IsPlacingChest = false;
                ActivePlacementChestId = null;
                SafeInvoke(OnChestPlacementModeChanged, false, null);
                SafeInvoke(OnChestPlaced, chestId, worldPos);
                SaveManager.SaveGame();
                TriggerDamageNumber("Chest Relocated!", worldPos + new Vector2(0, -40), new Color(0.4f, 1f, 0.4f));
                return true;
            }
        }
        return false;
    }

    public void UpdateChestItemPosition(string chestId, string itemName, Vector2 pos)
    {
        if (CaveChests.TryGetValue(chestId, out var chest))
        {
            if (chest.Items.TryGetValue(itemName, out var item))
            {
                item.PosX = pos.X;
                item.PosY = pos.Y;
                SaveManager.SaveGame();
            }
        }
    }

    public void TransferItemPouchToChest(string chestId, string itemKey, int count = 1)
    {
        if (!CaveChests.TryGetValue(chestId, out var chest)) return;
        if (!PouchItems.TryGetValue(itemKey, out var pouchItem) || pouchItem.Count <= 0) return;

        bool isFlask = itemKey.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase);

        if (isFlask)
        {
            chest.Items[itemKey] = new PouchItemData
            {
                Count = 1,
                BloodPercent = pouchItem.BloodPercent,
                PosX = 0f,
                PosY = 0f
            };
            PouchItems.Remove(itemKey);
        }
        else
        {
            int toTransfer = Math.Min(count, pouchItem.Count);
            if (chest.Items.TryGetValue(itemKey, out var chestItem))
            {
                chestItem.Count += toTransfer;
            }
            else
            {
                chest.Items[itemKey] = new PouchItemData
                {
                    Count = toTransfer,
                    BloodPercent = 0,
                    PosX = 0f,
                    PosY = 0f
                };
            }

            pouchItem.Count -= toTransfer;
            if (pouchItem.Count <= 0)
            {
                PouchItems.Remove(itemKey);
            }
        }

        SafeInvoke(OnPouchChanged);
        SafeInvoke(OnChestInventoryChanged, chestId);
        SaveManager.SaveGame();
    }

    public void TransferItemChestToPouch(string chestId, string itemKey, int count = 1)
    {
        if (!CaveChests.TryGetValue(chestId, out var chest)) return;
        if (!chest.Items.TryGetValue(itemKey, out var chestItem) || chestItem.Count <= 0) return;

        bool isFlask = itemKey.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase);

        if (isFlask)
        {
            PouchItems[itemKey] = new PouchItemData
            {
                Count = 1,
                BloodPercent = chestItem.BloodPercent,
                PosX = 0f,
                PosY = 0f
            };
            chest.Items.Remove(itemKey);
        }
        else
        {
            int toTransfer = Math.Min(count, chestItem.Count);
            AddPouchItem(itemKey, toTransfer);

            chestItem.Count -= toTransfer;
            if (chestItem.Count <= 0)
            {
                chest.Items.Remove(itemKey);
            }
        }

        SafeInvoke(OnPouchChanged);
        SafeInvoke(OnChestInventoryChanged, chestId);
        SaveManager.SaveGame();
    }

    public void TransferAllPouchToChest(string chestId)
    {
        if (!CaveChests.TryGetValue(chestId, out var chest)) return;
        var keys = new List<string>(PouchItems.Keys);
        foreach (var key in keys)
        {
            if (PouchItems.TryGetValue(key, out var item))
            {
                TransferItemPouchToChest(chestId, key, item.Count);
            }
        }
    }

    public void TransferAllChestToPouch(string chestId)
    {
        if (!CaveChests.TryGetValue(chestId, out var chest)) return;
        var keys = new List<string>(chest.Items.Keys);
        foreach (var key in keys)
        {
            if (chest.Items.TryGetValue(key, out var item))
            {
                TransferItemChestToPouch(chestId, key, item.Count);
            }
        }
    }
    #endregion

    #region Blood Core Methods
    public void SetBloodCoreReserves(float amount)
    {
        BloodCoreReserves = Mathf.Clamp(amount, 0f, BloodCoreMaxReserves);
        SafeInvoke(OnBloodCoreReservesChanged, BloodCoreReserves, BloodCoreMaxReserves);
        SaveManager.SaveGame();
    }

    public bool TryDrainBloodCore(float amount)
    {
        if (BloodCoreReserves < amount) return false;
        BloodCoreReserves -= amount;
        SafeInvoke(OnBloodCoreReservesChanged, BloodCoreReserves, BloodCoreMaxReserves);
        SaveManager.SaveGame();
        return true;
    }

    public float AddBloodCoreReserves(float amount)
    {
        float previous = BloodCoreReserves;
        BloodCoreReserves = Mathf.Min(BloodCoreMaxReserves, BloodCoreReserves + amount);
        float added = BloodCoreReserves - previous;
        if (added > 0f)
        {
            SafeInvoke(OnBloodCoreReservesChanged, BloodCoreReserves, BloodCoreMaxReserves);
            SaveManager.SaveGame();
        }
        return added;
    }
    #endregion

    #region Item Consumption Methods
    public bool EatMeat()
    {
        if (!PouchItems.TryGetValue("Meat", out var meat) || meat.Count <= 0)
        {
            return false;
        }

        meat.Count--;
        if (meat.Count <= 0)
        {
            PouchItems.Remove("Meat");
        }

        ModifyHealth(20f);
        ModifyPower(10f);

        TriggerDamageNumber("+20 HP  +10 Power", PlayerPosition + new Vector2(0, -75), new Color(0.4f, 0.95f, 0.45f));
        SafeInvoke(OnPouchChanged);
        SaveManager.SaveGame();
        return true;
    }

    public bool DrinkBloodFlask(string flaskKey)
    {
        if (!PouchItems.TryGetValue(flaskKey, out var flask) || flask.Count <= 0)
        {
            return false;
        }

        int bloodPercent = flask.BloodPercent;
        if (bloodPercent <= 0)
        {
            return false;
        }

        PouchItems.Remove(flaskKey);

        if (PouchItems.TryGetValue("EmptyFlask", out var empty))
        {
            empty.Count++;
        }
        else
        {
            PouchItems["EmptyFlask"] = new PouchItemData
            {
                Count = 1,
                BloodPercent = 0,
                PosX = flask.PosX,
                PosY = flask.PosY
            };
        }

        float ratio = bloodPercent / 100f;
        float healHp = Mathf.Round(25f * ratio);
        float healPower = Mathf.Round(25f * ratio);

        ModifyHealth(healHp);
        ModifyPower(healPower);

        TriggerDamageNumber($"+{healHp:0} HP  +{healPower:0} Power", PlayerPosition + new Vector2(0, -75), new Color(0.95f, 0.35f, 0.45f));
        SafeInvoke(OnPouchChanged);
        SaveManager.SaveGame();
        return true;
    }
    #endregion

    #region Safe Delegate Invocations
    private static void SafeInvoke(Action? action)
    {
        if (action == null) return;
        foreach (Action handler in action.GetInvocationList())
        {
            try
            {
                if (handler.Target is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
                    continue;
                handler();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private static void SafeInvoke<T>(Action<T>? action, T arg)
    {
        if (action == null) return;
        foreach (Action<T> handler in action.GetInvocationList())
        {
            try
            {
                if (handler.Target is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
                    continue;
                handler(arg);
            }
            catch (ObjectDisposedException) { }
        }
    }

    private static void SafeInvoke<T1, T2>(Action<T1, T2>? action, T1 arg1, T2 arg2)
    {
        if (action == null) return;
        foreach (Action<T1, T2> handler in action.GetInvocationList())
        {
            try
            {
                if (handler.Target is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
                    continue;
                handler(arg1, arg2);
            }
            catch (ObjectDisposedException) { }
        }
    }

    private static void SafeInvoke<T1, T2, T3>(Action<T1, T2, T3>? action, T1 arg1, T2 arg2, T3 arg3)
    {
        if (action == null) return;
        foreach (Action<T1, T2, T3> handler in action.GetInvocationList())
        {
            try
            {
                if (handler.Target is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
                    continue;
                handler(arg1, arg2, arg3);
            }
            catch (ObjectDisposedException) { }
        }
    }
    #endregion
}
