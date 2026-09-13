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
        GameState.Instance.OnPouchToggled += OnGameStatePouchToggled;
        GameState.Instance.OnPouchChanged += OnGameStatePouchChanged;

        RefreshItems();
    }

    private void OnGameStatePouchToggled(bool visible)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Visible = visible;
        if (visible) RefreshItems();
    }

    private void OnGameStatePouchChanged()
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        RefreshItems();
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPouchToggled -= OnGameStatePouchToggled;
            GameState.Instance.OnPouchChanged -= OnGameStatePouchChanged;
        }
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
            float maxX = Mathf.Max(0f, _itemsArea.Size.X - _draggedItemControl.Size.X);
            float maxY = Mathf.Max(0f, _itemsArea.Size.Y - _draggedItemControl.Size.Y);
            newPos = new Vector2(
                Mathf.Clamp(newPos.X, 0, maxX),
                Mathf.Clamp(newPos.Y, 0, maxY)
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
        if (!GodotObject.IsInstanceValid(this) || _itemsArea == null || !GodotObject.IsInstanceValid(_itemsArea))
            return;

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

            float maxX = Mathf.Max(0f, _itemsArea.Size.X - 44f);
            float maxY = Mathf.Max(0f, _itemsArea.Size.Y - 44f);

            bool isUnset = item.PosX == 0f && item.PosY == 0f;
            bool isOutOfBounds = item.PosX < 0f || item.PosX > maxX || item.PosY < 0f || item.PosY > maxY;

            Vector2 defaultPos = GetDefaultSlotPosition(itemIndex);
            Vector2 itemPos = (isUnset || isOutOfBounds)
                ? defaultPos
                : new Vector2(item.PosX, item.PosY);

            if (isUnset || isOutOfBounds)
            {
                item.PosX = itemPos.X;
                item.PosY = itemPos.Y;
            }

            bool isBloodFlask = itemName.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase);

            string iconPath = isBloodFlask
                ? GetFlaskTexturePath(item.BloodPercent)
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

            string tooltip;
            if (isBloodFlask)
            {
                int hp = (int)Math.Round(25f * (item.BloodPercent / 100f));
                tooltip = $"Blood Flask ({item.BloodPercent}%)\n(Right-click to Drink: +{hp} HP, +{hp} Power)";
            }
            else if (itemName is "Meat")
            {
                tooltip = $"Meat ({item.Count})\n(Right-click to Eat: +20 HP, +10 Power)";
            }
            else if (itemName is "EmptyFlask" or "Empty Flask" or "Flask")
            {
                tooltip = item.Count > 1 ? $"Empty Flasks ({item.Count})" : "Empty Flask";
            }
            else if (itemName is "GoldCoins" or "Gold Coins" or "Gold")
            {
                tooltip = $"Gold Coins ({item.Count})";
            }
            else
            {
                tooltip = $"{itemName} ({item.Count})";
            }

            if (GameState.Instance.IsChestInventoryOpen)
            {
                if (isBloodFlask)
                {
                    tooltip += "\n(Shift+Click or Right-Click: Deposit into Chest)";
                }
                else if (item.Count > 1)
                {
                    tooltip += "\n(Right-Click: Deposit 1 | Shift+Click: Choose amount to deposit)";
                }
                else
                {
                    tooltip += "\n(Right-Click/Shift+Click: Deposit into Chest)";
                }
            }

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

            // Item dragging, right-click consumption & chest deposit
            string captureItemName = itemName;
            int captureCount = item.Count;
            Texture2D? captureIcon = iconTex.Texture;

            itemContainer.GuiInput += (ev) =>
            {
                if (ev is InputEventMouseButton mb && mb.Pressed)
                {
                    // If chest inventory is currently open, allow direct depositing
                    if (GameState.Instance.IsChestInventoryOpen && GameState.Instance.ActiveChestId != null)
                    {
                        bool isShift = Input.IsKeyPressed(Key.Shift);
                        bool isConsumable = captureItemName is "Meat" || captureItemName.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase);

                        if (isShift && captureCount > 1)
                        {
                            GetViewport().SetInputAsHandled();
                            string activeChest = GameState.Instance.ActiveChestId;
                            string displayName = isBloodFlask ? $"Blood Flask ({item.BloodPercent}%)" : captureItemName;
                            ItemSplitModal.Instance?.Open(captureItemName, displayName, captureIcon, captureCount, "Deposit", (amount) =>
                            {
                                GameState.Instance.TransferItemPouchToChest(activeChest, captureItemName, amount);
                            });
                            return;
                        }

                        if ((isShift && captureCount <= 1) || (mb.ButtonIndex == MouseButton.Right && !isConsumable))
                        {
                            GetViewport().SetInputAsHandled();
                            GameState.Instance.TransferItemPouchToChest(GameState.Instance.ActiveChestId, captureItemName, 1);
                            return;
                        }
                    }

                    if (mb.ButtonIndex == MouseButton.Left)
                    {
                        _isDraggingItem = true;
                        _draggedItemControl = itemContainer;
                        _draggedItemName = captureItemName;
                        _itemDragOffset = itemContainer.GetLocalMousePosition();
                        GetViewport().SetInputAsHandled();
                    }
                    else if (mb.ButtonIndex == MouseButton.Right)
                    {
                        GetViewport().SetInputAsHandled();
                        if (captureItemName is "Meat")
                        {
                            GameState.Instance.EatMeat();
                        }
                        else if (captureItemName.StartsWith("BloodFlask", StringComparison.OrdinalIgnoreCase))
                        {
                            GameState.Instance.DrinkBloodFlask(captureItemName);
                        }
                    }
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
        _ => new Vector2(10 + (index % 3) * 55, 15 + ((index / 3) % 2) * 60)
    };

    public static string GetFlaskTexturePath(int percent)
    {
        if (percent <= 0) return "res://assets/flasks/blood_flask_0.png";
        if (percent <= 37) return "res://assets/flasks/blood_flask_25.png";
        if (percent <= 62) return "res://assets/flasks/blood_flask_50.png";
        if (percent <= 87) return "res://assets/flasks/blood_flask_75.png";
        return "res://assets/flasks/blood_flask_100.png";
    }
}
