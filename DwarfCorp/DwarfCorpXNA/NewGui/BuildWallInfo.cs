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
    public class BuildWallInfo : Widget
    {
        public VoxelType Data;

        public override void Construct()
        {
            Border = "border-fancy";

            var builder = new StringBuilder();
            builder.AppendLine(String.Format("{0} Wall", Data.Name));
            builder.AppendLine(String.Format("Strength: {0}", Data.StartingHealth));
            builder.AppendLine(String.Format("Requires: {0}", ResourceLibrary.Resources[Data.ResourceToRelease].ResourceName));
            builder.Append("CLICK TO BUILD");
            
            Font = "font";
            Text = builder.ToString();
        }
        
    }
}
