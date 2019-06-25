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
    public class BuildRoomInfo : Widget
    {
        public ZoneType Data;
        public WorldManager World;

        public override void Construct()
        {
            Border = "border-fancy";
            TextColor = new Vector4(0, 0, 0, 1);
            var builder = new StringBuilder();
            builder.AppendLine(Data.Name);
            builder.AppendLine(Data.Description);
                if (!Data.CanBuildAboveGround)
                    builder.AppendLine("* Must be built below ground.");
                if (!Data.CanBuildBelowGround)
                    builder.AppendLine("* Must be built above ground.");
                builder.AppendLine("Required per 4 tiles:");
                foreach (var requirement in Data.RequiredResources)
                {
                    builder.AppendLine(String.Format("{0}: {1}",
                        requirement.Key, requirement.Value.Count));
                }
                if (Data.RequiredResources.Count == 0)
                    builder.AppendLine("Nothing!");
            builder.Append("CLICK TO BUILD");
            
            Font = "font8";
            Text = builder.ToString();
        }

        public bool CanBuild()
        {
            foreach (var requirment in Data.RequiredResources)
            {
                var inventory = World.ListResourcesWithTag(requirment.Value.Type);
                if (inventory.Sum(r => r.Count) < requirment.Value.Count) return false;
            }

            return true;
        }
        
    }
}
