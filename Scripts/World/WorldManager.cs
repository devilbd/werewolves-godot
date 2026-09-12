using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.World;

public partial class WorldManager : Node2D
{
	[Export] public Werewolf Player { get; set; } = null!;

	private TextureRect _groundBackground = null!;
	private Node2D _lakeContainer = null!;
	private Node2D _entitiesContainer = null!;

	private Texture2D _groundForestTex = null!;
	private Texture2D _groundVillageTex = null!;
	private List<Texture2D> _lakeTextures = new();

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

		if (Player != null)
		{
			if (Player.GetParent() != _entitiesContainer)
			{
				Player.GetParent()?.RemoveChild(Player);
				_entitiesContainer.AddChild(Player);
			}
			Player.OnExitedScreenEdge += OnPlayerExitedScreenEdge;
		}

		GameState.Instance.OnSpawnDamageNumber += (text, posX, color) =>
		{
			var dn = Effects.DamageNumber.Instantiate(text, new Vector2(posX, Player?.GlobalPosition.Y ?? 300), color);
			_entitiesContainer.AddChild(dn);
		};

		GetViewport().SizeChanged += () =>
		{
			_groundBackground.Size = GetViewportRect().Size;
		};

		GenerateCurrentArea();
	}

	private void OnPlayerExitedScreenEdge(Vector2 dir)
	{
		Vector2I newCoords = GameState.Instance.CurrentMapPosition + new Vector2I((int)dir.X, (int)dir.Y);
		GameState.Instance.SetMapPosition(newCoords);
		GenerateCurrentArea();
	}

	public void GenerateCurrentArea()
	{
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

		Vector2I coords = GameState.Instance.CurrentMapPosition;
		bool isVillage = coords.X == 5 && coords.Y == 5;

		var viewportSize = GetViewportRect().Size;
		if (viewportSize == Vector2.Zero) viewportSize = new Vector2(1920, 1080);
		_groundBackground.Size = viewportSize;

		if (isVillage)
		{
			GenerateVillage(viewportSize);
		}
		else
		{
			GenerateWilderness(viewportSize);
		}
	}

	private void GenerateVillage(Vector2 size)
	{
		_groundBackground.Texture = _groundVillageTex;

		Vector2 center = size / 2.0f;
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

		// A few outskirts trees
		for (int i = 0; i < 22; i++)
		{
			Vector2 pos = new Vector2((float)GD.RandRange(40, size.X - 40), (float)GD.RandRange(40, size.Y - 40));
			if (pos.DistanceTo(center) > radius + 120f)
			{
				var tree = new TreeObject { GlobalPosition = pos, IsSelectable = GD.Randf() < 0.15f };
				_entitiesContainer.AddChild(tree);
			}
		}
	}

	private void GenerateWilderness(Vector2 size)
	{
		_groundBackground.Texture = _groundForestTex;

		Rect2? lakeBounds = null;

		// 25% chance of lake
		if (GD.Randf() < 0.25f && _lakeTextures.Count > 0)
		{
			var lakeTex = _lakeTextures[GD.RandRange(0, _lakeTextures.Count - 1)];
			Vector2 lakePos = new Vector2(size.X * 0.5f - 225, size.Y * 0.5f - 160);
			lakeBounds = new Rect2(lakePos, new Vector2(450, 320));

			var lakeSprite = new Sprite2D
			{
				Texture = lakeTex,
				GlobalPosition = lakeBounds.Value.GetCenter()
			};
			_lakeContainer.AddChild(lakeSprite);

			// Lake collision body
			var lakeBody = new StaticBody2D { GlobalPosition = lakeBounds.Value.GetCenter() };
			var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = lakeBounds.Value.Size * 0.85f } };
			lakeBody.AddChild(shape);
			_lakeContainer.AddChild(lakeBody);
		}

		// Generate ~60 trees
		int treeCount = 60;
		for (int i = 0; i < treeCount; i++)
		{
			Vector2 pos = new Vector2((float)GD.RandRange(50, size.X - 50), (float)GD.RandRange(50, size.Y - 50));
			if (lakeBounds.HasValue && lakeBounds.Value.Grow(40f).HasPoint(pos))
			{
				continue;
			}

			var tree = new TreeObject
			{
				GlobalPosition = pos,
				IsSelectable = GD.Randf() < 0.15f
			};
			_entitiesContainer.AddChild(tree);
		}

		// Generate 2 boulders
		for (int i = 0; i < 2; i++)
		{
			Vector2 pos = new Vector2((float)GD.RandRange(100, size.X - 100), (float)GD.RandRange(100, size.Y - 100));
			if (lakeBounds.HasValue && lakeBounds.Value.Grow(40f).HasPoint(pos))
			{
				continue;
			}

			var rock = new RockObject { GlobalPosition = pos };
			_entitiesContainer.AddChild(rock);
		}

		// 30% chance of wild deer
		if (GD.Randf() < 0.3f)
		{
			Vector2 deerPos = new Vector2((float)GD.RandRange(100, size.X - 100), (float)GD.RandRange(100, size.Y - 100));
			if (!lakeBounds.HasValue || !lakeBounds.Value.Grow(50f).HasPoint(deerPos))
			{
				var deer = new Deer
				{
					GlobalPosition = deerPos,
					TargetWerewolf = Player
				};
				_entitiesContainer.AddChild(deer);
			}
		}
	}
}
