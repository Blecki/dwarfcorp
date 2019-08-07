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
            public UInt32 EntityID;
        }

        public const UInt32 TopEntry = 0;

        private List<Message> Messages = new List<Message>();

        public float MessageLiveSeconds = 0.25f;
        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            Font = "font8";
            TextColor = new Vector4(1, 1, 1, 1);

            OnUpdate += (sender, time) =>
                {
                    var now = DateTime.Now;
                    Messages.RemoveAll(m => m.DeletionTime < now);
                    Messages.Sort((a, b) => (int)a.EntityID - (int)b.EntityID);
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

        public void AddMessage(UInt32 EntityID, String Message)
        {
            // Todo: Use entity ID instead of just comparing message
            var existingMessage = Messages.FirstOrDefault(m => m.EntityID == EntityID);
            if (existingMessage != null)
                existingMessage.DeletionTime = DateTime.Now.AddSeconds(MessageLiveSeconds);
            else
                Messages.Add(new InfoTray.Message
                {
                    RawMessage = Message,
                    DeletionTime = DateTime.Now.AddSeconds(MessageLiveSeconds),
                    EntityID = EntityID
                });

            Invalidate();
        }

        public void ClearTopMessage()
        {
            Messages.RemoveAll(m => m.EntityID == TopEntry);
            Invalidate();
        }

        protected override Gui.Mesh Redraw()
        {
            var lines = new List<String>();
            foreach (var m in Messages)
                lines.AddRange(m.RawMessage.Split('\n'));

            //if (ActiveMessage == null) return Gui.Mesh.EmptyMesh();

            var meshes = new List<Gui.Mesh>();
            var stringScreenSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var basic = Root.GetTileSheet("basic");
            var linePos = 0;

            foreach (var line in lines)//ActiveMessage.Lines)
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
                linePos += font.TileHeight * TextSize;
            }
            
            return Gui.Mesh.Merge(meshes.ToArray());
        }
    }
}
