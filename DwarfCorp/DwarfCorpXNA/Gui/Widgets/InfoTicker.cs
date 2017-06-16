using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class InfoTicker : Gui.Widget
    {
        private List<String> Messages = new List<String>();
        private System.Threading.Mutex MessageLock = new System.Threading.Mutex();
        private bool NeedsInvalidated = false;

        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);

        public int VisibleLines
        {
            get
            {
                var font = Root.GetTileSheet(Font);
                return Rect.Height / (font.TileHeight * TextSize);
            }
        }

        public override void Construct()
        {
            Root.RegisterForUpdate(this);
            OnUpdate = (sender, time) =>
            {
                MessageLock.WaitOne();
                if (NeedsInvalidated)
                    this.Invalidate();
                NeedsInvalidated = false;
                MessageLock.ReleaseMutex();
            };
        }

        public void AddMessage(String Message)
        {
            // AddMessage is called by another thread - need to protect the list.
            MessageLock.WaitOne();

            if (Messages.Count > 0 && Messages[Messages.Count - 1].Length > 11 &&
                Message.StartsWith(Messages[Messages.Count - 1].Substring(0, 11)))
                Messages[Messages.Count - 1] = Message;
            else
                Messages.Add(Message);

            if (Messages.Count > VisibleLines)
                Messages.RemoveAt(0);
            // Need to invalidate inside the main GUI thread or else!
            NeedsInvalidated = true;
            MessageLock.ReleaseMutex();
        }

        protected override Gui.Mesh Redraw()
        {
            var meshes = new List<Gui.Mesh>();
            var stringScreenSize = new Rectangle();
            var font = Root.GetTileSheet(Font);
            var basic = Root.GetTileSheet("basic");
            var linePos = 0;

            MessageLock.WaitOne();
            foreach (var line in Messages)
            {
                var stringMesh = Gui.Mesh.CreateStringMesh(line, font, new Vector2(TextSize, TextSize), out stringScreenSize)
                    .Translate(Rect.X, Rect.Y + linePos)
                    .Colorize(TextColor);
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(stringScreenSize.Width, stringScreenSize.Height)
                    .Translate(Rect.X, Rect.Y + linePos)
                    .Texture(basic.TileMatrix(1))
                    .Colorize(TextBackgroundColor));
                meshes.Add(stringMesh);
                linePos += font.TileHeight * TextSize;
            }
            MessageLock.ReleaseMutex();

            return Gui.Mesh.Merge(meshes.ToArray());
        }

        public bool HasMesssage(string loadingMessage)
        {
            return Messages.Contains(loadingMessage);
        }
    }
}
