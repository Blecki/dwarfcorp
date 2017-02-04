using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class InfoTicker : Gum.Widget
    {
        private List<Tuple<String, DateTime>> Messages = new List<Tuple<String, DateTime>>();
        public float MessageLiveSeconds = 10.0f;

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            OnUpdate += (sender, time) =>
                {
                    var now = DateTime.Now;
                    if (Messages.Count > 0 && Messages[0].Item2 < now)
                    {
                        Messages.RemoveAt(0);
                        Invalidate();
                    }
                };

            base.Construct();
        }

        public int VisibleMessages
        {
            get
            {
                var font = Root.GetTileSheet(Font);
                return Rect.Height / (font.TileHeight * TextSize);
            }
        }

        public void AddMessage(String Message)
        {
            foreach (var message in Message.Split('\n'))
            {
                if (Messages.Count == VisibleMessages)
                    Messages.RemoveAt(0);
                Messages.Add(Tuple.Create(message, DateTime.Now.AddSeconds(MessageLiveSeconds)));
            }
            Invalidate();
        }

        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();
            var ignore = new Rectangle();
            var font = Root.GetTileSheet(Font);
            for (var i = 0; i < Messages.Count; ++i)
                meshes.Add(Gum.Mesh.CreateStringMesh(Messages[i].Item1,
                    font, new Vector2(TextSize, TextSize), out ignore)
                    .Translate(Rect.X, Rect.Y + (font.TileHeight * TextSize * i)));
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
