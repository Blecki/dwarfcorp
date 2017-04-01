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
    public class BuildRoomInfo : Widget
    {
        public RoomData Data;
        public GameMaster Master;

        public override void Construct()
        {
            Border = "border-fancy";
            var builder = new StringBuilder();
            builder.AppendLine(Data.Description);
                if (!Data.CanBuildAboveGround)
                    builder.AppendLine("* Must be built below ground.");
                if (!Data.CanBuildBelowGround)
                    builder.AppendLine("* Must be built above ground.");
                if (Data.MustBeBuiltOnSoil)
                    builder.AppendLine("* Must be built on soil.");
                builder.AppendLine("Required per 4 tiles:");
                foreach (var requirement in Data.RequiredResources)
                {
                    builder.AppendLine(String.Format("{0}: {1}",
                        requirement.Key, requirement.Value.NumResources));
                }
                if (Data.RequiredResources.Count == 0)
                    builder.AppendLine("Nothing!");
            builder.Append("CLICK TO BUILD");
            
            Font = "font";
            Text = builder.ToString();
        }

        public bool CanBuild()
        {
            foreach (var requirment in Data.RequiredResources)
            {
                var inventory = Master.Faction.ListResourcesWithTag(requirment.Value.ResourceType);
                if (inventory.Sum(r => r.NumResources) < requirment.Value.NumResources) return false;
            }

            return true;
        }
        
    }
}
