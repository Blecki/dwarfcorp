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
        public GameMaster Master;

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

        private class BuildableItem
        {
            public TileReference Icon;
            public String Name;
            public Object Data;
        }

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
                    new BuildableItem
                    {
                        Icon = new TileReference("rooms", iconSheet.ConvertRectToIndex(data.Icon.SourceRect)),
                        Name = data.Name,
                        Data = data
                    }),
                    tup =>
                    {
                        var data = tup.Data as RoomData;
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
                        Master.Faction.RoomBuilder.CurrentRoomData = tup.Data as RoomData;
                        Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                        Master.Faction.WallBuilder.CurrentVoxelType = null;
                        Master.Faction.CraftBuilder.IsEnabled = false;
                        Master.CurrentToolMode = GameMaster.ToolMode.Build;
                        WorldManager.ShowTooltip("Click and drag to build " + tup.Name);
                        this.Close();
                    });
            }

            if (BuildType.HasFlag(BuildTypes.Wall))
            {
                var wallTypes = VoxelLibrary.GetTypes().Where(voxel => voxel.IsBuildable);

                BuildTab(tabPanel, "Walls", wallTypes.Select(wall =>
                    new BuildableItem
                    {
                        Icon = null,
                        Name = wall.Name,
                        Data = wall
                    }),
                    wall =>
                    {
                        var data = wall.Data as VoxelType;
                        var builder = new StringBuilder();
                        builder.AppendLine(String.Format("{0} Wall", data.Name));
                        builder.AppendLine(String.Format("Strength: {0}", data.StartingHealth));
                        builder.AppendLine(String.Format("Requires: {0}", ResourceLibrary.Resources[data.ResourceToRelease].ResourceName));
                        return builder.ToString();
                    },
                    wall =>
                    {
                        Master.Faction.RoomBuilder.CurrentRoomData = null;
                        Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                        Master.Faction.WallBuilder.CurrentVoxelType = wall.Data as VoxelType;
                        Master.Faction.CraftBuilder.IsEnabled = false;
                        Master.CurrentToolMode = GameMaster.ToolMode.Build;
                        WorldManager.ShowTooltip("Click and drag to build " + wall.Name + " wall.");
                        this.Close();
                    });
            }

            Layout();
        }
        
        private void BuildTab(Gum.Widgets.TabPanel TabPanel, String TabName, IEnumerable<BuildableItem> ItemSource,
            Func<BuildableItem, String> MakeDescription,
            Action<BuildableItem> BuildClicked)
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
                OnClick = (sender, args) => BuildClicked(list.SelectedItem.Tag as BuildableItem),
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
                                var buildableItem = selectedItem.Tag as BuildableItem;

                                descriptionPanel.Text = MakeDescription(buildableItem);
                                descriptionPanel.Invalidate();

                                if (buildableItem.Icon != null)
                                {
                                    iconPanel.Background = buildableItem.Icon;
                                    iconPanel.Hidden = false;
                                }
                                else
                                {
                                    iconPanel.Hidden = true;
                                }
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

                if (buildableItem.Icon != null)
                {
                    row.AddChild(new Gum.Widget
                    {
                        MinimumSize = new Point(32, 32),
                        Background = buildableItem.Icon,
                        AutoLayout = Gum.AutoLayout.DockLeft
                    });
                }

                row.AddChild(new Gum.Widget
                {
                    Text = buildableItem.Name,
                    AutoLayout = Gum.AutoLayout.DockFill
                });
            }

            list.SelectedIndex = 0;
        }
    }
}
