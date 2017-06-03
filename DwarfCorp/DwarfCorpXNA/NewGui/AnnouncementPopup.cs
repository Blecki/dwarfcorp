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
        public double SecondsVisible = 0.0f;
        public String Speaker = "dwarf";
        public List<Tuple<String,Action>> QueuedAnnouncements = new List<Tuple<string, Action>>();
        public double MessageLingerSeconds = 5.0f;

        public void QueueAnnouncement(String Announcement, Action ClickAction)
        {
            QueuedAnnouncements.Add(Tuple.Create(Announcement, ClickAction));
        }

        public override void Construct()
        {
            Hidden = true;

            OnUpdate += (sender, time) =>
                {
                    SecondsVisible -= time.ElapsedGameTime.TotalSeconds;
                    if (SecondsVisible <= 0.0)
                    {
                        if (QueuedAnnouncements.Count == 0)
                        {
                            Hidden = true;
                            OnClick = null;
                            Invalidate();
                        }
                        else
                        {
                            Hidden = false;
                            Text = QueuedAnnouncements[0].Item1;
                            var lambdaAction = QueuedAnnouncements[0].Item2;
                            if (lambdaAction != null)
                                OnClick = (s, a) => lambdaAction();
                            else
                                OnClick = null;
                            QueuedAnnouncements.RemoveAt(0);
                            SecondsVisible = MessageLingerSeconds;
                            Invalidate();
                        }
                    }
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
                .Translate(Rect.Right - speakerTiles.TileWidth, Rect.Bottom - speakerTiles.TileHeight));

            var bubbleRect = new Rectangle(Rect.Left, Rect.Top,
                Rect.Width - speakerTiles.TileWidth, Rect.Height - (speakerTiles.TileHeight / 2));
            var innerRect = bubbleRect.Interior(20, 20, 20, 0);
            meshes.Add(Gum.Mesh.CreateScale9Background(bubbleRect, Root.GetTileSheet("speech-bubble")));

            Rectangle ignore;
            var font = Root.GetTileSheet(Font);
            var text = (font is VariableWidthFont) ? (font as VariableWidthFont).WordWrapString(
                    Text, TextSize, innerRect.Width) : Text;
            int numLines = text.Split('\n').Length;
            meshes.Add(Gum.Mesh.CreateStringMesh(text, Root.GetTileSheet(Font),
                new Vector2(TextSize, TextSize), out ignore)
                .Translate(innerRect.X, innerRect.Y)
                .Colorize(TextColor));
            
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
