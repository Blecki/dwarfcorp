using System.Collections.Generic;
using System.Linq;
using System;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Gum;

namespace DwarfCorp.GameStates
{
    public class PaginatedChooserState : GameState
    {
        public class ChooserItem
        {
            public String Path;
            public Texture2D Screenshot;
            
            public enum ScreenshotStatusEnum
            {
                Unloaded,
                NoneFound,
                Loaded
            }

            public ScreenshotStatusEnum ScreenshotStatus = ScreenshotStatusEnum.Unloaded;
        }

        private Gum.Root GuiRoot;
        private List<ChooserItem> Items = new List<ChooserItem>();
        private NewGui.GridPanel Grid;
        private int PreviewOffset = 0;
        private bool NeedsRefresh = true;
        private int ItemSelected = 0;
        private Gum.Widget BottomBar;

        public Func<List<String>> ItemSource;
        public String NoItemsText = "Nothing to display.";
        public String ProceedButtonText = "Okay";
        public Action<String> OnProceedClicked;
        public Func<String, Texture2D> ScreenshotSource;

        public PaginatedChooserState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "GuiStateTemplate", StateManager)
        { }

        public override void OnEnter()
        {
            if (ItemSource != null)
                foreach (var path in ItemSource())
                    Items.Add(new ChooserItem { Path = path });

            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
            GuiRoot.SetMouseOverlay(null, 0);
            GuiRoot.RootItem.Transparent = false;
            GuiRoot.RootItem.Background = new Gum.TileReference("basic", 0);
            GuiRoot.RootItem.InteriorMargin = new Gum.Margin(16, 16, 16, 16);

            // CONSTRUCT GUI HERE...
            BottomBar = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockBottom,
                MinimumSize = new Point(0, 60),
                TextHorizontalAlign = Gum.HorizontalAlign.Center
            });

            if (Items.Count == 0)
                BottomBar.Text = NoItemsText;

            if (Items.Count > 0)
            {
                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatBottomRight,
                    Border = "border-button",
                    Text = ProceedButtonText,
                    OnClick = (sender, args) =>
                    {
                        var selectedItem = Items[PreviewOffset + ItemSelected];
                        if (OnProceedClicked != null) OnProceedClicked(selectedItem.Path);
                    }
                });

                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatTopRight,
                    Border = "border-button",
                    Text = "Next",
                    OnClick = (sender, args) =>
                    {
                        if (PreviewOffset + Grid.ItemsThatFit < Items.Count)
                        {
                            NeedsRefresh = true;
                            PreviewOffset += Grid.ItemsThatFit;
                        }
                    }
                });

                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.FloatTopLeft,
                    Border = "border-button",
                    Text = "Prev",
                    OnClick = (sender, args) =>
                    {
                        if (PreviewOffset > 0)
                        {
                            NeedsRefresh = true;
                            PreviewOffset -= Grid.ItemsThatFit;
                        }
                    }
                });

                BottomBar.AddChild(new Gum.Widget
                {
                    AutoLayout = AutoLayout.FloatBottom,
                    Border = "border-button",
                    Text = "Delete",
                    OnClick = (sender, args) =>
                    {
                        var confirm = GuiRoot.ConstructWidget(new NewGui.Confirm
                        {
                            OkayText = "Delete",
                            CancelText = "Keep",
                            Text = "Are you sure you want to delete this?",
                            OnClose = (s) =>
                            {
                                if ((s as NewGui.Confirm).DialogResult == NewGui.Confirm.Result.OKAY)
                                {
                                    var selectedItem = Items[PreviewOffset + ItemSelected];
                                    Items.Remove(selectedItem);
                                    System.IO.Directory.Delete(selectedItem.Path, true);
                                    NeedsRefresh = true;
                                }
                            }
                        });
                        GuiRoot.ShowPopup(confirm, Root.PopupExclusivity.AddToStack);
                    }
                });
            }

            BottomBar.AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.FloatBottomLeft,
                Border = "border-button",
                Text = "Back",
                OnClick = (sender, args) =>
                {
                    StateManager.PopState();
                }
            });

            Grid = GuiRoot.RootItem.AddChild(new NewGui.GridPanel
            {
                ItemSize = new Point(128, 128),
                ItemSpacing = new Point(8, 8),
                AutoLayout = Gum.AutoLayout.DockFill,
                Border = "border-one",
                InteriorMargin = new Gum.Margin(32, 0, 0, 0),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Top
            }) as NewGui.GridPanel;

            GuiRoot.RootItem.Layout();

            if (Items.Count > 0)
            {
                var gridSpaces = Grid.ItemsThatFit;
                for (var i = 0; i < gridSpaces; ++i)
                {
                    var lambda_index = i;
                    Grid.AddChild(new Gum.Widget
                    {
                        Border = "border-one",
                        Text = "No Image",
                        TextHorizontalAlign = HorizontalAlign.Center,
                        TextVerticalAlign = VerticalAlign.Center,
                        OnClick = (sender, args) =>
                        {
                            ItemSelected = lambda_index;
                            NeedsRefresh = true;
                        }
                    });
                }

                GuiRoot.RootItem.Layout();
            }

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

            if (NeedsRefresh && Items.Count > 0)
            {
                NeedsRefresh = false;

                if (PreviewOffset >= Items.Count && Items.Count > 0) // We're looking at an empty last page...
                    PreviewOffset -= Grid.ItemsThatFit;

                // Keep from selecting empty squares on final, incomplete page.
                var pageSize = System.Math.Min(Items.Count - PreviewOffset, Grid.ItemsThatFit);
                if (ItemSelected >= pageSize)
                    ItemSelected = pageSize - 1;

                var totalPages = (int)System.Math.Ceiling((float)Items.Count / (float)Grid.ItemsThatFit);
                Grid.Text = String.Format("Page {0} of {1}", (int)System.Math.Ceiling((float)PreviewOffset / (float)Grid.ItemsThatFit), totalPages);

                BottomBar.Text = Items[PreviewOffset + ItemSelected].Path;

                for (var i = 0; i < Grid.Children.Count; ++i)
                {
                    var square = Grid.GetChild(i);
                    if (i < pageSize)
                    {
                        square.Hidden = false;
                        square.BackgroundColor = new Vector4(1, 1, 1, 1);
                        if (i == ItemSelected)
                            square.BackgroundColor = new Vector4(1, 0, 0, 1);
                    }
                    else
                        square.Hidden = true;
                    square.Invalidate();
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            var mouse = GuiRoot.MousePointer;
            GuiRoot.MousePointer = null;
            GuiRoot.SetMouseOverlay(null, 0);

            GuiRoot.Draw();

            for (var i = PreviewOffset; i < Items.Count && i < (PreviewOffset + Grid.Children.Count); ++i)
            {
                var item = Items[i];
                if (item.ScreenshotStatus == ChooserItem.ScreenshotStatusEnum.Unloaded)
                {
                    item.Screenshot = (ScreenshotSource == null ? null : ScreenshotSource(item.Path));
                    item.ScreenshotStatus = (item.Screenshot == null ? ChooserItem.ScreenshotStatusEnum.NoneFound : ChooserItem.ScreenshotStatusEnum.Loaded);
                }

                if (item.ScreenshotStatus == ChooserItem.ScreenshotStatusEnum.Loaded)
                    GuiRoot.DrawQuad(Grid.GetChild(i - PreviewOffset).Rect.Interior(7,7,7,7), item.Screenshot);
            }

            GuiRoot.RedrawPopups(); // This hack sucks.
            GuiRoot.MousePointer = mouse;
            GuiRoot.SetMouseOverlay(null, 0);
            GuiRoot.DrawMouse();
            base.Render(gameTime);
        }
    }

}