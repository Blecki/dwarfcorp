using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    /// <summary>
    /// A properly framed Icon for use in an icon tray.
    /// </summary>
    public class BuildCraftInfo : Widget
    {
        public CraftItem Data;
        public GameMaster Master;
        public WorldManager World;
        private List<Gum.Widgets.ComboBox> ResourceCombos = new List<Gum.Widgets.ComboBox>();

        public override void Construct()
        {
            Border = "border-fancy";
            Font = "font";

            OnShown = (sender) =>
            {
                Clear();

                var builder = new StringBuilder();
                builder.AppendLine(Data.Name);
                builder.AppendLine(Data.Description);
                builder.AppendLine("Required:");

                AddChild(new Gum.Widget
                {
                    Text = builder.ToString(),
                    AutoLayout = Gum.AutoLayout.DockTop
                });

                var nearestBuildLocation = World.PlayerFaction.FindNearestItemWithTags(Data.CraftLocation, Vector3.Zero, false);

                if (nearestBuildLocation == null)
                {
                    AddChild(new Gum.Widget
                    {
                        Text = String.Format("Needs {0} to build!", Data.CraftLocation),
                        TextColor = new Vector4(1, 0, 0, 1),
                        AutoLayout = Gum.AutoLayout.DockTop
                    });
                }
                else
                {
                    foreach (var resourceAmount in Data.RequiredResources)
                    {
                        var resourceSelector = AddChild(new Gum.Widgets.ComboBox
                        {
                            Font = "outline-font",
                            Items = Master.Faction.ListResourcesWithTag(resourceAmount.ResourceType).Select(r => r.ResourceType.ToString()).ToList(),
                            AutoLayout = AutoLayout.DockTop
                        }) as Gum.Widgets.ComboBox;

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
