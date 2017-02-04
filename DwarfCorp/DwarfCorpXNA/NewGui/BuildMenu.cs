using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class BuildMenu : Gum.Widget
    {
        public GameMaster Faction;

        public enum BuildTypes
        {
            None = 0,
            Room = 1,
            Wall = 2,
            Item = 4,
            Craft = 8,
            Cook = 16,
            AllButCook = Room | Wall | Item | Craft
        }

        public BuildTypes BuildType = BuildTypes.None;

        public override void Construct()
        {
            Rect = Root.VirtualScreen;
            Border = "border-fancy";

            AddChild(new Widget
            {
                Text = "CLOSE",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => this.Close(),
                AutoLayout = AutoLayout.FloatBottomRight
            });

            var tabPanel = AddChild(new Gum.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1),
                SelectedTabColor = new Vector4(1, 0, 0, 1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gum.Widgets.TabPanel;

            if (BuildType.HasFlag(BuildTypes.Room))
            {
                var iconSheet = Root.GetTileSheet("rooms") as Gum.TileSheet;

                BuildTab(tabPanel, "Rooms", RoomLibrary.GetRoomTypes().Select(name => RoomLibrary.GetData(name)).Select(data =>
                    Tuple.Create(new Gum.TileReference("rooms", iconSheet.ConvertRectToIndex(data.Icon.SourceRect)), data.Name, (Object)data)),
                    tup =>
                    {
                        var data = tup.Item3 as RoomData;
                        var builder = new StringBuilder();
                        builder.AppendLine(data.Description);
                        if (!data.CanBuildAboveGround)
                            builder.AppendLine("* Must be built below ground.");
                        if (!data.CanBuildBelowGround)
                            builder.AppendLine("* Must be built above ground.");
                        if (data.MustBeBuiltOnSoil)
                            builder.AppendLine("* Must be built on soil.");
                        builder.AppendLine("Required per 4 tiles:");
                        foreach (var requirement in data.RequiredResources)
                        {
                            builder.AppendLine(String.Format("{0}: {1}",
                                requirement.Key, requirement.Value.NumResources));
                        }
                        if (data.RequiredResources.Count == 0)
                            builder.AppendLine("Nothing!");
                        return builder.ToString();
                    },
                    tup =>
                    {
                        Faction.Faction.RoomBuilder.CurrentRoomData = tup.Item3 as RoomData;
                        Faction.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                        Faction.Faction.WallBuilder.CurrentVoxelType = null;
                        Faction.Faction.CraftBuilder.IsEnabled = false;
                        Faction.CurrentToolMode = GameMaster.ToolMode.Build;
                        WorldManager.ShowTooltip("Click and drag to build " + tup.Item2);
                        this.Close();
                    });
            }

            Layout();
        }
        
        private void BuildTab(Gum.Widgets.TabPanel TabPanel, String TabName, IEnumerable<Tuple<TileReference, String, Object>> ItemSource,
            Func<Tuple<TileReference, String, Object>, String> MakeDescription,
            Action<Tuple<TileReference, String, Object>> BuildClicked)
        {
            var panel = TabPanel.AddTab(TabName, new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            Gum.Widgets.WidgetListView list = null;
            Gum.Widget descriptionPanel = null;
            Gum.Widget iconPanel = null;

            panel.AddChild(new Widget
            {
                Text = "BUILD",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => BuildClicked(list.SelectedItem.Tag as Tuple<TileReference, String, Object>),
                AutoLayout = AutoLayout.FloatBottomRight
            });           

            list = panel.AddChild(new Gum.Widgets.WidgetListView
                {
                    ItemHeight = 32,
                    MinimumSize = new Point(256,0),
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    OnSelectedIndexChanged = (sender) =>
                        {
                            var selectedItem = (sender as Gum.Widgets.WidgetListView).SelectedItem;
                            if (selectedItem != null)
                            {
                                descriptionPanel.Text = MakeDescription(selectedItem.Tag as Tuple<TileReference, String, Object>);
                                descriptionPanel.Invalidate();

                                iconPanel.Background = (selectedItem.Tag as Tuple<TileReference, String, Object>).Item1;
                                iconPanel.Invalidate();
                            }
                        }
                }) as Gum.Widgets.WidgetListView;

            iconPanel = panel.AddChild(new Widget
            {
                MinimumSize = new Point(32, 32),
                MaximumSize = new Point(32, 32),
                AutoLayout = Gum.AutoLayout.DockTop
            });

            descriptionPanel = panel.AddChild(new Widget
            {
                AutoLayout = Gum.AutoLayout.DockFill,
                OnLayout = (sender) => sender.Rect.Height -= 36
            });

            foreach (var buildableItem in ItemSource)
            {
                var row = new Gum.Widget
                    {
                        Background = new TileReference("basic", 0),
                        Tag = buildableItem
                    };

                list.AddItem(row);

                row.AddChild(new Gum.Widget
                {
                    MinimumSize = new Point(32, 32),
                    Background = buildableItem.Item1,
                    AutoLayout = Gum.AutoLayout.DockLeft
                });

                row.AddChild(new Gum.Widget
                {
                    Text = buildableItem.Item2,
                    AutoLayout = Gum.AutoLayout.DockFill
                });
            }

            list.SelectedIndex = 0;
        }
    }
}
