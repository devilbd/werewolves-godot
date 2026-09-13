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

    public bool IsPouchOpen { get; private set; } = false;

    // Events
    public event Action<float, float>? OnHealthChanged;
    public event Action<float, float>? OnPowerChanged;
    public event Action<Vector2>? OnPositionChanged;
    public event Action? OnPouchChanged;
    public event Action<Node2D?>? OnTargetChanged;
    public event Action<int, float, float>? OnCooldownUpdated;
    public event Action<bool>? OnPouchToggled;
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
        SaveManager.LoadGame();
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

        // Process passive health regeneration over time
        if (PlayerHealth > 0f && PlayerHealth < PlayerMaxHealth)
        {
            float oldHealth = PlayerHealth;
            PlayerHealth = Mathf.Min(PlayerMaxHealth, PlayerHealth + HealthRegenRate * dt);
            if (PlayerHealth != oldHealth)
            {
                SafeInvoke(OnHealthChanged, PlayerHealth, PlayerMaxHealth);
            }
        }

        // Process passive power regeneration over time
        if (PlayerPower < PlayerMaxPower)
        {
            float oldPower = PlayerPower;
            PlayerPower = Mathf.Min(PlayerMaxPower, PlayerPower + PowerRegenRate * dt);
            if (PlayerPower != oldPower)
            {
                SafeInvoke(OnPowerChanged, PlayerPower, PlayerMaxPower);
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

        IsBuffed = true;
        BuffTimeRemaining = BuffDuration;
        BaseDamage *= 1.3f;
        BaseDefense *= 1.3f;
        Speed *= 1.3f;
        Accuracy *= 1.3f;
        Evasion *= 1.3f;
    }

    private void RemoveBuff()
    {
        if (!IsBuffed) return;

        IsBuffed = false;
        BuffTimeRemaining = 0f;
        BaseDamage /= 1.3f;
        BaseDefense /= 1.3f;
        Speed /= 1.3f;
        Accuracy /= 1.3f;
        Evasion /= 1.3f;
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

    public void TogglePouch()
    {
        IsPouchOpen = !IsPouchOpen;
        SafeInvoke(OnPouchToggled, IsPouchOpen);
    }

    public void TriggerDamageNumber(string text, Vector2 position, Color color)
    {
        SafeInvoke(OnSpawnDamageNumber, text, position, color);
    }

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
