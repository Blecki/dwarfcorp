using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws a sprite to the screen.
    /// </summary>
    public class SpriteDrawCommand : DrawCommand2D
    {
        public Vector3 WorldPosition { get; set; }

        public ImageFrame Image { get; set; }

        public Color Tint { get; set; }

        public Vector2 Scale { get; set; }

        public float Rotation { get; set; }

        public SpriteEffects Effects { get; set; }

        public Vector2 Offset { get; set; }

        public SpriteDrawCommand()
        {
            Scale = new Vector2(1, 1);
            Tint = Color.White;
            Rotation = 0.0f;
            Effects = SpriteEffects.None;
            Offset = Vector2.Zero;
        }

        public SpriteDrawCommand(Vector3 worldPosition, ImageFrame image) :
            base()
        {
            WorldPosition = worldPosition;
            Image = image;
            Tint = Color.White;
        }


        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            if(camera == null)
            {
                return;
            }

            Vector3 screenCoord = viewport.Project(WorldPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            if(viewport.Bounds.Contains((int) screenCoord.X, (int) screenCoord.Y) && screenCoord.Z > 0)
            {
                batch.Draw(Image.Image, new Vector2(screenCoord.X, screenCoord.Y) + Offset, Image.SourceRect, Tint, Rotation, new Vector2(Image.SourceRect.Width / 2.0f, Image.SourceRect.Height / 2.0f), Scale, Effects, 0.0f);
            }
        }
    }
}
