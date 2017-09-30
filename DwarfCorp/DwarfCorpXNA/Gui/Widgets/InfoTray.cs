using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class InfoTray : Gui.Widget
    {
        private class Message
        {
            public DateTime DeletionTime;
            public String RawMessage;
            public List<String> Lines;
        }

        private Message ActiveMessage = null;

        public float MessageLiveSeconds = 10.0f;
        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            Font = "font8";
            TextColor = new Vector4(1, 1, 1, 1);

            OnUpdate += (sender, time) =>
                {
                    var now = DateTime.Now;
                    if (ActiveMessage != null && ActiveMessage.DeletionTime < now)
                    {
                        ActiveMessage = null;
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
            if (ActiveMessage != null && ActiveMessage.RawMessage == Message)
            {
                ActiveMessage.DeletionTime = DateTime.Now.AddSeconds(MessageLiveSeconds);
                return;
            }

            ActiveMessage = new Message
            {
                RawMessage = Message,
                DeletionTime = DateTime.Now.AddSeconds(MessageLiveSeconds),
                Lines = new List<String>(Message.Split('\n'))
            };

            Invalidate();
        }

        protected override Gui.Mesh Redraw()
        {
            if (ActiveMessage == null) return Gui.Mesh.EmptyMesh();

            var meshes = new List<Gui.Mesh>();
            var stringScreenSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var basic = Root.GetTileSheet("basic");
            var linePos = Rect.Bottom - (font.TileHeight * TextSize);

            foreach (var line in ActiveMessage.Lines.Reverse<String>())
            {
                var stringMesh = Gui.Mesh.CreateStringMesh(line, font, new Vector2(TextSize, TextSize), out stringScreenSize)
                   .Translate(Rect.X, linePos)
                   .Colorize(TextColor);
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(stringScreenSize.Width, stringScreenSize.Height)
                    .Translate(Rect.X, linePos)
                    .Texture(basic.TileMatrix(1))
                    .Colorize(TextBackgroundColor));
                meshes.Add(stringMesh);
                linePos -= font.TileHeight * TextSize;
            }
            
            return Gui.Mesh.Merge(meshes.ToArray());
        }
    }
}
