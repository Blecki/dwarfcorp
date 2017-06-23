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

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font";
            TextColor = new Vector4(0, 0, 0, 1);
            OnShown = (sender) =>
            {
                Clear();

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

                if (nearestBuildLocation == null)
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
                            MinimumSize = new Point(100, 16)
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
                            MinimumSize = new Point(100, 16)
                        }) as Gui.Widgets.ComboBox;

                        if (resourceSelector.Items.Count == 0)
                            resourceSelector.Items.Add("<Not enough!>");

                        resourceSelector.SelectedIndex = 0;

                        ResourceCombos.Add(resourceSelector);
                    }
                }

                Layout();
            };
        }

        public bool CanBuild()
        {
            var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false);

            if (nearestBuildLocation == null)
                return false;

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
