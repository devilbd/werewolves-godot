using Godot;
using Werewolves.Core;
using Werewolves.Entities;

namespace Werewolves.UI;

public partial class TargetPanel : PanelContainer
{
    [Export] public Werewolf? Player { get; set; }

    private Label _nameLabel = null!;
    private ProgressBar _healthBar = null!;
    private Button _attackButton = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(240, 75);

        _nameLabel = GetNodeOrNull<Label>("VBoxContainer/Header/NameLabel");
        _attackButton = GetNodeOrNull<Button>("VBoxContainer/Header/AttackButton");
        _healthBar = GetNodeOrNull<ProgressBar>("VBoxContainer/HealthBar");

        if (_nameLabel == null || _attackButton == null || _healthBar == null)
        {
            var vbox = new VBoxContainer { Name = "VBoxContainer" };
            vbox.AddThemeConstantOverride("separation", 4);
            AddChild(vbox);

            var headerHbox = new HBoxContainer { Name = "Header" };
            _nameLabel = new Label
            {
                Name = "NameLabel",
                Text = "Target",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            _nameLabel.AddThemeFontSizeOverride("font_size", 14);
            headerHbox.AddChild(_nameLabel);

            _attackButton = new Button
            {
                Name = "AttackButton",
                Text = "Attack",
                CustomMinimumSize = new Vector2(70, 24)
            };
            headerHbox.AddChild(_attackButton);
            vbox.AddChild(headerHbox);

            _healthBar = new ProgressBar
            {
                Name = "HealthBar",
                MinValue = 0,
                MaxValue = 100,
                Value = 100,
                CustomMinimumSize = new Vector2(220, 16),
                ShowPercentage = true
            };
            vbox.AddChild(_healthBar);
        }

        _attackButton.Pressed += OnAttackPressed;
        Visible = false;
        GameState.Instance.OnTargetChanged += OnTargetChanged;
    }

    private void OnTargetChanged(Node2D? target)
    {
        if (target is ISelectableTarget selectable && !selectable.IsDead)
        {
            Visible = true;
            _nameLabel.Text = selectable.TargetName;
            _healthBar.MaxValue = selectable.MaxHealth;
            _healthBar.Value = selectable.Health;
        }
        else
        {
            Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        if (Visible && GameState.Instance.SelectedTarget is ISelectableTarget selectable)
        {
            if (selectable.IsDead)
            {
                Visible = false;
                return;
            }

            _healthBar.Value = selectable.Health;
            _attackButton.Text = selectable.TargetName switch
            {
                "Tree" => "Chop",
                "Rock" => "Quarry",
                _ => "Attack"
            };
        }
    }

    private void OnAttackPressed()
    {
        Player?.TriggerInteractAction();
    }
}
