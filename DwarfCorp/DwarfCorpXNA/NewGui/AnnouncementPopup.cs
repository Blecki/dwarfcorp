using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.NewGui
{
    public class AnnouncementPopup : Gum.Widget
    {
        public double SecondsVisible = 10.0f;
        public String Speaker = "dwarf";
        public String Message;

        public override void Construct()
        {
            OnUpdate += (sender, time) =>
                {
                    SecondsVisible -= time.ElapsedGameTime.TotalSeconds;
                    if (SecondsVisible <= 0.0)
                        this.Close();
                };

            Tooltip = Message;

            Root.RegisterForUpdate(this);
            base.Construct();
        }


        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();

            var speakerTiles = Root.GetTileSheet(Speaker);
            meshes.Add(Gum.Mesh.Quad()
                .TileScaleAndTexture(speakerTiles, 0)
                .Translate(Rect.Right - speakerTiles.TileWidth, Rect.Bottom - speakerTiles.TileHeight));

            var bubbleRect = new Rectangle(Rect.Left, Rect.Top,
                Rect.Width - speakerTiles.TileWidth, Rect.Height - (speakerTiles.TileHeight / 2));
            var innerRect = new Rectangle(bubbleRect.X + 20, bubbleRect.Y, bubbleRect.Width - 20, bubbleRect.Height);
            meshes.Add(Gum.Mesh.CreateScale9Background(bubbleRect, Root.GetTileSheet("speech-bubble")));

            Rectangle ignore;
            var font = Root.GetTileSheet(Font);
            // Todo: Word wrap.
            var text = (font is VariableWidthFont) ? (font as VariableWidthFont).WordWrapString(
                    Text, TextSize, innerRect.Width) : Text;
            int numLines = text.Split('\n').Length;
            meshes.Add(Gum.Mesh.CreateStringMesh(text, Root.GetTileSheet(Font),
                new Vector2(TextSize, TextSize), out ignore)
                .Translate(innerRect.X, innerRect.Y + innerRect.Height / 2 - (font.TileHeight) * numLines)
                .Colorize(TextColor));
            
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
