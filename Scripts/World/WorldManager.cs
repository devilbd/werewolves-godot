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
	public static readonly Vector2 LairEntrancePosition = new Vector2(-650f, -450f);
	public static readonly Vector2 VillagePosition = new Vector2(2500f, -1800f);
	public static readonly Vector2 SilentLakePosition = new Vector2(-2000f, 2000f);
	public static readonly Vector2 MistyLakePosition = new Vector2(-2200f, -2200f);
	public static readonly Vector2 QuarryPosition = new Vector2(2200f, 2200f);
	public const float WorldRadius = 5000f;

	// Coordinate transformation helpers between Godot world coordinates and map coordinates
	// Inverts Y so North (Up) is positive, and keeps X so East (Right) is positive (Cartesian standard)
	public static Vector2 ToMapCoordinates(Vector2 worldPos) => new Vector2(worldPos.X, -worldPos.Y);
	public static Vector2 ToWorldCoordinates(Vector2 mapPos) => new Vector2(mapPos.X, -mapPos.Y);

	public static Vector2 LairEntranceMapPosition => ToMapCoordinates(LairEntrancePosition);
	public static Vector2 VillageMapPosition => ToMapCoordinates(VillagePosition);
	public static Vector2 SilentLakeMapPosition => ToMapCoordinates(SilentLakePosition);
	public static Vector2 MistyLakeMapPosition => ToMapCoordinates(MistyLakePosition);
	public static Vector2 QuarryMapPosition => ToMapCoordinates(QuarryPosition);

	private TextureRect _groundBackground = null!;
	private Node2D _lakeContainer = null!;
	private Node2D _entitiesContainer = null!;
	private Node2D _fogContainer = null!;

	private Texture2D _groundForestTex = null!;
	private Texture2D _groundVillageTex = null!;
	private readonly List<Texture2D> _lakeTextures = new();
	private readonly List<Rect2> _lakeBoundsList = new();
	private Effects.TargetReticle _targetReticle = null!;
	private Effects.FogDashedBorderOverlay _fogDashedBorderOverlay = null!;

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

		_fogContainer = GetNodeOrNull<Node2D>("FogContainer");
		if (_fogContainer == null)
		{
			_fogContainer = new Node2D { Name = "FogContainer", ZIndex = 15 };
			AddChild(_fogContainer);
		}
		else
		{
			_fogContainer.ZIndex = 15;
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

		GameState.Instance.OnSpawnDamageNumber += OnGameStateSpawnDamageNumber;

		// Attach selection reticle
		_targetReticle = new Effects.TargetReticle { Name = "TargetReticle" };
		AddChild(_targetReticle);

		// Attach fog dashed border overlay (renders dashed borders when player is inside fog clouds)
		_fogDashedBorderOverlay = new Effects.FogDashedBorderOverlay { Name = "FogDashedBorderOverlay" };
		AddChild(_fogDashedBorderOverlay);

		GenerateOpenWorld();
	}

	private void OnGameStateSpawnDamageNumber(string text, Vector2 pos, Color color)
	{
		if (!GodotObject.IsInstanceValid(this) || _entitiesContainer == null || !GodotObject.IsInstanceValid(_entitiesContainer))
			return;

		var dn = Effects.DamageNumber.Instantiate(text, pos, color);
		_entitiesContainer.AddChild(dn);
	}

	public override void _ExitTree()
	{
		if (GameState.Instance != null)
		{
			GameState.Instance.OnSpawnDamageNumber -= OnGameStateSpawnDamageNumber;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			GameState.Instance.SelectedTarget = null;
		}
		else if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			GameState.Instance.SelectedTarget = null;
		}
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
		if (pos.DistanceTo(VillagePosition) <= 1450f) return "The Village";
		if (pos.DistanceTo(LairEntrancePosition) <= 450f) return "Werewolf's Lair";
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

		foreach (Node child in _fogContainer.GetChildren())
		{
			child.QueueFree();
		}

		// 1. Build Lakes
		BuildLake(SilentLakePosition, 0);
		BuildLake(MistyLakePosition, 1);

		// 2. Build Village
		BuildVillage(VillagePosition);
		BuildVillagers(VillagePosition);

		// 3. Build Quarry Hills
		BuildQuarryHills(QuarryPosition);

		// 4. Build Deer Grazing Meadows
		BuildDeerHerds();

		// 5. Build Werewolf's Lair Outside Landmark
		BuildLairEntrance(LairEntrancePosition);

		// 6. Build Wilderness Trees & Rocks
		BuildWildernessVegetation();

		// 7. Build Quartz Mineral Deposits
		BuildQuartzDeposits();

		// 8. Build World Boundary Barriers
		BuildWorldBoundaries();

		// 9. Build Atmospheric Fog Zones
		BuildFogZones();
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
		// 1. Village cobblestone path network (covering central crossroads and radiating streets)
		Vector2[] pathOffsets =
		{
			Vector2.Zero,
			new Vector2(0f, -650f),
			new Vector2(0f, 650f),
			new Vector2(650f, 0f),
			new Vector2(-650f, 0f)
		};

		foreach (var offset in pathOffsets)
		{
			var pathSprite = new Sprite2D
			{
				Texture = _groundVillageTex,
				GlobalPosition = center + offset,
				ZIndex = -8
			};
			_lakeContainer.AddChild(pathSprite);
		}

		// 2. Inner Circle of Cottages (Town Square Plaza, radius = 850f)
		int innerHouseCount = 8;
		for (int i = 0; i < innerHouseCount; i++)
		{
			float angle = (i / (float)innerHouseCount) * Mathf.Pi * 2.0f;
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 850f;
			var house = HouseObject.Instantiate((i % 4) + 1, pos);
			_entitiesContainer.AddChild(house);
		}

		// 3. Outer Neighborhood Cottages across all 4 quadrants (extending village range)
		Vector2[] outerHouseOffsets =
		{
			// North-East District
			new Vector2(1350f, -700f),
			new Vector2(700f, -1350f),
			new Vector2(1450f, -1400f),

			// North-West District
			new Vector2(-1350f, -700f),
			new Vector2(-700f, -1350f),
			new Vector2(-1450f, -1400f),

			// South-East District
			new Vector2(1350f, 700f),
			new Vector2(700f, 1350f),
			new Vector2(1450f, 1400f),

			// South-West District
			new Vector2(-1350f, 700f),
			new Vector2(-700f, 1350f),
			new Vector2(-1450f, 1400f),
		};

		for (int i = 0; i < outerHouseOffsets.Length; i++)
		{
			var house = HouseObject.Instantiate(((i + 2) % 4) + 1, center + outerHouseOffsets[i]);
			_entitiesContainer.AddChild(house);
		}

		// 4. Street Lanterns placed at multiple strategic locations throughout town
		Vector2[] lanternOffsets =
		{
			// Central Square (4 corners around the plaza center)
			new Vector2(-220f, -220f),
			new Vector2(220f, -220f),
			new Vector2(-220f, 220f),
			new Vector2(220f, 220f),

			// Inner thoroughfare intersections
			new Vector2(0f, -480f),
			new Vector2(0f, 480f),
			new Vector2(480f, 0f),
			new Vector2(-480f, 0f),

			// Outer District crossroads
			new Vector2(800f, -800f),
			new Vector2(-800f, -800f),
			new Vector2(800f, 800f),
			new Vector2(-800f, 800f),

			// Town Gateways / Entrances
			new Vector2(0f, -1200f),
			new Vector2(0f, 1200f),
			new Vector2(1200f, 0f),
			new Vector2(-1200f, 0f),
		};

		foreach (var offset in lanternOffsets)
		{
			var lantern = LanternObject.Instantiate(center + offset, scale: 0.18f);
			_entitiesContainer.AddChild(lantern);
		}

		// 5. Outskirts buffer trees (lining the expanded perimeter)
		for (int i = 0; i < 40; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2f);
			float dist = (float)GD.RandRange(1350f, 1650f);
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			var tree = new TreeObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(tree);
		}
	}

	private void BuildVillagers(Vector2 center)
	{
		Vector2[] spawnOffsets =
		{
			// Central Square Plaza
			new Vector2(-100f, -80f),
			new Vector2(110f, 90f),

			// Inner thoroughfares / streets
			new Vector2(-40f, -420f),
			new Vector2(50f, 420f),
			new Vector2(420f, -40f),
			new Vector2(-420f, 50f),

			// Outer neighborhood pathways
			new Vector2(750f, -700f),
			new Vector2(-700f, 750f),
		};

		foreach (var offset in spawnOffsets)
		{
			Vector2 spawnPos = center + offset + new Vector2((float)GD.RandRange(-40, 40), (float)GD.RandRange(-40, 40));
			var villager = new Villager
			{
				GlobalPosition = spawnPos,
				HomePosition = spawnPos,
				TargetWerewolf = Player
			};
			_entitiesContainer.AddChild(villager);
		}
	}

	private void BuildLairEntrance(Vector2 pos)
	{
		var entrance = LairEntranceObject.Instantiate(pos);
		_entitiesContainer.AddChild(entrance);
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
			var tree = new TreeObject { GlobalPosition = pos };
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
			if (pos.DistanceTo(VillagePosition) < 1450f) continue;
			if (pos.DistanceTo(LairEntrancePosition) < 300f) continue;
			if (pos.DistanceTo(QuarryPosition) < 360f) continue;
			if (pos.DistanceTo(AwakeningGrovePosition) < 160f) continue;

			var tree = new TreeObject { GlobalPosition = pos };
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
			if (pos.DistanceTo(VillagePosition) < 1450f) continue;
			if (pos.DistanceTo(LairEntrancePosition) < 300f) continue;
			if (pos.DistanceTo(QuarryPosition) < 350f) continue;

			var rock = new RockObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(rock);
		}
	}

	private void BuildQuartzDeposits()
	{
		// 1. Scattered quartz formations across the open world
		int scatteredQuartzCount = 35;
		for (int i = 0; i < scatteredQuartzCount; i++)
		{
			Vector2 pos = new Vector2(
				(float)GD.RandRange(-WorldRadius + 300f, WorldRadius - 300f),
				(float)GD.RandRange(-WorldRadius + 300f, WorldRadius - 300f)
			);

			if (IsInsideLake(pos, 50f)) continue;
			if (pos.DistanceTo(VillagePosition) < 1450f) continue;
			if (pos.DistanceTo(LairEntrancePosition) < 300f) continue;
			if (pos.DistanceTo(QuarryPosition) < 350f) continue;
			if (pos.DistanceTo(AwakeningGrovePosition) < 160f) continue;

			var quartz = new QuartzObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(quartz);
		}

		// 2. Cluster of mineral quartz in Quarry Hills
		int quarryQuartzCount = 6;
		for (int i = 0; i < quarryQuartzCount; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2f);
			float dist = (float)GD.RandRange(80f, 350f);
			Vector2 pos = QuarryPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			var quartz = new QuartzObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(quartz);
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

	private void BuildFogZones()
	{
		// 1. Prime Landmark Fog: Misty Lake (Massive boosted fog bank)
		var mistyLakeFog = FogZone.Instantiate(
			position: MistyLakePosition,
			radius: 900f,
			color: new Color(0.88f, 0.94f, 1.0f, 0.94f),
			density: 0.70f,
			coverage: 0.60f,
			cycleSpeed: 0.035f
		);
		mistyLakeFog.Name = "FogZone_MistyLake";
		_fogContainer.AddChild(mistyLakeFog);

		// 2. Prime Landmark Fog: Silent Lake (Dense cool lake mist)
		var silentLakeFog = FogZone.Instantiate(
			position: SilentLakePosition,
			radius: 800f,
			color: new Color(0.85f, 0.92f, 0.98f, 0.92f),
			density: 0.65f,
			coverage: 0.58f,
			cycleSpeed: 0.032f
		);
		silentLakeFog.Name = "FogZone_SilentLake";
		_fogContainer.AddChild(silentLakeFog);

		// 3. Awakening Grove: Soft morning glade mist
		var groveFog = FogZone.Instantiate(
			position: AwakeningGrovePosition,
			radius: 550f,
			color: new Color(0.92f, 0.94f, 0.96f, 0.85f),
			density: 0.58f,
			coverage: 0.52f,
			cycleSpeed: 0.04f
		);
		groveFog.Name = "FogZone_AwakeningGrove";
		_fogContainer.AddChild(groveFog);

		// 4. Random Fog Zones Across the Open World with variable sizes
		Color[] mistPalette =
		{
			new Color(0.88f, 0.94f, 1.0f, 0.92f),   // Cool azure white
			new Color(0.86f, 0.92f, 0.88f, 0.90f),  // Mossy forest mist
			new Color(0.92f, 0.93f, 0.97f, 0.92f),  // Moonlit silver mist
			new Color(0.85f, 0.89f, 0.87f, 0.88f),  // Murky hollow vapor
			new Color(0.90f, 0.94f, 0.95f, 0.90f)   // Deep woods fog
		};

		int randomZoneCount = 36;
		for (int i = 0; i < randomZoneCount; i++)
		{
			Vector2 pos = new Vector2(
				(float)GD.RandRange(-WorldRadius + 500f, WorldRadius - 500f),
				(float)GD.RandRange(-WorldRadius + 500f, WorldRadius - 500f)
			);

			// Avoid placing fog directly over the central village square or lair entrance
			if (pos.DistanceTo(VillagePosition) < 850f) continue;
			if (pos.DistanceTo(LairEntrancePosition) < 300f) continue;

			// Variable sizes: small rolling patches (350-500f), medium banks (550-850f), massive expanses (900-1300f)
			float sizeTier = GD.Randf();
			float radius;
			float density;
			float coverage;

			if (sizeTier < 0.35f)
			{
				radius = (float)GD.RandRange(350f, 500f);
				density = (float)GD.RandRange(0.50f, 0.60f);
				coverage = (float)GD.RandRange(0.50f, 0.58f);
			}
			else if (sizeTier < 0.75f)
			{
				radius = (float)GD.RandRange(550f, 850f);
				density = (float)GD.RandRange(0.58f, 0.68f);
				coverage = (float)GD.RandRange(0.55f, 0.62f);
			}
			else
			{
				radius = (float)GD.RandRange(900f, 1300f);
				density = (float)GD.RandRange(0.65f, 0.75f);
				coverage = (float)GD.RandRange(0.58f, 0.66f);
			}

			Color color = mistPalette[i % mistPalette.Length];
			float cycleSpeed = (float)GD.RandRange(0.025f, 0.045f);
			float timeOffset = (float)GD.RandRange(0.0f, 500.0f);

			var zone = FogZone.Instantiate(pos, radius, color, density, coverage, cycleSpeed, timeOffset);
			zone.Name = $"FogZone_Random_{i}";
			_fogContainer.AddChild(zone);
		}
	}
}
