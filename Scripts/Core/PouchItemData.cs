using System.Text.Json.Serialization;

namespace Werewolves.Core;

public class PouchItemData
{
    [JsonPropertyName("count")]
    public int Count { get; set; } = 0;

    [JsonPropertyName("posX")]
    public float PosX { get; set; } = 0f;

    [JsonPropertyName("posY")]
    public float PosY { get; set; } = 0f;

    [JsonPropertyName("bloodPercent")]
    public int BloodPercent { get; set; } = 0;
}
