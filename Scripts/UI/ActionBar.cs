using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class ActionBar : Control
{
    private TextureButton _pouchButton = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(100, 60);

        _pouchButton = GetNodeOrNull<TextureButton>("PouchButton");
        if (_pouchButton == null)
        {
            _pouchButton = new TextureButton
            {
                Name = "PouchButton",
                TextureNormal = GD.Load<Texture2D>("res://assets/pouch_bag_o.png"),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                Size = new Vector2(50, 50),
                Position = new Vector2(25, 5)
            };
            AddChild(_pouchButton);
        }
        else if (_pouchButton.TextureNormal == null)
        {
            _pouchButton.TextureNormal = GD.Load<Texture2D>("res://assets/pouch_bag_o.png");
        }
        _pouchButton.Pressed += () => GameState.Instance.TogglePouch();
    }
}
