using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class ChestInventoryWindow : Control
{
    private TextureRect _bgTexture = null!;
    private Control _itemsArea = null!;
    private Button _closeButton = null!;
    private Button _moveButton = null!;
    private Button _dismantleButton = null!;
    private Label _titleLabel = null!;

    // Window dragging
    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;

    // Item dragging state
    private bool _isDraggingItem = false;
    private bool _hasDraggedItem = false;
    private Control? _draggedItemControl = null;
    private string? _draggedItemName = null;
    private Vector2 _itemDragOffset = Vector2.Zero;
    private Vector2 _itemDragStartPos = Vector2.Zero;

    private string? _activeChestId = null;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(600, 450);
        Size = new Vector2(600, 450);

        BuildUI();

        Visible = false;
        GameState.Instance.OnChestInventoryToggled += OnChestInventoryToggled;
        GameState.Instance.OnChestInventoryChanged += OnChestInventoryChanged;
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnChestInventoryToggled -= OnChestInventoryToggled;
            GameState.Instance.OnChestInventoryChanged -= OnChestInventoryChanged;
        }
    }

    private void BuildUI()
    {
        // 1. Background Texture
        _bgTexture = new TextureRect
        {
            Name = "BgTexture",
            Texture = GD.Load<Texture2D>("res://assets/chests/chest_inventory.png"),
            CustomMinimumSize = new Vector2(600, 450),
            Size = new Vector2(600, 450),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Stop
        };
        _bgTexture.GuiInput += OnBgGuiInput;
        AddChild(_bgTexture);

        // 2. Header
        _titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "Storage Chest",
            Position = new Vector2(40, 20),
            Size = new Vector2(250, 28)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.50f));
        _titleLabel.AddThemeConstantOverride("outline_size", 3);
        _titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(_titleLabel);

        // Move Chest Button
        _moveButton = new Button
        {
            Name = "MoveButton",
            Text = "Move Chest",
            Position = new Vector2(325, 18),
            Size = new Vector2(95, 28),
            TooltipText = "Pick up and reposition this chest elsewhere in the Lair"
        };
        ApplySmallButtonStyle(_moveButton, new Color(0.18f, 0.22f, 0.30f));
        _moveButton.Pressed += OnMovePressed;
        AddChild(_moveButton);

        // Dismantle Chest Button
        _dismantleButton = new Button
        {
            Name = "DismantleButton",
            Text = "Dismantle",
            Position = new Vector2(426, 18),
            Size = new Vector2(95, 28),
            TooltipText = "Dismantle chest and recover 10 Logs and 5 Stones (stored items returned to pouch)"
        };
        ApplySmallButtonStyle(_dismantleButton, new Color(0.42f, 0.15f, 0.15f));
        _dismantleButton.Pressed += OnDismantlePressed;
        AddChild(_dismantleButton);

        // Close Button
        _closeButton = new Button
        {
            Name = "CloseButton",
            Text = "X",
            Position = new Vector2(532, 16),
            Size = new Vector2(30, 28)
        };
        ApplyCloseButtonStyle(_closeButton);
        _closeButton.Pressed += () => GameState.Instance.CloseChestInventory();
        AddChild(_closeButton);

        // 3. Stored Items Area (Freeform layout matching Pouch)
        _itemsArea = new Control
        {
            Name = "ItemsArea",
            Position = new Vector2(35, 60),
            CustomMinimumSize = new Vector2(530, 365),
            Size = new Vector2(530, 365),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Pass
        };
        AddChild(_itemsArea);
    }

    private void OnChestInventoryToggled(bool isOpen, string? chestId)
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        Visible = isOpen;
        _activeChestId = isOpen ? chestId : null;

        if (isOpen && _activeChestId != null)
        {
            // Auto open pouch window side by side if not open
            if (!GameState.Instance.IsPouchOpen)
            {
                GameState.Instance.TogglePouch();
            }

            RefreshItems();
        }
    }

    private void OnChestInventoryChanged(string chestId)
    {
        if (_activeChestId == chestId && Visible)
        {
            RefreshItems();
        }
    }

    private void OnMovePressed()
    {
        if (_activeChestId == null) return;
        string chestId = _activeChestId;
        GameState.Instance.CloseChestInventory();
        GameState.Instance.StartChestPlacement(chestId);
    }

    private void OnDismantlePressed()
    {
        if (string.IsNullOrEmpty(_activeChestId)) return;
        string chestId = _activeChestId;
        GameState.Instance.DismantleChest(chestId);
    }

    public void RefreshItems()
    {
        if (!GodotObject.IsInstanceValid(this) || _itemsArea == null || _activeChestId == null) return;

        foreach (Node child in _itemsArea.GetChildren())
        {
            _itemsArea.RemoveChild(child);
            child.QueueFree();
        }

        if (!GameState.Instance.CaveChests.TryGetValue(_activeChestId, out var chest))
        {
            return;
        }

        int itemIndex = 0;
        foreach (var kvp in chest.Items)
        {
            string itemName = kvp.Key;
            var itemData = kvp.Value;
            if (itemData.Count <= 0) continue;

            float maxX = Mathf.Max(0f, _itemsArea.Size.X - 44f);
            float maxY = Mathf.Max(0f, _itemsArea.Size.Y - 44f);

            bool isUnset = itemData.PosX == 0f && itemData.PosY == 0f;
            bool isOutOfBounds = itemData.PosX < 0f || itemData.PosX > maxX || itemData.PosY < 0f || itemData.PosY > maxY;

            Vector2 defaultPos = GetDefaultPosition(itemIndex);
            Vector2 itemPos = (isUnset || isOutOfBounds) ? defaultPos : new Vector2(itemData.PosX, itemData.PosY);

            if (isUnset || isOutOfBounds)
            {
                itemData.PosX = itemPos.X;
                itemData.PosY = itemPos.Y;
            }

            var itemCtrl = CreateItemControl(itemName, itemData, itemPos);
            _itemsArea.AddChild(itemCtrl);
            itemIndex++;
        }
    }

    private Control CreateItemControl(string itemName, PouchItemData item, Vector2 itemPos)
    {
        bool isBloodFlask = itemName.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase);
        bool isPowerFlask = itemName.StartsWith("PowerFlask", StringComparison.OrdinalIgnoreCase);

        string iconPath = isBloodFlask
            ? GetFlaskTexturePath(item.BloodPercent)
            : isPowerFlask
                ? GetPowerFlaskTexturePath(item.PowerPercent)
                : itemName switch
                {
                    "Logs" => "res://assets/logs_collected_o.png",
                    "Stones" => "res://assets/rock_stones_loot_collected_o.png",
                    "Meat" => "res://assets/meat_collected_o.png",
                    "GoldCoins" or "Gold Coins" or "Gold" => "res://assets/gold_coins.png",
                    "Quartz" => "res://assets/resources/quartz/quartz_2.png",
                    "EmptyFlask" or "Empty Flask" or "Flask" => "res://assets/flasks/blood_flask_0.png",
                    "Grass" => "res://assets/grass/grass_drop.png",
                    _ => "res://assets/logs_collected_o.png"
                };

        string displayName = isBloodFlask
            ? $"Blood Flask ({item.BloodPercent}%)"
            : (isPowerFlask ? $"Power Flask ({item.PowerPercent}%)" : itemName);

        string tooltip = (isBloodFlask || isPowerFlask)
            ? $"{displayName}\n(Click/Right-Click: Grab to Pouch | Drag to organize)"
            : item.Count > 1
                ? $"{itemName} ({item.Count})\n(Click: Grab 1 | Shift+Click: Choose amount | Drag to organize)"
                : $"{itemName}\n(Click/Right-Click: Grab to Pouch | Drag to organize)";

        var itemContainer = new Control
        {
            Position = itemPos,
            CustomMinimumSize = new Vector2(44, 44),
            Size = new Vector2(44, 44),
            MouseFilter = MouseFilterEnum.Stop,
            TooltipText = tooltip
        };

        var iconTex = new TextureRect
        {
            Texture = GD.Load<Texture2D>(iconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(44, 44),
            MouseFilter = MouseFilterEnum.Ignore
        };
        itemContainer.AddChild(iconTex);

        var countLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };

        if (isBloodFlask)
        {
            countLabel.Text = $"{item.BloodPercent}%";
            countLabel.Position = new Vector2(4, 26);
            countLabel.Size = new Vector2(38, 16);
            countLabel.HorizontalAlignment = HorizontalAlignment.Right;
            countLabel.AddThemeFontSizeOverride("font_size", 11);
            countLabel.AddThemeColorOverride("font_color", item.BloodPercent >= 100 ? new Color(1f, 0.45f, 0.45f) : new Color(1f, 0.85f, 0.4f));
        }
        else if (isPowerFlask)
        {
            countLabel.Text = $"{item.PowerPercent}%";
            countLabel.Position = new Vector2(4, 26);
            countLabel.Size = new Vector2(38, 16);
            countLabel.HorizontalAlignment = HorizontalAlignment.Right;
            countLabel.AddThemeFontSizeOverride("font_size", 11);
            countLabel.AddThemeColorOverride("font_color", item.PowerPercent >= 100 ? new Color(0.35f, 0.85f, 1f) : new Color(0.6f, 0.9f, 1f));
        }
        else
        {
            countLabel.Text = item.Count.ToString();
            countLabel.Position = new Vector2(22, 24);
            countLabel.Size = new Vector2(20, 16);
            countLabel.HorizontalAlignment = HorizontalAlignment.Right;
            countLabel.AddThemeFontSizeOverride("font_size", 12);
            countLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.4f));
        }

        countLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        countLabel.AddThemeConstantOverride("outline_size", 2);
        itemContainer.AddChild(countLabel);

        string captureItemName = itemName;
        int captureCount = item.Count;
        Texture2D? captureIcon = iconTex.Texture;

        itemContainer.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && _activeChestId != null)
            {
                bool isShift = mb.ShiftPressed || Input.IsKeyPressed(Key.Shift);

                // If Shift held and count > 1: Open Split Quantity Modal
                if (isShift && captureCount > 1)
                {
                    GetViewport().SetInputAsHandled();
                    string chestId = _activeChestId;
                    ItemSplitModal.Instance?.Open(captureItemName, displayName, captureIcon, captureCount, "Grab", (amount) =>
                    {
                        GameState.Instance.TransferItemChestToPouch(chestId, captureItemName, amount);
                    });
                    return;
                }

                if (isShift && captureCount <= 1)
                {
                    GetViewport().SetInputAsHandled();
                    GameState.Instance.TransferItemChestToPouch(_activeChestId, captureItemName, 1);
                    return;
                }

                // Right click grabs 1 immediately
                if (mb.ButtonIndex == MouseButton.Right)
                {
                    GetViewport().SetInputAsHandled();
                    GameState.Instance.TransferItemChestToPouch(_activeChestId, captureItemName, 1);
                    return;
                }

                // Left click initiates drag or click
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    _isDraggingItem = true;
                    _hasDraggedItem = false;
                    _draggedItemControl = itemContainer;
                    _draggedItemName = captureItemName;
                    _itemDragStartPos = itemContainer.Position;
                    _itemDragOffset = itemContainer.GetLocalMousePosition();
                    GetViewport().SetInputAsHandled();
                }
            }
        };

        return itemContainer;
    }

    private static Vector2 GetDefaultPosition(int index)
    {
        int col = index % 8;
        int row = index / 8;
        return new Vector2(15 + col * 62, 15 + row * 62);
    }

    private static string GetFlaskTexturePath(int bloodPercent)
    {
        if (bloodPercent <= 0) return "res://assets/flasks/blood_flask_0.png";
        if (bloodPercent <= 25) return "res://assets/flasks/blood_flask_25.png";
        if (bloodPercent <= 50) return "res://assets/flasks/blood_flask_50.png";
        if (bloodPercent <= 75) return "res://assets/flasks/blood_flask_75.png";
        return "res://assets/flasks/blood_flask_100.png";
    }

    private static string GetPowerFlaskTexturePath(int powerPercent)
    {
        if (powerPercent >= 85) return "res://assets/flasks/power_flask_100.png";
        if (powerPercent >= 40) return "res://assets/flasks/power_flask_50.png";
        if (powerPercent >= 15) return "res://assets/flasks/power_flask_25.png";
        return "res://assets/flasks/power_flask_0.png";
    }

    private void OnBgGuiInput(InputEvent @event)
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
        // Window dragging
        if (_isDragging && @event is InputEventMouseMotion)
        {
            Position = GetGlobalMousePosition() - _dragOffset;
            var vpSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, vpSize.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, vpSize.Y - Size.Y))
            );
        }

        // Item dragging
        if (_isDraggingItem && @event is InputEventMouseMotion && _draggedItemControl != null)
        {
            Vector2 localMouse = _itemsArea.GetLocalMousePosition();
            Vector2 newPos = localMouse - _itemDragOffset;

            if ((newPos - _itemDragStartPos).Length() > 5f)
            {
                _hasDraggedItem = true;
            }

            float maxX = Mathf.Max(0f, _itemsArea.Size.X - _draggedItemControl.Size.X);
            float maxY = Mathf.Max(0f, _itemsArea.Size.Y - _draggedItemControl.Size.Y);
            newPos = new Vector2(
                Mathf.Clamp(newPos.X, 0, maxX),
                Mathf.Clamp(newPos.Y, 0, maxY)
            );
            _draggedItemControl.Position = newPos;
        }

        // Item drop or click release
        if (_isDraggingItem && @event is InputEventMouseButton mouseBtn && !mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (_draggedItemControl != null && _draggedItemName != null && _activeChestId != null)
            {
                Vector2 globalMouse = GetGlobalMousePosition();
                var pouchWin = GetParent().GetNodeOrNull<PouchWindow>("PouchWindow");
                bool droppedOnPouch = pouchWin != null && pouchWin.Visible && pouchWin.GetGlobalRect().HasPoint(globalMouse);

                if (droppedOnPouch || !_hasDraggedItem)
                {
                    // Dropped onto pouch or quick click without dragging: transfer 1 item to pouch
                    GameState.Instance.TransferItemChestToPouch(_activeChestId, _draggedItemName, 1);
                }
                else
                {
                    // Dragged and released: save new position in chest
                    GameState.Instance.UpdateChestItemPosition(_activeChestId, _draggedItemName, _draggedItemControl.Position);
                }
            }

            _isDraggingItem = false;
            _hasDraggedItem = false;
            _draggedItemControl = null;
            _draggedItemName = null;
        }
    }

    private static void ApplySmallButtonStyle(Button btn, Color baseBg)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = baseBg,
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
            BgColor = baseBg.Lightened(0.18f),
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
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f));
        btn.AddThemeFontSizeOverride("font_size", 12);
    }

    private static void ApplyCloseButtonStyle(Button btn)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.20f, 0.08f, 0.08f, 0.85f),
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
        btn.AddThemeFontSizeOverride("font_size", 14);
    }
}
