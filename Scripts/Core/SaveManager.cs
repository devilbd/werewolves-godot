using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Werewolves.Core;

public static class SaveManager
{
    private const string SavePath = "user://werewolves_save.json";

    public class SaveData
    {
        [JsonPropertyName("mapX")] public int MapX { get; set; } = 0;
        [JsonPropertyName("mapY")] public int MapY { get; set; } = 0;
        [JsonPropertyName("playerX")] public float PlayerX { get; set; } = 0f;
        [JsonPropertyName("playerY")] public float PlayerY { get; set; } = 0f;
        [JsonPropertyName("isInLair")] public bool IsInLair { get; set; } = false;
        [JsonPropertyName("pouchItems")] public Dictionary<string, PouchItemData> PouchItems { get; set; } = new();
        [JsonPropertyName("bloodCoreReserves")] public float BloodCoreReserves { get; set; } = 1000f;
        [JsonPropertyName("craftedCaveObjects")] public List<string> CraftedCaveObjects { get; set; } = new();
        [JsonPropertyName("caveChests")] public List<CaveChestData> CaveChests { get; set; } = new();
    }

    public static Vector2 LoadedPlayerPosition { get; set; } = Vector2.Zero;

    public static void SaveGame()
    {
        if (GameState.Instance == null) return;

        try
        {
            var data = new SaveData
            {
                MapX = 0,
                MapY = 0,
                PlayerX = LoadedPlayerPosition.X,
                PlayerY = LoadedPlayerPosition.Y,
                IsInLair = GameState.Instance.IsInLair,
                PouchItems = GameState.Instance.PouchItems,
                BloodCoreReserves = GameState.Instance.BloodCoreReserves,
                CraftedCaveObjects = new List<string>(GameState.Instance.CraftedStaticObjects),
                CaveChests = new List<CaveChestData>(GameState.Instance.CaveChests.Values)
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
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
            if (!FileAccess.FileExists(SavePath))
            {
                return;
            }

            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            string json = file.GetAsText();
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data == null) return;

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

            GameState.Instance.CaveChests.Clear();
            if (data.CaveChests != null)
            {
                foreach (var chest in data.CaveChests)
                {
                    GameState.Instance.CaveChests[chest.Id] = chest;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load game: {ex.Message}");
        }
    }
}
