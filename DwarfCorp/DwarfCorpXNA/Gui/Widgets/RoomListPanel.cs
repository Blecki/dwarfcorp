using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class RoomListPanel : Widget
    {
        public WorldManager World;

        private WidgetListView ListView;
        private EditableTextField FilterBox; 

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font10";
            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);

                FilterBox = AddChild(new EditableTextField
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 24),
                    Text = ""
                }) as EditableTextField;

                ListView = AddChild(new WidgetListView
                {
                    AutoLayout = AutoLayout.DockFill,
                    SelectedItemForegroundColor = new Vector4(0,0,0,1),
                    ChangeColorOnSelected=false,
                    Border = null,
                    ItemHeight = 24
                }) as WidgetListView;

                ListView.Border = null; // Can't make WidgetListView stop defaulting its border without breaking everywhere else its used.
            };

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden) return;

                var roomsToDisplay = World.PlayerFaction.GetRooms().Where(r => !String.IsNullOrEmpty(FilterBox.Text) ? r.ID.Contains(FilterBox.Text) : true);

                int i = 0;
                ListView.ClearItems();
                foreach (var room in roomsToDisplay)
                {
                    i++;
                    var tag = room.GuiTag as Widget;
                    var lambdaCopy = room;

                    if (tag != null)
                        ListView.AddItem(tag);
                    else
                    {
                        #region Create gui row

                        tag = Root.ConstructWidget(new Widget
                        {
                            Text = room.GetDescriptionString(),
                            MinimumSize = new Point(0, 16),
                            Padding = new Margin(0, 0, 4, 4),
                            TextVerticalAlign = VerticalAlign.Center,
                            Background = new TileReference("basic", 0),
                            BackgroundColor = i % 2 == 0 ? new Vector4(0.0f, 0.0f, 0.0f, 0.1f) : new Vector4(0, 0, 0, 0.25f)
                        });

                        tag.OnUpdate = (sender1, args) =>
                        {
                            if (tag.IsAnyParentHidden())
                            {
                                return;
                            }

                            if (sender1.ComputeBoundingChildRect().Contains(Root.MousePosition))
                            {
                                Drawer3D.DrawBox(lambdaCopy.GetBoundingBox(), Color.White, 0.1f, true);
                            }
                        };

                        Root.RegisterForUpdate(tag);

                        tag.AddChild(new Button
                        {
                            Text = "Destroy",
                            AutoLayout = AutoLayout.DockRight,
                            MinimumSize = new Point(16, 0),
                            ChangeColorOnHover = true,
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                                {
                                    Text = "Do you want to destroy this " + lambdaCopy.RoomData.Name + "?",
                                    OnClose = (_sender2) => DestroyZoneTool.DestroyRoom((_sender2 as Gui.Widgets.Confirm).DialogResult, lambdaCopy, World.PlayerFaction, World)
                                });
                            }
                        });

                        tag.AddChild(new Widget { MinimumSize = new Point(4, 0), AutoLayout = AutoLayout.DockRight });

                        tag.AddChild(new Button
                        {
                            Text = "Go to",
                            AutoLayout = AutoLayout.DockRight,
                            ChangeColorOnHover = true,
                            MinimumSize = new Point(16, 0),
                            TextVerticalAlign = VerticalAlign.Center,
                            OnClick = (_sender, args) =>
                            {
                                World.Camera.ZoomTargets.Clear();
                                World.Camera.ZoomTargets.Add(lambdaCopy.GetBoundingBox().Center());
                            }
                        });

                        if (lambdaCopy is Stockpile && !(lambdaCopy is Graveyard) && !(lambdaCopy is Treasury))
                        {

                            tag.AddChild(new Button
                            {
                                Text = "Resources...",
                                AutoLayout = AutoLayout.DockRight,
                                ChangeColorOnHover = true,
                                MinimumSize = new Point(16, 0),
                                TextVerticalAlign = VerticalAlign.Center,
                                OnClick = (_sender, args) =>
                                {
                                    var widget = Root.ConstructWidget(new Widget()
                                    {
                                        Border = "border-fancy",
                                        Font = "font10",
                                        Rect = Root.RenderData.VirtualScreen.Interior(200, 150, 200, 150)
                                    });

                                    widget.AddChild(new Widget()
                                    {
                                        MinimumSize = new Point(120, 32),
                                        Text = "Allowed Resources",
                                        Font = "font16",
                                        AutoLayout = AutoLayout.DockTop,
                                    });

                                    var interiorWidget = widget.AddChild(new Widget()
                                    {
                                        Rect = Root.RenderData.VirtualScreen.Interior(200, 150, 200, 180),
                                        AutoLayout = AutoLayout.DockTop
                                    });

                                    var stockpile = lambdaCopy as Stockpile;

                                    var grid = interiorWidget.AddChild(new GridPanel()
                                    {
                                        AutoLayout = AutoLayout.DockFill,
                                        ItemSize = new Point(200, 32),
                                        ItemSpacing = new Point(2, 2)
                                    }) as GridPanel;

                                    List<Resource.ResourceTags> blacklistableResources = new List<Resource.ResourceTags>()
                                    {
                                        Resource.ResourceTags.Alcohol,
                                        Resource.ResourceTags.Meat,
                                        Resource.ResourceTags.Metal,
                                        Resource.ResourceTags.Gem,
                                        Resource.ResourceTags.Magical,
                                        Resource.ResourceTags.Wood,
                                        Resource.ResourceTags.Soil,
                                        Resource.ResourceTags.Sand,
                                        Resource.ResourceTags.Fruit,
                                        Resource.ResourceTags.Gourd,
                                        Resource.ResourceTags.Fungus,
                                        Resource.ResourceTags.Fuel,
                                        Resource.ResourceTags.Craft,
                                        Resource.ResourceTags.CraftItem,
                                        Resource.ResourceTags.Bone,
                                        Resource.ResourceTags.Potion,
                                        Resource.ResourceTags.PreparedFood,
                                        Resource.ResourceTags.Rail,
                                        Resource.ResourceTags.Seed
                                    }.OrderBy(t => t.ToString()).ToList();
                                    List<CheckBox> boxes = new List<CheckBox>();
                                    foreach (Resource.ResourceTags tagType in blacklistableResources)
                                    {
                                        var resource = ResourceLibrary.GetAverageWithTag(tagType);
                                        var resources = ResourceLibrary.GetResourcesByTag(tagType);
                                        Resource.ResourceTags lambdaType = tagType;
                                        var entry = grid.AddChild(new Widget());

                                        if (resource != null)
                                        {
                                            entry.AddChild(new ResourceIcon()
                                            {
                                                MinimumSize = new Point(32, 32),
                                                MaximumSize = new Point(32, 32),
                                                Layers = resource.GuiLayers,
                                                AutoLayout = AutoLayout.DockLeft
                                            });
                                        }

                                        var numResourcesInGroup = resources.Count();
                                        var extraTooltip = numResourcesInGroup > 0 ?  "\ne.g " + TextGenerator.GetListString(resources.Select(s => (string)s.Name).Take(Math.Min(numResourcesInGroup, 4)).ToList()) : "";

                                        boxes.Add(entry.AddChild(new CheckBox()
                                        {
                                            Text = TextGenerator.SplitCamelCase(tagType.ToString()),
                                            Tooltip = "Check to allow this stockpile to store " + tagType.ToString() + " resources." + extraTooltip,
                                            CheckState = !stockpile.BlacklistResources.Contains(tagType),
                                            OnCheckStateChange = (checkSender) =>
                                            {
                                                var checkbox = checkSender as CheckBox;
                                                if (checkbox.CheckState && stockpile.BlacklistResources.Contains(lambdaType))
                                                {
                                                    stockpile.BlacklistResources.Remove(lambdaType);
                                                }
                                                else if (!stockpile.BlacklistResources.Contains(lambdaType))
                                                {
                                                    stockpile.BlacklistResources.Add(lambdaType);
                                                }
                                            },
                                            AutoLayout = AutoLayout.DockLeft
                                        }
                                        ) as CheckBox);


                                    }


                                    widget.AddChild(new CheckBox()
                                    {
                                        Text = "Toggle All",
                                        CheckState = boxes.All(b => b.CheckState),
                                        OnCheckStateChange = (checkSender) =>
                                        {
                                            foreach (var box in boxes)
                                            {
                                                box.CheckState = (checkSender as CheckBox).CheckState;
                                            }
                                        },
                                        AutoLayout = AutoLayout.FloatBottomLeft
                                    });

                                    widget.AddChild(new Button()
                                    {
                                        Text = "OK",
                                        AutoLayout = AutoLayout.FloatBottomRight,
                                        OnClick = (sender1, args1) => { widget.Close(); }
                                    });


                                    widget.Layout();
                                    Root.ShowModalPopup(widget);

                                }
                            });
                        }

                        #endregion

                        room.GuiTag = tag;
                        ListView.AddItem(tag);
                    }

                    tag.Text = room.GetDescriptionString();
                }

                ListView.Invalidate();
            };

            base.Construct();
        }

       
    }
}
