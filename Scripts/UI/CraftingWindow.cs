using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class CraftingWindow : Control
{
    private Panel _mainPanel = null!;
    private VBoxContainer _recipesContainer = null!;
    private Button _closeButton = null!;

    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;

    private readonly Dictionary<string, Button> _actionButtons = new();
    private readonly Dictionary<string, Label> _costsLabels = new();
    private readonly Dictionary<string, Label> _statusLabels = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(580, 560);
        Size = new Vector2(580, 560);

        BuildWindow();

        Visible = false;
        GameState.Instance.OnCraftingToggled += OnCraftingToggled;
        GameState.Instance.OnPouchChanged += RefreshRecipes;
        GameState.Instance.OnBloodCoreReservesChanged += OnBloodCoreReservesChanged;
        GameState.Instance.OnStaticObjectCrafted += OnStaticObjectCrafted;

        RefreshRecipes();
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnCraftingToggled -= OnCraftingToggled;
            GameState.Instance.OnPouchChanged -= RefreshRecipes;
            GameState.Instance.OnBloodCoreReservesChanged -= OnBloodCoreReservesChanged;
            GameState.Instance.OnStaticObjectCrafted -= OnStaticObjectCrafted;
        }
    }

    private void BuildWindow()
    {
        _mainPanel = new Panel
        {
            Name = "MainPanel",
            CustomMinimumSize = new Vector2(580, 560),
            Size = new Vector2(580, 560),
            MouseFilter = MouseFilterEnum.Stop
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.08f, 0.11f, 0.96f),
            BorderColor = new Color(0.72f, 0.58f, 0.32f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            ShadowColor = new Color(0f, 0f, 0f, 0.70f),
            ShadowSize = 8
        };
        _mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _mainPanel.GuiInput += OnPanelGuiInput;
        AddChild(_mainPanel);

        // Header
        var header = new HBoxContainer
        {
            Name = "Header",
            Position = new Vector2(16, 12),
            Size = new Vector2(548, 36)
        };
        _mainPanel.AddChild(header);

        var titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "Cave Crafting & Installations",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.50f));
        titleLabel.AddThemeConstantOverride("outline_size", 3);
        titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        header.AddChild(titleLabel);

        _closeButton = new Button
        {
            Name = "CloseButton",
            Text = "X",
            CustomMinimumSize = new Vector2(28, 28),
            Size = new Vector2(28, 28)
        };
        _closeButton.Pressed += () => GameState.Instance.ToggleCrafting(false);
        ApplyCloseButtonStyle(_closeButton);
        header.AddChild(_closeButton);

        // Subtitle / instructions
        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = "Construct functional hideout structures and storage containers for your sanctuary.",
            Position = new Vector2(18, 48),
            Size = new Vector2(544, 22)
        };
        subtitle.AddThemeFontSizeOverride("font_size", 12);
        subtitle.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.80f));
        _mainPanel.AddChild(subtitle);

        // Scrollable cards container
        var scroll = new ScrollContainer
        {
            Name = "ScrollContainer",
            Position = new Vector2(16, 76),
            Size = new Vector2(548, 468)
        };
        _mainPanel.AddChild(scroll);

        _recipesContainer = new VBoxContainer
        {
            Name = "RecipesContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _recipesContainer.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_recipesContainer);

        // Build 4 recipe cards
        BuildRecipeCard("Chest");
        BuildRecipeCard("CraftingTable");
        BuildRecipeCard("BloodJuicer");
        BuildRecipeCard("Laboratory");
    }

    private void BuildRecipeCard(string recipeId)
    {
        if (!GameState.Recipes.TryGetValue(recipeId, out var recipe)) return;

        var card = new PanelContainer
        {
            Name = $"Card_{recipeId}",
            CustomMinimumSize = new Vector2(532, 102),
            MouseFilter = MouseFilterEnum.Pass
        };

        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.11f, 0.12f, 0.16f, 0.90f),
            BorderColor = new Color(0.40f, 0.35f, 0.25f, 0.75f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var hbox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        hbox.AddThemeConstantOverride("separation", 12);
        card.AddChild(hbox);

        // Icon Box
        var iconPanel = new Panel
        {
            CustomMinimumSize = new Vector2(64, 64),
            Size = new Vector2(64, 64)
        };
        var iconBg = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 1f),
            BorderColor = new Color(0.55f, 0.45f, 0.28f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        iconPanel.AddThemeStyleboxOverride("panel", iconBg);

        var iconTex = new TextureRect
        {
            Texture = GD.Load<Texture2D>(recipe.IconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(58, 58),
            Position = new Vector2(3, 3)
        };
        iconPanel.AddChild(iconTex);
        hbox.AddChild(iconPanel);

        // Middle description & cost
        var infoVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        infoVBox.AddThemeConstantOverride("separation", 3);

        var titleRow = new HBoxContainer();
        var nameLbl = new Label
        {
            Text = recipe.Name
        };
        nameLbl.AddThemeFontSizeOverride("font_size", 15);
        nameLbl.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.65f));
        titleRow.AddChild(nameLbl);

        var locLbl = new Label
        {
            Text = $"  [{recipe.LocationDescription}]"
        };
        locLbl.AddThemeFontSizeOverride("font_size", 11);
        locLbl.AddThemeColorOverride("font_color", new Color(0.65f, 0.75f, 0.90f));
        titleRow.AddChild(locLbl);
        infoVBox.AddChild(titleRow);

        var descLbl = new Label
        {
            Text = recipe.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLbl.AddThemeFontSizeOverride("font_size", 11);
        descLbl.AddThemeColorOverride("font_color", new Color(0.70f, 0.72f, 0.76f));
        infoVBox.AddChild(descLbl);

        var costLbl = new Label
        {
            Name = $"CostLabel_{recipeId}"
        };
        costLbl.AddThemeFontSizeOverride("font_size", 12);
        _costsLabels[recipeId] = costLbl;
        infoVBox.AddChild(costLbl);

        hbox.AddChild(infoVBox);

        // Right button & status box
        var rightVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(120, 0),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        rightVBox.AddThemeConstantOverride("separation", 4);

        var statusLbl = new Label
        {
            Name = $"StatusLabel_{recipeId}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusLbl.AddThemeFontSizeOverride("font_size", 11);
        _statusLabels[recipeId] = statusLbl;
        rightVBox.AddChild(statusLbl);

        var actionBtn = new Button
        {
            Name = $"ActionBtn_{recipeId}",
            CustomMinimumSize = new Vector2(116, 34),
            Text = recipe.IsStatic ? "Build" : "Craft & Place"
        };
        ApplyActionButtonStyle(actionBtn);

        actionBtn.Pressed += () => OnRecipeActionPressed(recipeId);
        _actionButtons[recipeId] = actionBtn;
        rightVBox.AddChild(actionBtn);

        hbox.AddChild(rightVBox);

        _recipesContainer.AddChild(card);
    }

    private void OnRecipeActionPressed(string recipeId)
    {
        if (!GameState.Instance.IsInLair)
        {
            GameState.Instance.TriggerDamageNumber("You must be inside the Lair to build cave objects!", GameState.Instance.PlayerPosition + new Vector2(0, -60), new Color(1f, 0.4f, 0.4f));
            return;
        }

        if (!GameState.Recipes.TryGetValue(recipeId, out var recipe)) return;

        if (recipe.IsStatic)
        {
            if (GameState.Instance.CraftedStaticObjects.Contains(recipeId))
            {
                GameState.Instance.TriggerDamageNumber("Already constructed!", GameState.Instance.PlayerPosition + new Vector2(0, -60), new Color(1f, 0.8f, 0.3f));
                return;
            }

            if (!GameState.Instance.CanCraft(recipeId))
            {
                GameState.Instance.TriggerDamageNumber("Not enough materials!", GameState.Instance.PlayerPosition + new Vector2(0, -60), new Color(1f, 0.4f, 0.4f));
                return;
            }

            GameState.Instance.CraftStaticObject(recipeId);
            RefreshRecipes();
        }
        else
        {
            // Dynamic placement (Chest)
            if (!GameState.Instance.CanCraft(recipeId))
            {
                GameState.Instance.TriggerDamageNumber("Not enough materials!", GameState.Instance.PlayerPosition + new Vector2(0, -60), new Color(1f, 0.4f, 0.4f));
                return;
            }

            GameState.Instance.StartChestPlacement(null);
            GameState.Instance.ToggleCrafting(false);
        }
    }

    public void RefreshRecipes()
    {
        if (!GodotObject.IsInstanceValid(this) || _recipesContainer == null) return;

        foreach (var kvp in GameState.Recipes)
        {
            string id = kvp.Key;
            var recipe = kvp.Value;

            bool isBuilt = recipe.IsStatic && GameState.Instance.CraftedStaticObjects.Contains(id);
            bool canCraft = GameState.Instance.CanCraft(id);

            // Format Cost Label
            if (_costsLabels.TryGetValue(id, out var costLbl))
            {
                var costParts = new List<string>();
                foreach (var ing in recipe.Ingredients)
                {
                    int owned = 0;
                    if (GameState.Instance.PouchItems.TryGetValue(ing.Key, out var item))
                    {
                        owned = item.Count;
                    }
                    costParts.Add($"{ing.Key}: {owned}/{ing.Value}");
                }
                if (recipe.BloodCost > 0f)
                {
                    int ownedBlood = (int)GameState.Instance.BloodCoreReserves;
                    costParts.Add($"Blood: {ownedBlood}/{(int)recipe.BloodCost}");
                }

                costLbl.Text = "Cost: " + string.Join("  |  ", costParts);
                costLbl.AddThemeColorOverride("font_color", canCraft ? new Color(0.45f, 0.95f, 0.50f) : new Color(0.95f, 0.45f, 0.45f));
            }

            // Status & Button
            if (_actionButtons.TryGetValue(id, out var btn) && _statusLabels.TryGetValue(id, out var statusLbl))
            {
                if (isBuilt)
                {
                    statusLbl.Text = "✓ Constructed";
                    statusLbl.AddThemeColorOverride("font_color", new Color(0.40f, 0.95f, 0.55f));
                    btn.Text = "Constructed";
                    btn.Disabled = true;
                }
                else
                {
                    statusLbl.Text = canCraft ? "Ready to Build" : "Missing Materials";
                    statusLbl.AddThemeColorOverride("font_color", canCraft ? new Color(0.85f, 0.95f, 0.60f) : new Color(0.80f, 0.45f, 0.45f));

                    btn.Text = recipe.IsStatic ? "Build" : "Craft & Place";
                    btn.Disabled = !canCraft;
                }
            }
        }
    }

    private void OnCraftingToggled(bool visible)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Visible = visible;
        if (visible)
        {
            RefreshRecipes();
        }
    }

    private void OnBloodCoreReservesChanged(float current, float max)
    {
        RefreshRecipes();
    }

    private void OnStaticObjectCrafted(string objectId)
    {
        RefreshRecipes();
    }

    private void OnPanelGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - Position;
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDragging && @event is InputEventMouseMotion)
        {
            Position = GetGlobalMousePosition() - _dragOffset;
            var vpSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, vpSize.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, vpSize.Y - Size.Y))
            );
        }
    }

    private void ApplyActionButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.16f, 0.18f, 0.24f, 1.0f),
            BorderColor = new Color(0.65f, 0.52f, 0.32f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.24f, 0.26f, 0.34f, 1.0f),
            BorderColor = new Color(1.0f, 0.85f, 0.40f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var disabled = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.11f, 0.7f),
            BorderColor = new Color(0.30f, 0.30f, 0.32f, 0.5f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("disabled", disabled);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.80f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.50f, 0.50f, 0.52f));
        btn.AddThemeFontSizeOverride("font_size", 12);
    }

    private void ApplyCloseButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.08f, 0.08f, 0.8f),
            BorderColor = new Color(0.65f, 0.25f, 0.25f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.10f, 0.10f, 1.0f),
            BorderColor = new Color(0.95f, 0.35f, 0.35f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.85f));
        btn.AddThemeFontSizeOverride("font_size", 13);
    }
}
