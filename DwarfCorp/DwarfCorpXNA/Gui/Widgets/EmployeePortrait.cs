using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePortrait : Widget
    {
        private Gui.Mesh SpriteMesh;
        public DwarfCorp.LayeredSprites.LayerStack Sprite;
        public AnimationPlayer AnimationPlayer;

        public override void Layout()
        {
            base.Layout();
            int x = 48;
            int y = 40;
            float ratio = Math.Max((float)Rect.Width / x, 1.0f);
            int posX = Rect.X + Rect.Width / 2 - (int)(ratio * x) / 2;
            int posY = Rect.Y + Rect.Height / 2 - (int)(ratio * y) / 2;
            SpriteMesh = Gui.Mesh.Quad()
                .Scale((ratio * x), (ratio * y))
                .Translate(Rect.X, Rect.Y);
        }

        public override void PostDraw(GraphicsDevice device)
        {
            if (Hidden || Transparent)
                return;

            if (IsAnyParentHidden())
                return;

            if (Sprite == null)
            {
                return;
            }

            if (SpriteMesh == null)
            {
                Layout();
            }

            var texture = Sprite.GetCompositeTexture();
            if (texture != null)
            {
                var sheet = new SpriteSheet(texture, 48, 40);
                SpriteMesh.ResetQuadTexture();
                var frame = AnimationPlayer.GetCurrentAnimation().Frames[AnimationPlayer.CurrentFrame];
                SpriteMesh.Texture(sheet.TileMatrix(frame.X, frame.Y));
                Root.DrawMesh(SpriteMesh, texture);
            }

            base.PostDraw(device);
        }
    }

}
