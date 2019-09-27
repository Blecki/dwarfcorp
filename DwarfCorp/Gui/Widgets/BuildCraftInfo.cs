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
        private List<ResourceCombo> ResourceCombos = new List<ResourceCombo>();
        private Gui.Widgets.ComboBox NumCombo = new ComboBox();
        private Widget BottomBar;
        private Widget Button;
        public Action<Gui.Widget, Gui.InputEventArgs> BuildAction = null;

        private class ResourceCombo
        {
            public ResourceTagAmount Resource;
            public ComboBox Combo;
            public Widget Count;
        }

        public override void Construct()
        {
            Border = "";
            Font = "font10";
            TextColor = new Vector4(0, 0, 0, 1);
            InteriorMargin = new Margin(16, 16, 16, 16);

            var titleBar = AddChild(new Gui.Widget()
            {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    MinimumSize = new Point(0, 34),
                });

            int k = 0;
            foreach (var ingredient in Data.RequiredResources)
            {
                var resource = Library.FindMedianResourceTypeWithTag(ingredient.Tag);
                if (resource != null)
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
                        Tooltip = ingredient.Tag.ToString()
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
                    Text = String.Format("{0} {1}: ", resourceAmount.Count, resourceAmount.Tag),
                    AutoLayout = AutoLayout.DockLeft
                });

                child.Layout();

                var resourceSelector = child.AddChild(new Gui.Widgets.ComboBox
                {
                    Font = "font8",
                    Items = new List<string> { "<Not enough!>" },
                    AutoLayout = AutoLayout.DockLeft,
                    MinimumSize = new Point(200, 18),
                    Tooltip = String.Format("Type of {0} to use.", resourceAmount.Tag)
                }) as Gui.Widgets.ComboBox;

                var resourceCountIndicator = child.AddChild(new Widget
                {
                    Font = "font8",
                    AutoLayout = AutoLayout.DockFill
                });

                ResourceCombos.Add(new ResourceCombo
                {
                    Resource = resourceAmount,
                    Combo = resourceSelector,
                    Count = resourceCountIndicator
                });
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

            BottomBar = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 32),
                TextColor = new Vector4(1, 0, 0, 1)
            });

            Button = BottomBar.AddChild(new Button()
            {
                Text = Library.GetString("place-new", Data.Verb),
                Tooltip = Library.GetString("place-new-tooltip", Data.Verb, Data.DisplayName),
                OnClick = (widget, args) =>
                {
                    if (Button.Hidden) return;
                    BuildAction(this, args);
                },
                AutoLayout = AutoLayout.DockLeftCentered,
                MinimumSize = new Point(64, 28),
            });

            OnUpdate += (sender, time) =>
            {
                if (Hidden) return;

                bool notEnoughResources = false;

                var availableResources = World.ListResources();

                foreach (var combo in ResourceCombos)
                {
                    var currentSelection = combo.Combo.SelectedItem;

                    combo.Combo.Items = World.ListResourcesWithTag(combo.Resource.Tag).Where(r => r.Count >= combo.Resource.Count).Select(r => r.Type).OrderBy(p => p).ToList();

                    if (combo.Combo.Items.Count == 0)
                    {
                        combo.Combo.Items.Add("<Not enough!>");
                        notEnoughResources = true;
                    }

                    if (combo.Combo.Items.Contains(currentSelection))
                        combo.Combo.SelectedIndex = combo.Combo.Items.IndexOf(currentSelection);
                    else
                        combo.Combo.SelectedIndex = 0;

                    if (combo.Combo.SelectedItem == "<Not enough!>")
                        combo.Count.Text = "";
                    else
                    {
                        if (availableResources.ContainsKey(combo.Combo.SelectedItem))
                            combo.Count.Text = String.Format("Available: {0}", availableResources[combo.Combo.SelectedItem].Count);
                        else
                            combo.Count.Text = "Available: None!";
                    }

                    combo.Combo.Invalidate();
                    combo.Count.Invalidate();
                }
                               
                var minion = World.PlayerFaction.Minions.FirstOrDefault(m => Data.IsMagical ? m.Stats.IsTaskAllowed(TaskCategory.Research) : m.Stats.IsTaskAllowed(TaskCategory.BuildObject));

                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false, null);
                Button.Hidden = true;
                if (minion == null)
                    BottomBar.Text = String.Format("Needs {0} to {1}!", Data.IsMagical ? "Wizard" : "CraftsDwarf", Data.Verb); // Todo: Required minion should be data
                else if (!String.IsNullOrEmpty(Data.CraftLocation) && Data.Type == CraftItem.CraftType.Resource && nearestBuildLocation == null)
                    BottomBar.Text = String.Format("Needs {0} to {1}!", Data.CraftLocation, Data.Verb);
                else if (notEnoughResources)
                    BottomBar.Text = "You don't have enough resources.";
                else
                {
                    Button.Hidden = false;
                    BottomBar.Text = "";
                }
            };

            Root.RegisterForUpdate(this);
            Layout();
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
            
            return true;
        }

        public List<ResourceAmount> GetSelectedResources()
        {
            var r = new List<ResourceAmount>();
            for (var i = 0; i < Data.RequiredResources.Count && i < ResourceCombos.Count; ++i)
            {
                //if (ResourceCombos[i].SelectedItem == null) continue;
                //if (ResourceCombos[i].SelectedItem == "<Not enough!>") continue;
                r.Add(new ResourceAmount(ResourceCombos[i].Combo.SelectedItem,
                    Data.RequiredResources[i].Count));
            }
            return r;
        }

        protected override Mesh Redraw()
        {
            var borderMesh = Mesh.CreateScale9Background(Rect, Root.GetTileSheet("border-fancy"), Scale9Corners.Top | Scale9Corners.Left | Scale9Corners.Right);
            return Mesh.Merge(borderMesh, base.Redraw());
        }
    }

}
