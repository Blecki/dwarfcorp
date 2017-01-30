using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            Root.RegisterForUpdate(this);
            base.Construct();
        }

        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();

            var speakerTiles = Root.GetTileSheet(Speaker);
            meshes.Add(Gum.Mesh.Quad()
                .TileScaleAndTexture(speakerTiles, 0)
                .Translate(Rect.X, Rect.Bottom - speakerTiles.TileHeight));

            var bubbleRect = new Rectangle(Rect.Left + speakerTiles.TileWidth, Rect.Top,
                Rect.Width - speakerTiles.TileWidth, Rect.Height - (speakerTiles.TileHeight / 2));

            meshes.Add(Gum.Mesh.CreateScale9Background(bubbleRect, Root.GetTileSheet("speech-bubble")));

            Rectangle ignore;
            // Todo: Word wrap.
            meshes.Add(Gum.Mesh.CreateStringMesh(Text, Root.GetTileSheet(Font),
                new Vector2(TextSize, TextSize), out ignore)
                .Translate(bubbleRect.X + 20, bubbleRect.Y + 10)
                .Colorize(TextColor));

            if (!String.IsNullOrEmpty(Message))
                meshes.Add(Gum.Mesh.CreateStringMesh(Text, Root.GetTileSheet(Font),
                    new Vector2(TextSize, TextSize), out ignore)
                    .Translate(bubbleRect.X + 20, bubbleRect.Y + 30)
                    .Colorize(TextColor));
            
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
