using Godot;
using Werewolves.Core;
using Werewolves.Entities;
using Werewolves.UI;
using Werewolves.World;

namespace Werewolves;

public partial class Main : Node2D
{
	private Werewolf _player = null!;
	private WorldManager _worldManager = null!;
	private HUDManager _hud = null!;

	public override void _Ready()
	{
		// 1. Initialize GameState singleton if not present
		if (GameState.Instance == null)
		{
			var gameState = new GameState { Name = "GameState" };
			AddChild(gameState);
		}

		if (GameState.Instance?.IsInLair == true)
		{
			Callable.From(() => GetTree().ChangeSceneToFile("res://scenes/Lair.tscn")).CallDeferred();
			return;
		}

		// 2. Set default game cursor
		CursorManager.ResetNormal();

		// 3. Find or Create Player (Werewolf)
		_player = GetNodeOrNull<Werewolf>("WorldManager/Entities/Werewolf")
			   ?? GetNodeOrNull<Werewolf>("Entities/Werewolf")
			   ?? GetNodeOrNull<Werewolf>("WerewolfPlayer");

		if (_player == null)
		{
			_player = new Werewolf
			{
				Name = "WerewolfPlayer",
				GlobalPosition = SaveManager.LoadedPlayerPosition
			};
			AddChild(_player);
		}

		// 4. Find or Create WorldManager
		_worldManager = GetNodeOrNull<WorldManager>("WorldManager");
		if (_worldManager == null)
		{
			_worldManager = new WorldManager
			{
				Name = "WorldManager",
				Player = _player
			};
			AddChild(_worldManager);
		}
		else
		{
			_worldManager.Player = _player;
		}

		// 5. Find or Create HUD
		_hud = GetNodeOrNull<HUDManager>("HUD");
		if (_hud == null)
		{
			_hud = new HUDManager
			{
				Name = "HUD",
				Player = _player
			};
			AddChild(_hud);
		}
		else
		{
			_hud.Player = _player;
		}
	}
}
