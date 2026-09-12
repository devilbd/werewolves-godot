using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.World;

public partial class WorldManager : Node2D
{
	[Export] public Werewolf Player { get; set; } = null!;

	// Known landmark positions in the open world
	public static readonly Vector2 AwakeningGrovePosition = Vector2.Zero;
	public static readonly Vector2 VillagePosition = new Vector2(2500f, -1800f);
	public static readonly Vector2 SilentLakePosition = new Vector2(-2000f, 2000f);
	public static readonly Vector2 MistyLakePosition = new Vector2(-2200f, -2200f);
	public static readonly Vector2 QuarryPosition = new Vector2(2200f, 2200f);
	public const float WorldRadius = 5000f;

	private TextureRect _groundBackground = null!;
	private Node2D _lakeContainer = null!;
	private Node2D _entitiesContainer = null!;

	private Texture2D _groundForestTex = null!;
	private Texture2D _groundVillageTex = null!;
	private readonly List<Texture2D> _lakeTextures = new();
	private readonly List<Rect2> _lakeBoundsList = new();

	public override void _Ready()
	{
		_groundForestTex = GD.Load<Texture2D>("res://assets/pine_tree_forest_ground_1.png");
		_groundVillageTex = GD.Load<Texture2D>("res://assets/houses/simple_path_cross_prim.png");
		_lakeTextures.Add(GD.Load<Texture2D>("res://assets/lake.png"));
		_lakeTextures.Add(GD.Load<Texture2D>("res://assets/lake_1.png"));

		// Look for existing background or create
		_groundBackground = GetNodeOrNull<TextureRect>("GroundBackground");
		if (_groundBackground == null)
		{
			_groundBackground = new TextureRect
			{
				Name = "GroundBackground",
				Texture = _groundForestTex,
				StretchMode = TextureRect.StretchModeEnum.Tile,
				TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
				ZIndex = -10
			};
			AddChild(_groundBackground);
		}
		else
		{
			_groundBackground.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
			_groundBackground.StretchMode = TextureRect.StretchModeEnum.Tile;
			_groundBackground.ZIndex = -10;
		}

		_lakeContainer = GetNodeOrNull<Node2D>("LakeContainer");
		if (_lakeContainer == null)
		{
			_lakeContainer = new Node2D { Name = "LakeContainer", ZIndex = -5 };
			AddChild(_lakeContainer);
		}

		_entitiesContainer = GetNodeOrNull<Node2D>("Entities");
		if (_entitiesContainer == null)
		{
			_entitiesContainer = new Node2D { Name = "Entities", YSortEnabled = true };
			AddChild(_entitiesContainer);
		}

		if (Player == null)
		{
			Player = _entitiesContainer.GetNodeOrNull<Werewolf>("Werewolf");
		}

		if (Player != null && Player.GetParent() != _entitiesContainer)
		{
			Player.GetParent()?.RemoveChild(Player);
			_entitiesContainer.AddChild(Player);
		}

		GameState.Instance.OnSpawnDamageNumber += (text, pos, color) =>
		{
			var dn = Effects.DamageNumber.Instantiate(text, pos, color);
			_entitiesContainer.AddChild(dn);
		};

		GenerateOpenWorld();
	}

	public override void _Process(double delta)
	{
		// Dynamically snap and tile the ground background around the camera/player
		Vector2 center = Player != null ? Player.GlobalPosition : Vector2.Zero;
		const float tileSize = 350f;
		Vector2 snapped = new Vector2(
			Mathf.Floor(center.X / tileSize) * tileSize,
			Mathf.Floor(center.Y / tileSize) * tileSize
		);

		Vector2 bgSize = new Vector2(3850f, 2800f);
		_groundBackground.GlobalPosition = snapped - bgSize / 2f;
		_groundBackground.Size = bgSize;
	}

	public static string GetRegionName(Vector2 pos)
	{
		if (pos.DistanceTo(VillagePosition) <= 650f) return "The Village";
		if (pos.DistanceTo(SilentLakePosition) <= 500f) return "Silent Lake";
		if (pos.DistanceTo(MistyLakePosition) <= 500f) return "Misty Lake";
		if (pos.DistanceTo(QuarryPosition) <= 500f) return "Quarry Hills";
		if (pos.DistanceTo(AwakeningGrovePosition) <= 400f) return "Awakening Grove";
		return "Wilderness";
	}

	public void GenerateOpenWorld()
	{
		_lakeBoundsList.Clear();

		// Clear previous entities (except player)
		foreach (Node child in _entitiesContainer.GetChildren())
		{
			if (child != Player)
			{
				child.QueueFree();
			}
		}

		foreach (Node child in _lakeContainer.GetChildren())
		{
			child.QueueFree();
		}

		// 1. Build Lakes
		BuildLake(SilentLakePosition, 0);
		BuildLake(MistyLakePosition, 1);

		// 2. Build Village
		BuildVillage(VillagePosition);

		// 3. Build Quarry Hills
		BuildQuarryHills(QuarryPosition);

		// 4. Build Deer Grazing Meadows
		BuildDeerHerds();

		// 5. Build Wilderness Trees & Rocks
		BuildWildernessVegetation();

		// 6. Build World Boundary Barriers
		BuildWorldBoundaries();
	}

	private void BuildLake(Vector2 center, int textureIndex)
	{
		if (_lakeTextures.Count == 0) return;

		var lakeTex = _lakeTextures[textureIndex % _lakeTextures.Count];
		Vector2 lakeSize = new Vector2(450f, 320f);
		Rect2 bounds = new Rect2(center - lakeSize / 2f, lakeSize);
		_lakeBoundsList.Add(bounds);

		var lakeSprite = new Sprite2D
		{
			Texture = lakeTex,
			GlobalPosition = center
		};
		_lakeContainer.AddChild(lakeSprite);

		var lakeBody = new StaticBody2D { GlobalPosition = center };
		var shape = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = lakeSize * 0.85f }
		};
		lakeBody.AddChild(shape);
		_lakeContainer.AddChild(lakeBody);
	}

	private void BuildVillage(Vector2 center)
	{
		// Village path ground texture
		var pathSprite = new Sprite2D
		{
			Texture = _groundVillageTex,
			GlobalPosition = center,
			ZIndex = -8
		};
		_lakeContainer.AddChild(pathSprite);

		float radius = 380f;
		int houseCount = 8;

		// Circular ring of 8 cottages
		for (int i = 0; i < houseCount; i++)
		{
			float angle = (i / (float)houseCount) * Mathf.Pi * 2.0f;
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			var house = HouseObject.Instantiate((i % 4) + 1, pos);
			_entitiesContainer.AddChild(house);
		}

		// Lantern in center
		var lantern = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://assets/houses/lantern_light.png"),
			GlobalPosition = center
		};
		_entitiesContainer.AddChild(lantern);

		// Outskirts trees
		for (int i = 0; i < 28; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2f);
			float dist = (float)GD.RandRange(radius + 80f, radius + 260f);
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			var tree = new TreeObject
			{
				GlobalPosition = pos,
				IsSelectable = GD.Randf() < 0.20f
			};
			_entitiesContainer.AddChild(tree);
		}
	}

	private void BuildQuarryHills(Vector2 center)
	{
		int rockCount = 14;
		for (int i = 0; i < rockCount; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2f);
			float dist = (float)GD.RandRange(30f, 320f);
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			var rock = new RockObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(rock);
		}

		// A few sparse trees around quarry perimeter
		for (int i = 0; i < 8; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2f);
			float dist = (float)GD.RandRange(280f, 400f);
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			var tree = new TreeObject { GlobalPosition = pos, IsSelectable = GD.Randf() < 0.15f };
			_entitiesContainer.AddChild(tree);
		}
	}

	private void BuildDeerHerds()
	{
		Vector2[] herdCenters =
		{
			new Vector2(0f, -2400f),
			new Vector2(-2400f, 0f),
			new Vector2(1500f, 400f),
			new Vector2(400f, 1800f),
			new Vector2(-800f, -1200f)
		};

		foreach (var herdCenter in herdCenters)
		{
			int count = GD.RandRange(2, 4);
			for (int i = 0; i < count; i++)
			{
				Vector2 offset = new Vector2((float)GD.RandRange(-150, 150), (float)GD.RandRange(-150, 150));
				Vector2 pos = herdCenter + offset;
				if (!IsInsideLake(pos, 50f))
				{
					var deer = new Deer
					{
						GlobalPosition = pos,
						HomePosition = pos,
						TargetWerewolf = Player
					};
					_entitiesContainer.AddChild(deer);
				}
			}
		}
	}

	private void BuildWildernessVegetation()
	{
		int treeCount = 450;
		for (int i = 0; i < treeCount; i++)
		{
			Vector2 pos = new Vector2(
				(float)GD.RandRange(-WorldRadius + 200f, WorldRadius - 200f),
				(float)GD.RandRange(-WorldRadius + 200f, WorldRadius - 200f)
			);

			// Avoid clearing areas
			if (IsInsideLake(pos, 60f)) continue;
			if (pos.DistanceTo(VillagePosition) < 550f) continue;
			if (pos.DistanceTo(QuarryPosition) < 360f) continue;
			if (pos.DistanceTo(AwakeningGrovePosition) < 160f) continue;

			var tree = new TreeObject
			{
				GlobalPosition = pos,
				IsSelectable = GD.Randf() < 0.15f
			};
			_entitiesContainer.AddChild(tree);
		}

		// Scattered rocks across the world
		int scatteredRockCount = 30;
		for (int i = 0; i < scatteredRockCount; i++)
		{
			Vector2 pos = new Vector2(
				(float)GD.RandRange(-WorldRadius + 300f, WorldRadius - 300f),
				(float)GD.RandRange(-WorldRadius + 300f, WorldRadius - 300f)
			);

			if (IsInsideLake(pos, 50f)) continue;
			if (pos.DistanceTo(VillagePosition) < 500f) continue;
			if (pos.DistanceTo(QuarryPosition) < 350f) continue;

			var rock = new RockObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(rock);
		}
	}

	private void BuildWorldBoundaries()
	{
		// Physical boundary collision walls at WorldRadius
		var worldWalls = new StaticBody2D { Name = "WorldWalls" };
		float extent = WorldRadius;
		float thickness = 200f;

		// Top wall
		AddWallShape(worldWalls, new Vector2(0, -extent - thickness / 2f), new Vector2(extent * 2f + thickness * 2f, thickness));
		// Bottom wall
		AddWallShape(worldWalls, new Vector2(0, extent + thickness / 2f), new Vector2(extent * 2f + thickness * 2f, thickness));
		// Left wall
		AddWallShape(worldWalls, new Vector2(-extent - thickness / 2f, 0), new Vector2(thickness, extent * 2f + thickness * 2f));
		// Right wall
		AddWallShape(worldWalls, new Vector2(extent + thickness / 2f, 0), new Vector2(thickness, extent * 2f + thickness * 2f));

		AddChild(worldWalls);

		// Dense tree border lining the perimeter
		int perimeterTreeCount = 120;
		for (int i = 0; i < perimeterTreeCount; i++)
		{
			float t = (i / (float)perimeterTreeCount) * (extent * 2f) - extent;
			// Top & Bottom edges
			_entitiesContainer.AddChild(new TreeObject { GlobalPosition = new Vector2(t, -extent + (float)GD.RandRange(-30, 30)) });
			_entitiesContainer.AddChild(new TreeObject { GlobalPosition = new Vector2(t, extent + (float)GD.RandRange(-30, 30)) });
			// Left & Right edges
			_entitiesContainer.AddChild(new TreeObject { GlobalPosition = new Vector2(-extent + (float)GD.RandRange(-30, 30), t) });
			_entitiesContainer.AddChild(new TreeObject { GlobalPosition = new Vector2(extent + (float)GD.RandRange(-30, 30), t) });
		}
	}

	private static void AddWallShape(StaticBody2D body, Vector2 pos, Vector2 size)
	{
		var col = new CollisionShape2D
		{
			Position = pos,
			Shape = new RectangleShape2D { Size = size }
		};
		body.AddChild(col);
	}

	private bool IsInsideLake(Vector2 pos, float margin)
	{
		foreach (var b in _lakeBoundsList)
		{
			if (b.Grow(margin).HasPoint(pos)) return true;
		}
		return false;
	}
}
