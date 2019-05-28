using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class BuildCraftInfo : Widget
    {
        public CraftItem Data;
        public WorldManager World;
        private List<Gui.Widgets.ComboBox> ResourceCombos = new List<Gui.Widgets.ComboBox>();
        private Gui.Widgets.ComboBox NumCombo = new ComboBox();
        public Action<Gui.Widget, Gui.InputEventArgs> BuildAction = null;
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
                ResourceCombos.Clear();
                //Parent.OnClick = null;
                var titleBar = AddChild(new Gui.Widget()
                {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    MinimumSize = new Point(0, 34),
                });
                int k = 0;
                foreach(var ingredient in Data.RequiredResources)
                {
                    var resource = ResourceLibrary.FindMedianWithTag(ingredient.Type);
                    titleBar.AddChild(new Gui.Widget
                    {
                        MinimumSize = new Point(32, 32),
                        MaximumSize = new Point(32, 32),
                        Background = resource.GuiLayers[0],
                        AutoLayout = AutoLayout.DockLeft,
                        Text = ingredient.Count.ToString(),
                        TextHorizontalAlign = HorizontalAlign.Right,
                        TextVerticalAlign = VerticalAlign.Bottom,
                        Font = "font10-outline-numsonly",
                        TextColor = Color.White.ToVector4(),
                        Tooltip = ingredient.Type.ToString()
                    });
                    if (k < Data.RequiredResources.Count - 1)
                    {
                        titleBar.AddChild(new Gui.Widget
                        {
                            MinimumSize = new Point(16, 32),
                            MaximumSize = new Point(16, 32),
                            AutoLayout = AutoLayout.DockLeft,
                            Text = "+",
                            TextHorizontalAlign = HorizontalAlign.Center,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Font = "font10"
                        });
                    }
                    else
                    {
                        titleBar.AddChild(new Gui.Widget
                        {
                            MinimumSize = new Point(16, 32),
                            MaximumSize = new Point(16, 32),
                            AutoLayout = AutoLayout.DockLeft,
                            Text = ">>",
                            TextHorizontalAlign = HorizontalAlign.Center,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            Font = "font10"
                        });
                    }
                    k++;
                }

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

                var minion = World.PlayerFaction.Minions.FirstOrDefault(m => Data.IsMagical ? m.Stats.IsTaskAllowed(Task.TaskCategory.Research) : m.Stats.IsTaskAllowed(Task.TaskCategory.BuildObject));

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
                    foreach (var resourceAmount in Data.RequiredResources)
                    {

                        var child = AddChild(new Widget()
                        {
                            AutoLayout = AutoLayout.DockTop,
                            MinimumSize = new Point(200, 18)
                        });

                        child.AddChild(new Gui.Widget()
                        {
                            Font = "font8",
                            Text = String.Format("{0} {1}: ",resourceAmount.Count, resourceAmount.Type),
                            AutoLayout = AutoLayout.DockLeft
                        });
                        child.Layout();

                        var resourceSelector = child.AddChild(new Gui.Widgets.ComboBox
                        {
                            Font = "font8",
                            Items = World.ListResourcesWithTag(resourceAmount.Type).Where(r => r.Count >= resourceAmount.Count).Select(r => r.Type).OrderBy(p => p).ToList(),
                            AutoLayout = AutoLayout.DockLeft,
                            MinimumSize = new Point(200, 18),
                            Tooltip = String.Format("Type of {0} to use.", resourceAmount.Type)
                        }) as Gui.Widgets.ComboBox;

                        if (AllowWildcard)
                            resourceSelector.Items.Insert(0, "Any");

                        if (resourceSelector.Items.Count == 0)
                            resourceSelector.Items.Add("<Not enough!>");

                        resourceSelector.SelectedIndex = 0;

                        ResourceCombos.Add(resourceSelector);
                    }


                    if (Data.Type == CraftItem.CraftType.Resource)
                    {
                        var child2 = AddChild(new Widget()
                        {
                            AutoLayout = AutoLayout.DockTop,
                            MinimumSize = new Point(100, 18)
                        });

                        child2.AddChild(new Gui.Widget()
                        {
                            Font = "font8",
                            Text = "Repeat ",
                            AutoLayout = AutoLayout.DockLeft
                        });
                        NumCombo = child2.AddChild(new Gui.Widgets.ComboBox
                        {
                            Font = "font8",
                            Items = new List<string>()
                                {
                                    "1x",
                                    "5x",
                                    "10x",
                                    "100x"
                                },
                            AutoLayout = AutoLayout.DockLeft,
                            MinimumSize = new Point(64, 18),
                            MaximumSize = new Point(64, 18),
                            Tooltip = "Craft this many objects."
                        }) as Gui.Widgets.ComboBox;
                        NumCombo.SelectedIndex = 0;
                    }

                    var bottomBar = AddChild(new Widget()
                    {
                        AutoLayout = AutoLayout.DockTop,
                        MinimumSize = new Point(256, 32)
                    });

                    if (BuildAction != null)
                    {
                        if (Data.Type == CraftItem.CraftType.Object && PlaceAction != null)
                        {
                            var resources = World.ListResources();
                            if (resources.Any(resource => ResourceLibrary.GetResourceByName(resource.Key).CraftInfo.CraftItemType == Data.Name))
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

                        var buildButton = bottomBar.AddChild(new Button()
                        {
                            Text = Library.GetString("place-new", Data.Verb),
                            Tooltip = Library.GetString("place-new-tooltip", Data.Verb, Data.DisplayName),
                            OnClick = (widget, args) => 
                            {
                                BuildAction(this, args);
                                //sender.Hidden = true;
                                //sender.Invalidate();
                            },
                            AutoLayout = AutoLayout.DockLeftCentered,
                            MinimumSize = new Point(64, 28),
                        });

                        //Parent.OnClick = (parent, args) => buildButton.OnClick(buildButton, args);
                    }
                }

                Layout();
            };
        }

        public int GetNumRepeats()
        {
            if (NumCombo == null)
            {
                return 1;
            }

            switch (NumCombo.SelectedIndex)
            {
                case 0:
                    return 1;
                case 1:
                    return 5;
                case 2:
                    return 10;
                case 3:
                    return 100;
            }
            return 1;
        }

        public bool CanBuild()
        {
            if (!String.IsNullOrEmpty(Data.CraftLocation))
            { 
                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false, null);

                if (nearestBuildLocation == null)
                {
                    return false;
                }
            }

            if (!AllowWildcard)
            {
                foreach (var resourceAmount in Data.RequiredResources)
                    if (World.ListResourcesWithTag(resourceAmount.Type).Count == 0)
                    {
                        return false;
                    }
            }

            return true;
        }

        public List<ResourceAmount> GetSelectedResources()
        {
            var r = new List<ResourceAmount>();
            for (var i = 0; i < Data.RequiredResources.Count && i < ResourceCombos.Count; ++i)
            {
                if (ResourceCombos[i].SelectedItem == null) continue;
                if (ResourceCombos[i].SelectedItem == "<Not enough!>") continue;
                if (ResourceCombos[i].SelectedItem == "Any") continue;
                r.Add(new ResourceAmount(ResourceCombos[i].SelectedItem,
                    Data.RequiredResources[i].Count));
            }
            return r;
        }
}

}
