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
        [JsonPropertyName("pouchItems")] public Dictionary<string, PouchItemData> PouchItems { get; set; } = new();
    }

    public static Vector2 LoadedPlayerPosition { get; set; } = Vector2.Zero;

    public static void SaveGame()
    {
        try
        {
            var data = new SaveData
            {
                MapX = 0,
                MapY = 0,
                PlayerX = LoadedPlayerPosition.X,
                PlayerY = LoadedPlayerPosition.Y,
                PouchItems = GameState.Instance.PouchItems
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

            GameState.Instance.PouchItems.Clear();
            if (data.PouchItems != null)
            {
                foreach (var kvp in data.PouchItems)
                {
                    GameState.Instance.PouchItems[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load game: {ex.Message}");
        }
    }
}
