using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class PlaceCraftInfo : Widget
    {
        public CraftItem Data;
        public WorldManager World;
        public Action<Gui.Widget, Gui.InputEventArgs> PlaceAction = null;
        public bool AllowWildcard = true;

        public override void Construct()
        {
            Border = "border-one";
            Font = "font10";
            TextColor = new Vector4(0, 0, 0, 1);

            OnShown += (sender) =>
            {
                Clear();

                var titleBar = AddChild(new Gui.Widget()
                {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    MinimumSize = new Point(0, 34),
                });

                titleBar.AddChild(new Gui.Widget
                {
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Background = Data.Icon,
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    Text = Data.CraftedResultsCount.ToString(),
                    Font = "font10-outline-numsonly",
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    TextColor = Color.White.ToVector4()

                });
                titleBar.AddChild(new Gui.Widget
                {
                    Text = " " + Data.Name,
                    Font = "font16",
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(0, 34),
                    Padding = new Margin(0, 0, 16, 0)
                });

                AddChild(new Gui.Widget
                {
                    Text = Data.Description + "\n",
                    AutoLayout = Gui.AutoLayout.DockTop,
                    AutoResizeToTextHeight = true
                });

                var minion = World.PlayerFaction.Minions.FirstOrDefault(m => Data.IsMagical ? m.Stats.IsTaskAllowed(TaskCategory.Research) : m.Stats.IsTaskAllowed(TaskCategory.BuildObject));

                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false, null);
                if (minion == null)
                {
                    AddChild(new Gui.Widget
                    {
                        Text = String.Format("Needs {0} to {1}!", Data.IsMagical ? "Wizard" : "CraftsDwarf", Data.Verb),
                        TextColor = new Vector4(1, 0, 0, 1),
                        AutoLayout = Gui.AutoLayout.DockBottom
                    });
                }
                else if (!String.IsNullOrEmpty(Data.CraftLocation) && Data.Type == CraftItem.CraftType.Resource && nearestBuildLocation == null)
                {
                    AddChild(new Gui.Widget
                    {
                        Text = String.Format("Needs {0} to {1}!", Data.CraftLocation, Data.Verb),
                        TextColor = new Vector4(1, 0, 0, 1),
                        AutoLayout = Gui.AutoLayout.DockBottom
                    });
                }
                else
                {
                    var bottomBar = AddChild(new Widget()
                    {
                        AutoLayout = AutoLayout.DockTop,
                        MinimumSize = new Point(256, 32)
                    });

                    if (Data.Type == CraftItem.CraftType.Object && PlaceAction != null)
                    {
                        var resources = World.ListResources();
                        if (resources.Any(resource => Library.GetResourceType(resource.Key).CraftInfo.CraftItemType == Data.Name))
                        {
                            bottomBar.AddChild(new Button()
                            {
                                Text = Library.GetString("place-existing"),
                                OnClick = (widget, args) =>
                                {
                                    PlaceAction(this, args);
                                },
                                AutoLayout = AutoLayout.DockLeftCentered,
                                MinimumSize = new Point(64, 28),
                                Tooltip = Library.GetString("place-existing-tooltip", Data.DisplayName)
                            });
                        }
                    }
                }

                Layout();
            };
        }

        public bool CanBuild()
        {
            return true;
        }
    }
}
