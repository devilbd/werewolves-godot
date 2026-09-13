using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Werewolves.Core;

public static class SaveManager
{
    public const int CurrentSaveVersion = 3;

    public static string CustomSavePath { get; set; } = "";
    public static bool IsTestEnvironment { get; set; } = false;

    public static string ActiveSavePath =>
        IsTestEnvironment ? "user://werewolves_test_save.json" :
        (!string.IsNullOrEmpty(CustomSavePath) ? CustomSavePath : "user://werewolves_save.json");

    public class SaveData
    {
        [JsonPropertyName("saveVersion")] public int SaveVersion { get; set; } = 0;
        [JsonPropertyName("mapX")] public int MapX { get; set; } = 0;
        [JsonPropertyName("mapY")] public int MapY { get; set; } = 0;
        [JsonPropertyName("playerX")] public float PlayerX { get; set; } = 0f;
        [JsonPropertyName("playerY")] public float PlayerY { get; set; } = 0f;
        [JsonPropertyName("isInLair")] public bool IsInLair { get; set; } = false;
        [JsonPropertyName("pouchItems")] public Dictionary<string, PouchItemData> PouchItems { get; set; } = new();
        [JsonPropertyName("bloodCoreReserves")] public float BloodCoreReserves { get; set; } = 1000f;
        [JsonPropertyName("craftedCaveObjects")] public List<string> CraftedCaveObjects { get; set; } = new();
        [JsonPropertyName("destroyedCaveObjects")] public List<string> DestroyedCaveObjects { get; set; } = new();
        [JsonPropertyName("caveChests")] public List<CaveChestData> CaveChests { get; set; } = new();
        [JsonPropertyName("bloodJuicerMeats")] public int BloodJuicerMeats { get; set; } = 0;
    }

    public static Vector2 LoadedPlayerPosition { get; set; } = Vector2.Zero;

    public static void SaveGame()
    {
        if (GameState.Instance == null) return;

        try
        {
            var data = new SaveData
            {
                SaveVersion = CurrentSaveVersion,
                MapX = 0,
                MapY = 0,
                PlayerX = LoadedPlayerPosition.X,
                PlayerY = LoadedPlayerPosition.Y,
                IsInLair = GameState.Instance.IsInLair,
                PouchItems = GameState.Instance.PouchItems,
                BloodCoreReserves = GameState.Instance.BloodCoreReserves,
                CraftedCaveObjects = new List<string>(GameState.Instance.CraftedStaticObjects),
                DestroyedCaveObjects = new List<string>(GameState.Instance.DestroyedCaveObjects),
                CaveChests = new List<CaveChestData>(GameState.Instance.CaveChests.Values),
                BloodJuicerMeats = GameState.Instance.BloodJuicerMeats
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(ActiveSavePath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save game: {ex.Message}");
        }
    }

    public static void LoadGame()
    {
        try
        {
            if (!FileAccess.FileExists(ActiveSavePath))
            {
                return;
            }

            using var file = FileAccess.Open(ActiveSavePath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            string json = file.GetAsText();
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data == null) return;

            // Execute version migrations if the loaded save is from an older version
            bool migrated = MigrateSaveData(data, json);

            LoadedPlayerPosition = new Vector2(data.PlayerX, data.PlayerY);
            GameState.Instance.PlayerPosition = LoadedPlayerPosition;
            GameState.Instance.IsInLair = data.IsInLair;

            GameState.Instance.BloodCoreReserves = data.BloodCoreReserves;

            GameState.Instance.PouchItems.Clear();
            if (data.PouchItems != null)
            {
                foreach (var kvp in data.PouchItems)
                {
                    GameState.Instance.PouchItems[kvp.Key] = kvp.Value;
                }
            }

            GameState.Instance.CraftedStaticObjects.Clear();
            if (data.CraftedCaveObjects != null)
            {
                foreach (var obj in data.CraftedCaveObjects)
                {
                    GameState.Instance.CraftedStaticObjects.Add(obj);
                }
            }

            GameState.Instance.DestroyedCaveObjects.Clear();
            if (data.DestroyedCaveObjects != null)
            {
                foreach (var obj in data.DestroyedCaveObjects)
                {
                    GameState.Instance.DestroyedCaveObjects.Add(obj);
                }
            }

            GameState.Instance.CaveChests.Clear();
            if (data.CaveChests != null)
            {
                foreach (var chest in data.CaveChests)
                {
                    GameState.Instance.CaveChests[chest.Id] = chest;
                }
            }

            GameState.Instance.BloodJuicerMeats = data.BloodJuicerMeats;

            // If the save was migrated, immediately persist the updated schema to disk
            if (migrated)
            {
                SaveGame();
                GD.Print($"[SaveManager] Successfully saved migrated save (v{CurrentSaveVersion}) to disk.");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load game: {ex.Message}");
        }
    }

    public static void BackupSaveFile(string originalJson, int fromVersion)
    {
        try
        {
            if (IsTestEnvironment) return;

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string backupPath = $"user://werewolves_save.backup_v{fromVersion}_{timestamp}.json";
            using var file = FileAccess.Open(backupPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(originalJson);
                GD.Print($"[SaveManager] Created pre-migration backup at {backupPath}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveManager] Failed to create backup: {ex.Message}");
        }
    }

    public static bool MigrateSaveData(SaveData data, string originalJson)
    {
        int originalVersion = data.SaveVersion;
        bool needsRestore = originalVersion < 3 || (data.IsInLair && data.CraftedCaveObjects.Count == 0 && data.DestroyedCaveObjects.Count == 0 && !data.PouchItems.ContainsKey("Logs"));

        if (originalVersion >= CurrentSaveVersion && !needsRestore) return false;

        GD.Print($"[SaveManager] Migrating save data (original version: {originalVersion}, needsRestore: {needsRestore})...");
        BackupSaveFile(originalJson, originalVersion);

        // v0 -> v1: Basic collection safety and blood core defaults
        if (data.PouchItems == null) data.PouchItems = new Dictionary<string, PouchItemData>();
        if (data.CraftedCaveObjects == null) data.CraftedCaveObjects = new List<string>();
        if (data.DestroyedCaveObjects == null) data.DestroyedCaveObjects = new List<string>();
        if (data.CaveChests == null) data.CaveChests = new List<CaveChestData>();
        if (data.BloodCoreReserves <= 0f) data.BloodCoreReserves = 1000f;

        // v1 -> v2: Blood Juicer & Flask power percents
        foreach (var item in data.PouchItems.Values)
        {
            if (item.PowerPercent < 0) item.PowerPercent = 0;
        }

        // v2 -> v3: Cave Object Resource Restoration
        if (needsRestore)
        {
            RestoreWipedCaveObjectResources(data);
        }

        data.SaveVersion = CurrentSaveVersion;
        return true;
    }

    private static void AddResourceToSaveData(SaveData data, string itemName, int count)
    {
        if (count <= 0) return;
        if (data.PouchItems.TryGetValue(itemName, out var existing))
        {
            existing.Count += count;
        }
        else
        {
            int slotIdx = data.PouchItems.Count;
            int col = slotIdx % 7;
            int row = slotIdx / 7;
            data.PouchItems[itemName] = new PouchItemData
            {
                Count = count,
                PosX = 10 + col * 55,
                PosY = 15 + row * 55,
                BloodPercent = 0,
                PowerPercent = 0
            };
        }
    }

    private static void RestoreWipedCaveObjectResources(SaveData data)
    {
        GD.Print("[SaveManager] Restoring resources for wiped / destroyed cave objects...");

        // If player visited lair and has zero crafted objects, refund the resources for all 3 standard installations:
        // - Crafting Table: 25 Logs, 15 Stones
        // - Blood Juicer: 30 Stones, 20 Quartz, 100 Blood
        // - Laboratory: 25 Stones, 25 Quartz, 15 Grass
        // Total refund: 25 Logs, 70 Stones, 45 Quartz, 15 Grass, 100 Blood
        AddResourceToSaveData(data, "Logs", 25);
        AddResourceToSaveData(data, "Stones", 70);
        AddResourceToSaveData(data, "Quartz", 45);
        AddResourceToSaveData(data, "Grass", 15);
        data.BloodCoreReserves = MathF.Min(1000f, data.BloodCoreReserves + 100f);

        if (!data.DestroyedCaveObjects.Contains("CraftingTable")) data.DestroyedCaveObjects.Add("CraftingTable");
        if (!data.DestroyedCaveObjects.Contains("BloodJuicer")) data.DestroyedCaveObjects.Add("BloodJuicer");
        if (!data.DestroyedCaveObjects.Contains("Laboratory")) data.DestroyedCaveObjects.Add("Laboratory");

        GD.Print("[SaveManager] Restored: +25 Logs, +70 Stones, +45 Quartz, +15 Grass, +100 Blood into save data.");
    }
}
