using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Play
{
    public class MenuNode
    {
        public enum Type
        {
            Category,
            Leaf
        }

        public String Name;
        public Type NodeType = Type.Leaf;
        public CommandMenuItem MenuItem;
        public List<MenuNode> Children = new List<MenuNode>();
        public Widget GuiTag = null;
    }

    public class CommandTray : Widget
    {
        public WorldManager World;
        public List<CommandMenuItem> Commands;
        public MenuNode MenuRoot;
        public List<String> ActiveMenu = new List<string>();
        public Widget CommandWidget;
        public Widget PopupFrame;
        public ScrollingCommandTray CommandGrid;
        private Gui.Widgets.HorizontalScrollBar ScrollBar = null;
        public Rectangle ToolPopupZone = new Rectangle(0, 0, 0, 0);
        
        public void PushCategory(String Category)
        {
            ActiveMenu.Add(Category);
            ScrollBar.ScrollPosition = 0;
        }

        public void PopCategory()
        {
            if (ActiveMenu.Count > 0)
                ActiveMenu.RemoveAt(ActiveMenu.Count - 1);
            ClearCommandWidget();
        }

        public MaybeNull<MenuNode> FindNode(NeverNull<MenuNode> Node, List<String> Path, int Offset)
        {
            if (Offset < 0)
                return null;

            if (Offset >= Path.Count)
                return Node.Value;

            foreach (var child in Node.Value.Children)
                if (child.Name == Path[Offset])
                    return FindNode(child, Path, Offset + 1);

            return null;
        }

        public MaybeNull<MenuNode> CreateCategory(NeverNull<MenuNode> Node, List<String> Path, int Offset)
        {
            if (Offset < 0)
                return null;

            if (Offset >= Path.Count)
                return Node.Value;

            var child = Node.Value.Children.FirstOrDefault(c => c.Name == Path[Offset]);
            if (child == null)
            {
                child = new MenuNode
                {
                    Name = Path[Offset],
                    NodeType = MenuNode.Type.Category
                };

                Node.Value.Children.Add(child);
            }

            return CreateCategory(child, Path, Offset + 1);
        }

        private MaybeNull<MenuNode> GetFirstLeaf(NeverNull<MenuNode> Node)
        {
            if (Node.Value.NodeType == MenuNode.Type.Leaf)
                return Node.Value;
            foreach (var child in Node.Value.Children)
                if (GetFirstLeaf(child).HasValue(out var leaf))
                    return leaf;
            return null;
        }

        private void SetCategoryIcons(NeverNull<MenuNode> Node, String FullPath)
        {
            if (Node.Value.NodeType == MenuNode.Type.Leaf)
                return;
            var myPath = String.IsNullOrEmpty(FullPath) ? Node.Value.Name : (FullPath + "." + Node.Value.Name);

            foreach (var child in Node.Value.Children)
                SetCategoryIcons(child, myPath);

            if (Library.GetCategoryIcon(myPath).HasValue(out var presetIcon) && presetIcon.DynamicIcon != null)
            {
                Node.Value.MenuItem = new CommandMenuItem
                {
                    ID = presetIcon.Category,
                    DisplayName = presetIcon.Label,
                    Icon = presetIcon.DynamicIcon,
                    Tooltip = presetIcon.Tooltip,
                    EnableHotkeys = presetIcon.EnableHotkeys
                };
            }
            else
            {
                var firstLeaf = GetFirstLeaf(Node);
                if (firstLeaf.HasValue(out var leaf))
                    Node.Value.MenuItem = new CommandMenuItem
                    {
                        DisplayName = Node.Value.Name,
                        Icon = leaf.MenuItem.Icon,
                        OldStyleIcon = leaf.MenuItem.OldStyleIcon,
                        Tooltip = Node.Value.Name,
                        EnableHotkeys = true
                    };
            }
        }

        private void BuildMenuTree(List<CommandMenuItem> Commands)
        {
            MenuRoot = new MenuNode { NodeType = MenuNode.Type.Category };

            foreach (var item in Commands)
            {
                if (item.ID.Count == 0) continue; // Malformed item.

                if (item.HoverWidget != null)
                    Root.ConstructWidget(item.HoverWidget);

                var newNode = new MenuNode
                {
                    NodeType = MenuNode.Type.Leaf,
                    Name = item.ID[item.ID.Count - 1],
                    MenuItem = item
                };

                var categoryID = new List<String>(item.ID);
                categoryID.RemoveAt(categoryID.Count - 1);
                var existingNode = FindNode(MenuRoot, item.ID, 0);
                if (existingNode.HasValue(out var categoryNode))
                    categoryNode.Children.Add(newNode);
                else
                {
                    if (CreateCategory(MenuRoot, categoryID, 0).HasValue(out categoryNode))
                        categoryNode.Children.Add(newNode);
                }
            }

        }

        private void VisitTree(NeverNull<MenuNode> Node, Action<MenuNode> Func)
        {
            Func(Node);
            foreach (var child in Node.Value.Children)
                VisitTree(child, Func);
        }

        public override void Construct()
        {
            Padding = new Margin(2, 2, 2, 2);

            base.Construct();

            Commands = CommandMenuItemEnumerator.EnumerateCommandMenuItems(World).ToList();
            foreach (var command in Commands)
                if (command.HoverWidget != null) Root.ConstructWidget(command.HoverWidget);

            BuildMenuTree(Commands);
            SetCategoryIcons(MenuRoot, "");
            VisitTree(MenuRoot, (node) =>
            {
                node.GuiTag = Root.ConstructWidget(new CommandMenuItemIcon
                {
                    Command = node.MenuItem,
                    OnClick = (sender, args) =>
                    {
                        if (node.NodeType == MenuNode.Type.Category)
                        {
                            PushCategory(node.Name);
                            RefreshItems();
                        }
                        else if (node.NodeType == MenuNode.Type.Leaf)
                        {
                            if (node.MenuItem.HoverWidget != null)
                            {
                                ClearCommandWidget();
                                PopupFrame.Hidden = false;
                                CommandWidget = node.MenuItem.HoverWidget;
                                CommandWidget.AutoLayout = AutoLayout.DockFill;
                                PopupFrame.AddChild(CommandWidget);

                                var midPoint = sender.Rect.X + (sender.Rect.Width / 2);

                                var popupBorder = Root.GetTileSheet(PopupFrame.Border);
                                var bestSize = CommandWidget.GetBestSize();
                                CommandWidget.Rect = new Rectangle(0, 0, bestSize.X, bestSize.Y);

                                PopupFrame.Rect.X = midPoint - (CommandWidget.Rect.Width / 2) - popupBorder.TileWidth;
                                PopupFrame.Rect.Y = Parent.Rect.Y - CommandWidget.Rect.Height - (popupBorder.TileHeight * 2);
                                PopupFrame.Rect.Width = CommandWidget.Rect.Width + (popupBorder.TileWidth * 2);
                                PopupFrame.Rect.Height = CommandWidget.Rect.Height + (popupBorder.TileHeight * 2);

                                if (PopupFrame.Rect.X < ToolPopupZone.X)
                                    PopupFrame.Rect.X = ToolPopupZone.X;

                                if (PopupFrame.Rect.Right > ToolPopupZone.Right)
                                    PopupFrame.Rect.X = ToolPopupZone.Right - PopupFrame.Rect.Width;

                                PopupFrame.Layout();
                                Children.Add(PopupFrame);
                                Invalidate();
                            }

                            if (sender is CommandMenuItemIcon icon && icon.Enabled)
                                node.MenuItem.OnClick?.Invoke(node.MenuItem, args);
                        }
                    }
                });
            });

            ScrollBar = AddChild(new Gui.Widgets.HorizontalScrollBar
            {
                AutoLayout = AutoLayout.DockBottom,
                OnScrollValueChanged = (sender) =>
                {
                    CommandGrid.ScrollPosition = ScrollBar.ScrollPosition;
                    CommandGrid.Layout();
                }
            }) as Gui.Widgets.HorizontalScrollBar;


            AddChild(new CommandMenuItemIcon
            {
                Command = new CommandMenuItem
                {
                    OldStyleIcon = new TileReference("tool-icons", 11),
                    Tooltip = "Go Back"
                },
                OnClick = (sender, args) =>
                {
                    PopCategory();
                    ClearCommandWidget();
                    RefreshItems();
                },
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(32, 32)
            });

            PopupFrame = Root.ConstructWidget(new Widget
            {
                Border = "border-button",
                Hidden = true
            });

            CommandGrid = AddChild(new ScrollingCommandTray
            {
                AutoLayout = AutoLayout.DockFill,
                ItemSize = new Point(40, 40)
            }) as ScrollingCommandTray;

            OnLayout = (sender) => RefreshItems();

            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                foreach (var item in CommandGrid.Children)
                    if (item is CommandMenuItemIcon icon)
                        icon.UpdateAvailability();
                if (CommandWidget != null)
                    Root.SafeCall(CommandWidget.OnUpdate, CommandWidget, time);
            };
        }

        public void RefreshItems()
        {
            CommandGrid.ClearContents();
            if (FindNode(MenuRoot, ActiveMenu, 0).HasValue(out var menu))
            {
                foreach (var item in menu.Children)
                    CommandGrid.AddChild(item.GuiTag);

                if (menu.MenuItem.EnableHotkeys)
                    Hotkeys.AssignHotKeys(menu.Children, (child, key) =>
                    {
                        if (child.GuiTag is CommandMenuItemIcon icon)
                        {
                            icon.HotkeyValue = key;
                            icon.DrawHotkey = true;
                        }
                    });
                else
                    foreach (var child in menu.Children)
                        if (child.GuiTag is CommandMenuItemIcon icon)
                            icon.DrawHotkey = false;

                var itemsVisible = CommandGrid.GetItemsVisible();
                if (itemsVisible >= CommandGrid.Children.Count)
                {
                    CommandGrid.ScrollPosition = 0;
                    ScrollBar.Hidden = true;
                }
                else
                {
                    ScrollBar.SupressOnScroll = true; // Prevents infinite loop.
                    ScrollBar.ScrollArea = CommandGrid.Children.Count - CommandGrid.GetItemsVisible() + 1;
                    ScrollBar.Hidden = false;
                }

                CommandGrid.Layout();
            }
            else
            {
                ActiveMenu = new List<string>();
                RefreshItems();
            }
        }

        public void HandleHotkeyPress(Keys Key)
        {
            foreach (var child in CommandGrid.Children)
                if (child is CommandMenuItemIcon icon && icon.DrawHotkey == true && icon.HotkeyValue == Key)
                {
                    Root.SafeCall(child.OnClick, child, new InputEventArgs { X = child.Rect.X, Y = child.Rect.Y });
                    return;
                }
        }

        public void ClearCommandWidget()
        {
            PopupFrame.Children.Clear();
            PopupFrame.Hidden = true;
            PopupFrame.Invalidate();
            Children.Remove(PopupFrame);
        }
    }
}
