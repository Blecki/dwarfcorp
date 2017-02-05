using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class InfoTicker : Gum.Widget
    {
        private class Message
        {
            public DateTime DeletionTime;
            public String RawMessage;
            public List<String> Lines;
        }

        private List<Message> Messages = new List<Message>();
        private int MessageLineCount {  get { return Messages.Sum(m => m.Lines.Count); } }

        public float MessageLiveSeconds = 10.0f;
        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            Font = "font";
            TextColor = new Vector4(1, 1, 1, 1);

            OnUpdate += (sender, time) =>
                {
                    var now = DateTime.Now;
                    if (Messages.Count > 0 && Messages[0].DeletionTime < now)
                    {
                        Messages.RemoveAt(0);
                        Invalidate();
                    }
                };

            base.Construct();
        }

        public int VisibleLines
        {
            get
            {
                var font = Root.GetTileSheet(Font);
                return Rect.Height / (font.TileHeight * TextSize);
            }
        }

        public void AddMessage(String Message)
        {
            var existingMessage = Messages.FirstOrDefault(m => m.RawMessage == Message);

            if (existingMessage != null)
                Messages.Remove(existingMessage);

            Messages.Add(new Message
            {
                RawMessage = Message,
                DeletionTime = DateTime.Now.AddSeconds(MessageLiveSeconds),
                Lines = new List<String>(Message.Split('\n'))
            });

            while (MessageLineCount > VisibleLines)
                Messages.RemoveAt(0);

            Invalidate();
        }

        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();
            var stringScreenSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var basic = Root.GetTileSheet("basic");
            var linePos = 0;

            foreach (var line in Messages.SelectMany(m => m.Lines))
            {
                var stringMesh = Gum.Mesh.CreateStringMesh(line, font, new Vector2(TextSize, TextSize), out stringScreenSize)
                    .Translate(Rect.X, Rect.Y + linePos)
                    .Colorize(TextColor);
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(stringScreenSize.Width, stringScreenSize.Height)
                    .Translate(Rect.X, Rect.Y + linePos)
                    .Texture(basic.TileMatrix(1))
                    .Colorize(TextBackgroundColor));
                meshes.Add(stringMesh);
                linePos += font.TileHeight * TextSize;

            }

            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
