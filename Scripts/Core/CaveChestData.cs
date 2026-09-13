using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Werewolves.Core;

public class CaveChestData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("posX")]
    public float PosX { get; set; } = 0f;

    [JsonPropertyName("posY")]
    public float PosY { get; set; } = 0f;

    [JsonPropertyName("items")]
    public Dictionary<string, PouchItemData> Items { get; set; } = new();
}
