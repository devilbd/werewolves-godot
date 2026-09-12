using System;
using System.Collections.Generic;
using Godot;
using Werewolves.Core;

namespace Werewolves.UI;

public partial class PouchWindow : Control
{
    private TextureRect _bagTexture = null!;
    private Control _itemsArea = null!;
    private Button _closeButton = null!;

    // Dragging window state
    private bool _isDraggingWindow = false;
    private Vector2 _windowDragOffset = Vector2.Zero;

    // Dragging item state
    private bool _isDraggingItem = false;
    private Control? _draggedItemControl = null;
    private string? _draggedItemName = null;
    private Vector2 _itemDragOffset = Vector2.Zero;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(450, 450);
        Size = new Vector2(450, 450);

        _bagTexture = GetNodeOrNull<TextureRect>("BagTexture");
        if (_bagTexture == null)
        {
            _bagTexture = new TextureRect
            {
                Name = "BagTexture",
                Texture = GD.Load<Texture2D>("res://assets/pouch.png"),
                CustomMinimumSize = new Vector2(450, 450),
                Size = new Vector2(450, 450),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Stop
            };
            AddChild(_bagTexture);
        }
        else if (_bagTexture.Texture == null)
        {
            _bagTexture.Texture = GD.Load<Texture2D>("res://assets/pouch.png");
        }

        _closeButton = GetNodeOrNull<Button>("CloseButton");
        if (_closeButton == null)
        {
            _closeButton = new Button
            {
                Name = "CloseButton",
                Text = "X",
                Position = new Vector2(285, 105),
                Size = new Vector2(32, 32),
                Flat = true
            };
            _closeButton.AddThemeFontSizeOverride("font_size", 18);
            _closeButton.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.1f));
            _closeButton.AddThemeColorOverride("font_hover_color", Colors.White);
            AddChild(_closeButton);
        }

        _itemsArea = GetNodeOrNull<Control>("ItemsArea");
        if (_itemsArea == null)
        {
            _itemsArea = new Control
            {
                Name = "ItemsArea",
                Position = new Vector2(145, 135),
                CustomMinimumSize = new Vector2(170, 165),
                Size = new Vector2(170, 165),
                ClipContents = true,
                MouseFilter = MouseFilterEnum.Pass
            };
            AddChild(_itemsArea);
        }

        GetNodeOrNull<Node>("Grabber")?.QueueFree();

        _closeButton.Pressed += () => GameState.Instance.TogglePouch();
        _bagTexture.GuiInput += OnBagGuiInput;

        Visible = false;
        GameState.Instance.OnPouchToggled += (visible) =>
        {
            Visible = visible;
            if (visible) RefreshItems();
        };

        GameState.Instance.OnPouchChanged += RefreshItems;

        RefreshItems();
    }

    private void OnBagGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _isDraggingWindow = true;
                _windowDragOffset = GetGlobalMousePosition() - Position;
            }
            else
            {
                _isDraggingWindow = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDraggingWindow && @event is InputEventMouseMotion)
        {
            Position = GetGlobalMousePosition() - _windowDragOffset;
            var viewportSize = GetViewportRect().Size;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, viewportSize.X - Size.X),
                Mathf.Clamp(Position.Y, 0, viewportSize.Y - Size.Y)
            );
        }

        if (_isDraggingItem && @event is InputEventMouseMotion && _draggedItemControl != null)
        {
            Vector2 localMouse = _itemsArea.GetLocalMousePosition();
            Vector2 newPos = localMouse - _itemDragOffset;
            newPos = new Vector2(
                Mathf.Clamp(newPos.X, 0, _itemsArea.Size.X - 44),
                Mathf.Clamp(newPos.Y, 0, _itemsArea.Size.Y - 44)
            );
            _draggedItemControl.Position = newPos;
        }

        if (_isDraggingItem && @event is InputEventMouseButton mouseBtn && !mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (_draggedItemControl != null && _draggedItemName != null)
            {
                GameState.Instance.UpdatePouchItemPosition(_draggedItemName, _draggedItemControl.Position);
            }
            _isDraggingItem = false;
            _draggedItemControl = null;
            _draggedItemName = null;
        }
    }

    public void RefreshItems()
    {
        foreach (Node child in _itemsArea.GetChildren())
        {
            child.QueueFree();
        }

        int itemIndex = 0;
        foreach (var kvp in GameState.Instance.PouchItems)
        {
            string itemName = kvp.Key;
            PouchItemData item = kvp.Value;
            if (item.Count <= 0) continue;

            Vector2 itemPos = (item.PosX == 0f && item.PosY == 0f)
                ? GetDefaultSlotPosition(itemIndex)
                : new Vector2(item.PosX, item.PosY);

            var itemContainer = new Control
            {
                Position = itemPos,
                CustomMinimumSize = new Vector2(44, 44),
                Size = new Vector2(44, 44),
                MouseFilter = MouseFilterEnum.Stop
            };

            string iconPath = itemName switch
            {
                "Logs" => "res://assets/logs_collected_o.png",
                "Stones" => "res://assets/rock_stones_loot_collected_o.png",
                "Meat" => "res://assets/meat_collected_o.png",
                _ => "res://assets/logs_collected_o.png"
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
                Text = item.Count.ToString(),
                Position = new Vector2(24, 24),
                Size = new Vector2(20, 16),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            countLabel.AddThemeFontSizeOverride("font_size", 12);
            countLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.4f));
            itemContainer.AddChild(countLabel);

            // Item dragging
            string captureItemName = itemName;
            itemContainer.GuiInput += (ev) =>
            {
                if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                {
                    _isDraggingItem = true;
                    _draggedItemControl = itemContainer;
                    _draggedItemName = captureItemName;
                    _itemDragOffset = itemContainer.GetLocalMousePosition();
                    GetViewport().SetInputAsHandled();
                }
            };

            _itemsArea.AddChild(itemContainer);
            itemIndex++;
        }
    }

    private static Vector2 GetDefaultSlotPosition(int index) => index switch
    {
        0 => new Vector2(10, 15),
        1 => new Vector2(65, 15),
        2 => new Vector2(120, 15),
        3 => new Vector2(10, 75),
        4 => new Vector2(65, 75),
        5 => new Vector2(120, 75),
        _ => new Vector2(10, 15)
    };
}
