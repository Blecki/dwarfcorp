using System.Collections.Generic;
using System.Linq;
using System;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class PaginatedChooserState : GameState
    {
        public class ChooserItem
        {
            public String Path;
            public Texture2D Screenshot;
            public string Valid;
            public TimeSpan Age;
            public string Name;

            public enum ScreenshotStatusEnum
            {
                Unloaded,
                NoneFound,
                Loaded
            }

            public ScreenshotStatusEnum ScreenshotStatus = ScreenshotStatusEnum.Unloaded;
        }

        public class ChooserWidget : Widget
        {
            public ChooserItem Item { get; set; }
            public Widget ScreenshotWidget { get; set; }
            public Widget DeleteButton { get; set; }
            public Widget LoadButton { get; set; }

            public override void Construct()
            {
                MinimumSize = new Point(1024, 128 + 6);
                ScreenshotWidget = AddChild(new Widget()
                {
                    MinimumSize = new Point(128, 128),
                    AutoLayout = AutoLayout.DockLeft,
                    Text = Item.Screenshot == null ? "No image" : "",
                    Font = "font8",
                    Border = "border-one"
                });

                var rightContent = AddChild(new Widget()
                {
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(1024, 128),
                    InteriorMargin = new Margin(10, 10, 10, 10)
                });

                var title = rightContent.AddChild(new Widget()
                {
                    Text = Item.Name,
                    Font = "font16",
                    AutoLayout = AutoLayout.DockTop,
                });

                rightContent.AddChild(new Widget()
                {
                    Text = TextGenerator.AgeToString(Item.Age),
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop
                });

                if (!String.IsNullOrEmpty(Item.Valid))
                {
                    rightContent.AddChild(new Widget()
                    {
                        Text = String.Format("(Invalid save: {0})", Item.Valid),
                        Font = "font8",
                        AutoLayout = AutoLayout.DockTop
                    });
                }

                var buttonContainer = rightContent.AddChild(new Widget()
                {
                    MinimumSize = new Point(64, 32),
                    AutoLayout = AutoLayout.DockBottom
                });

                if (String.IsNullOrEmpty(Item.Valid))
                {
                    LoadButton = buttonContainer.AddChild(new Gui.Widgets.Button()
                    {
                        Text = "Load",
                        AutoLayout = AutoLayout.DockLeft,
                        Border = "border-thin",
                        InteriorMargin = new Margin(0, 0, 3, 3),
                        Tooltip = "Click to load this save file"
                    });
                }

                DeleteButton = buttonContainer.AddChild(new Gui.Widgets.Button()
                {
                    Text = "Delete",
                    AutoLayout = AutoLayout.DockLeft,
                    Border = "border-thin",
                    InteriorMargin = new Margin(0, 0, 3, 3),
                    Tooltip = "Click to delete this save file"
                });

                InteriorMargin = new Margin(3, 3, 3, 3);
                base.Construct();
            }
        }


        private Gui.Root GuiRoot;
        private List<ChooserItem> Items = new List<ChooserItem>();
        private Gui.Widgets.WidgetListView Grid;
        private bool NeedsRefresh = true;
        private int ItemSelected = 0;
        private Gui.Widget BottomBar;

        public Func<List<global::System.IO.DirectoryInfo>> ItemSource;
        public String NoItemsText = "Nothing to display.";
        public String ProceedButtonText = "Okay";
        public Action<String> OnProceedClicked;
        public Func<String, Texture2D> ScreenshotSource;
        public Func<String, String> ValidateItem;
        public Func<String, String> GetItemName;
        public String InvalidItemText;


        public PaginatedChooserState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, StateManager)
        { this.EnableScreensaver = true; }

        public override void OnEnter()
        {
            if (ItemSource != null)
                foreach (var path in ItemSource())
                    Items.Add(new ChooserItem { Name = GetItemName(path.FullName), Path = path.FullName, Valid = ValidateItem == null ? "" : ValidateItem(path.FullName), Age = DateTime.Now - path.LastWriteTime });

            foreach(var item in Items)
            {
                item.Screenshot = ScreenshotSource(item.Path);
            }

            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            GuiRoot.RootItem.Transparent = false;
            //GuiRoot.RootItem.Background = new Gui.TileReference("basic", 0);
            GuiRoot.RootItem.InteriorMargin = new Gui.Margin(16, 16, 32, 32);

            // CONSTRUCT GUI HERE...
            BottomBar = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                AutoLayout = Gui.AutoLayout.DockBottom,
                MinimumSize = new Point(0, 60),
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Background = new Gui.TileReference("basic", 0),
                Font = "font10",
                InteriorMargin = new Gui.Margin(10, 10, 10, 10)
            });

            if (Items.Count == 0)
                BottomBar.Text = NoItemsText;


            BottomBar.AddChild(new Gui.Widgets.Button
            {
                AutoLayout = Gui.AutoLayout.FloatBottomLeft,
                Border = "border-button",
                Text = "< Back",
                Tooltip = "Back to the main screen",
                OnClick = (sender, args) =>
                {
                    StateManager.PopState();
                }
            });

            Grid = GuiRoot.RootItem.AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                Border = "border-one",
                Font = "font10",
                InteriorMargin = new Gui.Margin(32, 0, 0, 0),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Top,
                SelectedItemBackgroundColor = Color.LightBlue.ToVector4(),
                SelectedItemForegroundColor = Color.Black.ToVector4()
            }) as Gui.Widgets.WidgetListView;

            GuiRoot.RootItem.Layout();

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            if (NeedsRefresh)
            {
                NeedsRefresh = false;
                Grid.ClearItems();
                int i = 0;
                foreach(var item in Items)
                {
                    var itemWidget = new ChooserWidget
                    {
                        Item = item,
                        Background = new TileReference("basic", 0),
                        BackgroundColor = i % 2 == 0 ? new Vector4(0, 0, 0, 0.1f) : new Vector4(0, 0, 0, 0.2f)
                    };

                    Grid.AddItem(itemWidget);

                    itemWidget.DeleteButton.OnClick = (sender, args) =>
                    {
                        var confirm = GuiRoot.ConstructWidget(new Gui.Widgets.Confirm
                        {
                            OkayText = "Delete",
                            CancelText = "Keep",
                            Text = "Are you sure you want to delete this?",
                            OnClose = (s) =>
                            {
                                if ((s as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                {
                                    var selectedItem = itemWidget.Item;
                                    Items.Remove(selectedItem);
                                    try
                                    {
                                        global::System.IO.Directory.Delete(selectedItem.Path, true);
                                    }
                                    catch (Exception e)
                                    {
                                        GuiRoot.ShowModalPopup(new Gui.Widgets.Confirm()
                                        {
                                            OkayText = "Ok",
                                            CancelText = "",
                                            Text = e.Message
                                        });

                                    }
                                    NeedsRefresh = true;
                                }
                            }
                        });
                        GuiRoot.ShowModalPopup(confirm);
                    };

                    if (itemWidget.LoadButton != null)
                        itemWidget.LoadButton.OnClick = (sender, args) =>
                        {
                            if (String.IsNullOrEmpty(itemWidget.Item.Valid) && OnProceedClicked != null)
                                OnProceedClicked(itemWidget.Item.Path);
                        };

                    i++;
                }

                Grid.OnSelectedIndexChanged = (widget) =>
                {
                    this.ItemSelected = Grid.SelectedIndex;
                    NeedsRefresh = true;
                };
                if (Grid.SelectedIndex > Items.Count - 1 || Grid.SelectedIndex < 0)
                {
                    Grid.SelectedIndex = 0;
                }
                ItemSelected = Grid.SelectedIndex;
                if (Items.Count > 0)
                {
                    var directoryTime = global::System.IO.Directory.GetLastWriteTime(Items[ItemSelected].Path);

                    BottomBar.Text = Items[ItemSelected].Path;

                    if (!String.IsNullOrEmpty(Items[ItemSelected].Valid))
                        BottomBar.Text += "\n" + Items[ItemSelected].Valid;
                    else
                        BottomBar.Text += "\n" + directoryTime.ToShortDateString() + " " + directoryTime.ToShortTimeString();
                }
                else
                {
                    BottomBar.Text = NoItemsText;
                }

            }

            GuiRoot.Update(gameTime.ToRealTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            var mouse = GuiRoot.MousePointer;
            GuiRoot.MousePointer = null;
            GuiRoot.MouseOverlaySheet = null;

            GuiRoot.Draw();

            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (item.ScreenshotStatus == ChooserItem.ScreenshotStatusEnum.Unloaded)
                {
                    item.Screenshot = (ScreenshotSource == null ? null : ScreenshotSource(item.Path));
                    item.ScreenshotStatus = (item.Screenshot == null ? ChooserItem.ScreenshotStatusEnum.NoneFound : ChooserItem.ScreenshotStatusEnum.Loaded);
                }

                if (item.ScreenshotStatus == ChooserItem.ScreenshotStatusEnum.Loaded)
                {
                    if (i < Grid.Children.Count - 1)
                    {
                        var widget = (Grid.GetChild(i + 1) as ChooserWidget);
                        var rect = widget.ScreenshotWidget.Rect;
                        if (!widget.Hidden)
                            GuiRoot.DrawQuad(rect, item.Screenshot);
                    }
                }
            }

            GuiRoot.RedrawPopups(); // This hack sucks.
            GuiRoot.MousePointer = mouse;
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }

}