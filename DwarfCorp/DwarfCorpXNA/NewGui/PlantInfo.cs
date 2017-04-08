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
    public class PlantInfo : Widget
    {
        public ResourceLibrary.ResourceType Type;
        public GameMaster Master;

        public override void Construct()
        {
            Border = "border-fancy";

            var builder = new StringBuilder();
            builder.AppendLine("Plant " + Type);
            var res = Type.GetResource();
            builder.AppendLine(res.Description);
            if (res.Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
                builder.AppendLine("* Grows above ground");
            if (res.Tags.Contains(Resource.ResourceTags.BelowGroundPlant))
                builder.AppendLine("* Grows below ground");
            builder.AppendLine("* Grows in soil");
            Font = "font";
            Text = builder.ToString();
            TextColor = Color.Black.ToVector4();
        }
    }
}
