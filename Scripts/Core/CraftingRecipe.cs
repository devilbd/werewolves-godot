using System.Collections.Generic;
using Godot;

namespace Werewolves.Core;

public class CraftingRecipe
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconPath { get; set; } = "";
    public Dictionary<string, int> Ingredients { get; set; } = new();
    public float BloodCost { get; set; } = 0f;
    public bool IsStatic { get; set; } = false;
    public Vector2 StaticPosition { get; set; } = Vector2.Zero;
    public string LocationDescription { get; set; } = "";
    public bool IsItem { get; set; } = false;
    public string ResultItem { get; set; } = "";
    public int ResultCount { get; set; } = 1;
    public int ResultPowerPercent { get; set; } = 0;
    public string Station { get; set; } = "CraftingTable";
    public string Category { get; set; } = "Station";
}
