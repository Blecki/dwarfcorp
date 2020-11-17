using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class PlantInfo : Widget
    {
        public String Type;

        public override void Construct()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Plant " + Type);
            if (Library.GetResourceType(Type).HasValue(out var res))
            {
                builder.AppendLine(res.Description);
                if (res.Tags.Contains("AboveGroundPlant"))
                    builder.AppendLine("* Grows above ground");
                if (res.Tags.Contains("BelowGroundPlant"))
                    builder.AppendLine("* Grows below ground");
            }
            builder.AppendLine("* Grows in soil");
            Font = "font8";
            Text = builder.ToString();
            TextColor = Color.Black.ToVector4();
        }
    }
}
