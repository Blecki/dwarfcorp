using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    /// <summary>
    /// A properly framed Icon for use in an icon tray.
    /// </summary>
    public class BuildCraftInfo : Widget
    {
        public CraftItem Data;
        public GameMaster Master;
        public WorldManager World;
        private List<Gui.Widgets.ComboBox> ResourceCombos = new List<Gui.Widgets.ComboBox>();
        private Gui.Widgets.ComboBox NumCombo = new ComboBox();
        public Action<Gui.Widget, Gui.InputEventArgs> BuildAction = null;

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font";
            TextColor = new Vector4(0, 0, 0, 1);
            OnShown = (sender) =>
            {
                Clear();
                ResourceCombos.Clear();
                Parent.OnClick = null;

                var builder = new StringBuilder();
                builder.AppendLine(Data.Name);
                builder.AppendLine(Data.Description);
                builder.AppendLine("Required:");

                AddChild(new Gui.Widget
                {
                    Text = builder.ToString(),
                    AutoLayout = Gui.AutoLayout.DockTop
                });

                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false);

                if (!String.IsNullOrEmpty(Data.CraftLocation) && nearestBuildLocation == null)
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
                            MinimumSize = new Point(100, 18)
                        });

                        child.AddChild(new Gui.Widget()
                        {
                            Font = "font",
                            Text = String.Format("{0} {1}: ",resourceAmount.NumResources, resourceAmount.ResourceType),
                            AutoLayout = AutoLayout.DockLeft
                        });

                        var resourceSelector = child.AddChild(new Gui.Widgets.ComboBox
                        {
                            Font = "font",
                            Items = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType).Select(r => r.ResourceType.ToString()).ToList(),
                            AutoLayout = AutoLayout.DockLeft,
                            MinimumSize = new Point(100, 18)
                        }) as Gui.Widgets.ComboBox;

                        if (resourceSelector.Items.Count == 0)
                            resourceSelector.Items.Add("<Not enough!>");

                        resourceSelector.SelectedIndex = 0;

                        ResourceCombos.Add(resourceSelector);


                        if (Data.Type == CraftItem.CraftType.Resource)
                        {
                            var child2 = AddChild(new Widget()
                            {
                                AutoLayout = AutoLayout.DockTop,
                                MinimumSize = new Point(100, 18)
                            });

                            child2.AddChild(new Gui.Widget()
                            {
                                Font = "font",
                                Text = "Repeat ",
                                AutoLayout = AutoLayout.DockLeft
                            });
                            NumCombo = child2.AddChild(new Gui.Widgets.ComboBox
                            {
                                Font = "font",
                                Items = new List<string>()
                                {
                                    "1x",
                                    "5x",
                                    "10x",
                                    "100x"
                                },
                                AutoLayout = AutoLayout.DockLeft,
                                MinimumSize = new Point(64, 18),
                                MaximumSize = new Point(64, 18)
                            }) as Gui.Widgets.ComboBox;
                            NumCombo.SelectedIndex = 0;
                        }
                    }

                    if (BuildAction != null)
                    {
                        var buildButton = AddChild(new Button()
                        {
                            Text = "Craft",
                            OnClick = (widget, args) => 
                            {
                                BuildAction(widget, args);
                                //sender.Hidden = true;
                                //sender.Invalidate();
                            },
                            AutoLayout = AutoLayout.DockTop,
                            MinimumSize = new Point(64, 24),
                            MaximumSize = new Point(64, 24)
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
                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false);

                if (nearestBuildLocation == null)
                    return false;
            }

            foreach (var resourceAmount in Data.RequiredResources)
                if (Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType).Count == 0)
                    return false;

            return true;
        }

        public List<ResourceAmount> GetSelectedResources()
        {
            var r = new List<ResourceAmount>();
            for (var i = 0; i < Data.RequiredResources.Count && i < ResourceCombos.Count; ++i)
            {
                if (ResourceCombos[i].SelectedItem == "<Not enough!>") continue;
                r.Add(new ResourceAmount(ResourceCombos[i].SelectedItem,
                    Data.RequiredResources[i].NumResources));
            }
            return r;
        }
}
}
