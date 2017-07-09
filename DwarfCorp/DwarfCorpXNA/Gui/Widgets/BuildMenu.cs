using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    // Todo: Delete this class.
    public class BuildMenu : Gui.Widget
    {
        public GameMaster Master;
        public WorldManager World;
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
            Rect = Root.RenderData.VirtualScreen;
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

            var tabPanel = AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1),
                SelectedTabColor = new Vector4(1, 0, 0, 1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gui.Widgets.TabPanel;

            #region Room Tab
            if (BuildType.HasFlag(BuildTypes.Room))
            {
                var iconSheet = Root.GetTileSheet("rooms") as Gui.TileSheet;

                BuildTab(tabPanel, "Rooms", RoomLibrary.GetRoomTypes().Select(name => RoomLibrary.GetData(name)).Select(data =>
                    new BuildableItem
                    {
                        Icon = data.NewIcon,
                        Name = data.Name,
                        Data = data
                    }),
                    (item, description, buildButton) =>
                    {
                        var data = item.Data as RoomData;
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

                        description.AddChild(new Gui.Widget
                            {
                                Background = item.Icon,
                                MinimumSize = new Point(32, 32),
                                AutoLayout = Gui.AutoLayout.DockTop,
                                MaximumSize = new Point(32,32)
                            });

                        description.AddChild(new Gui.Widget
                            {
                                Font = "outline-font",
                                Text = builder.ToString(),
                                AutoLayout = Gui.AutoLayout.DockFill
                            });

                        buildButton.OnClick = (sender, args) =>
                        {
                            Master.Faction.RoomBuilder.CurrentRoomData = item.Data as RoomData;
                            Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                            Master.Faction.WallBuilder.CurrentVoxelType = null;
                            Master.Faction.CraftBuilder.IsEnabled = false;
                            Master.CurrentToolMode = GameMaster.ToolMode.Build;
                            World.ShowToolPopup("Click and drag to build " + item.Name);
                            this.Close();
                        };
                    });
            }
            #endregion

            #region Wall tab
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
                    (item, description, buildButton) =>
                    {
                        var data = item.Data as VoxelType;
                        var builder = new StringBuilder();
                        builder.AppendLine(String.Format("{0} Wall", data.Name));
                        builder.AppendLine(String.Format("Strength: {0}", data.StartingHealth));
                        builder.AppendLine(String.Format("Requires: {0}", ResourceLibrary.Resources[data.ResourceToRelease].ResourceName));

                        description.AddChild(new Gui.Widget
                            {
                                Font = "outline-font",
                                Text = builder.ToString(),
                                AutoLayout = AutoLayout.DockFill
                            });

                        buildButton.OnClick = (sender, args) =>
                            {
                                Master.Faction.RoomBuilder.CurrentRoomData = null;
                                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                                Master.Faction.WallBuilder.CurrentVoxelType = item.Data as VoxelType;
                                Master.Faction.CraftBuilder.IsEnabled = false;
                                Master.CurrentToolMode = GameMaster.ToolMode.Build;
                                World.ShowToolPopup("Click and drag to build " + item.Name + " wall.");
                                this.Close();
                            };
                    });
            }
            #endregion

            #region Item Tab
            if (BuildType.HasFlag(BuildTypes.Item))
            {
                var iconSheet = Root.GetTileSheet("crafts") as Gui.TileSheet;
                BuildTab(tabPanel, "Objects",
                    CraftLibrary.CraftItems.Values.Where(item => item.Type == CraftItem.CraftType.Object).Select(craft => new BuildableItem
                    {
                        Icon = craft.Icon,
                        Name = craft.Name,
                        Data = craft
                    }),
                    (item, description, buildButton) =>
                    {
                        var data = item.Data as CraftItem;
                        var builder = new StringBuilder();
                        builder.AppendLine(data.Name);
                        builder.AppendLine(data.Description);
                        builder.AppendLine("Required:");

                        description.AddChild(new Gui.Widget
                            {
                                Background = item.Icon,
                                MinimumSize = new Point(32, 32),
                                AutoLayout = AutoLayout.DockTop,
                                MaximumSize = new Point(32,32)
                            });

                        description.AddChild(new Gui.Widget
                            {
                                Font = "outline-font",
                                Text = builder.ToString(),
                                AutoLayout = Gui.AutoLayout.DockTop
                            });

                        var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(data.CraftLocation, Vector3.Zero, false);

                        if (!String.IsNullOrEmpty(data.CraftLocation) && nearestBuildLocation == null)
                        {
                            description.AddChild(new Gui.Widget
                                {
                                    Font = "outline-font",
                                    Text = String.Format("Needs {0} to build!", data.CraftLocation),
                                    TextColor = new Vector4(1,0,0,1),
                                    AutoLayout = Gui.AutoLayout.DockTop
                                });
                        }
                        else
                        {
                            foreach (var resourceAmount in data.RequiredResources)
                            {
                                var resourceSelector = description.AddChild(new Gui.Widgets.ComboBox
                                {
                                    Font = "outline-font",
                                    Items = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType).Select(r => r.ResourceType.ToString()).ToList(),
                                    AutoLayout = AutoLayout.DockTop
                                }) as Gui.Widgets.ComboBox;

                                if (resourceSelector.Items.Count == 0)
                                    resourceSelector.Items.Add("<Not enough!>");
                            }
                        }
                    });
            }
        

            #endregion

            Layout();
        }
        
        private void BuildTab(Gui.Widgets.TabPanel TabPanel, String TabName, IEnumerable<BuildableItem> ItemSource,
            Action<BuildableItem, Gui.Widget, Gui.Widget> BuildDescriptionPanel)
        {
            var panel = TabPanel.AddTab(TabName, new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            Gui.Widgets.WidgetListView list = null;
            Gui.Widget descriptionPanel = null;
            Gui.Widget buildButton = null;
            
            list = panel.AddChild(new Gui.Widgets.WidgetListView
                {
                    ItemHeight = 32,
                    MinimumSize = new Point(256,0),
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    OnSelectedIndexChanged = (sender) =>
                        {
                            var selectedItem = (sender as Gui.Widgets.WidgetListView).SelectedItem;
                            if (selectedItem != null)
                            {
                                descriptionPanel.Clear();
                                buildButton.Hidden = false;
                                BuildDescriptionPanel(selectedItem.Tag as BuildableItem, descriptionPanel, buildButton);
                                descriptionPanel.Layout();
                                buildButton.Invalidate();        
        
                            }
                        }
                }) as Gui.Widgets.WidgetListView;

            var bottomRow = panel.AddChild(new Widget
                {
                    MinimumSize = new Point(0, 32),
                    AutoLayout = Gui.AutoLayout.DockBottom
                });

            buildButton = bottomRow.AddChild(new Widget
            {
                Text = "BUILD",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                //OnClick = (sender, args) => BuildClicked(list.SelectedItem.Tag as BuildableItem),
                AutoLayout = AutoLayout.DockRight
            });
            
            descriptionPanel = panel.AddChild(new Widget
            {
                AutoLayout = Gui.AutoLayout.DockFill,
                OnLayout = (sender) => sender.Rect.Height -= 36
            });

            foreach (var buildableItem in ItemSource)
            {
                var row = new Gui.Widget
                    {
                        Background = new TileReference("basic", 0),
                        Tag = buildableItem
                    };

                list.AddItem(row);

                if (buildableItem.Icon != null)
                {
                    row.AddChild(new Gui.Widget
                    {
                        MinimumSize = new Point(32, 32),
                        Background = buildableItem.Icon,
                        AutoLayout = Gui.AutoLayout.DockLeft
                    });
                }

                row.AddChild(new Gui.Widget
                {
                    Text = buildableItem.Name,
                    AutoLayout = Gui.AutoLayout.DockFill
                });
            }

            list.SelectedIndex = 0;
        }
    }
}
