using Microsoft.Xna.Framework;
using System.Text;
using System;

namespace DwarfCorp.Gui.Widgets
{
    public class PlaceCraftInfo : Widget
    {
        public ResourceType Data;
        public WorldManager World;

        public override Point GetBestSize()
        {
            return new Point(Rect.Width, Rect.Height);
        }

        public override void Construct()
        {
            Font = "font10";
            TextColor = new Vector4(0, 0, 0, 1);

            var builder = new StringBuilder();
            builder.AppendLine(String.Format("Place {0}", Data.DisplayName));
            builder.AppendLine(Data.Description);
            Text = builder.ToString();
        }

        public bool CanBuild()
        {
            return true;
        }
    }
}
