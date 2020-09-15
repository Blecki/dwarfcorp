using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class SimpleBobber : SimpleSprite
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public Vector3 OriginalPos;

        public SimpleBobber(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            SpriteSheet Sheet,
            Point Frame,
            float mag,
            float rate, 
            float offset)
            : base(Manager, Name, LocalTransform, Sheet, Frame)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            OriginalPos = LocalTransform.Translation;
            LightsWithVoxels = false;
        }

        public float Sin(float x)
        {
            x -= ((int)(x / MathFunctions.fPI)) * MathFunctions.fPI;
            const float B = 4 / MathFunctions.fPI;
            const float C = -4 / (MathFunctions.fPI * MathFunctions.fPI);

            return -(B * x + C * x * ((x < 0) ? -x : x));
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            float x = Sin(((float)gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            var transform = GlobalTransform;
            var originalOffset = transform.Translation;

            if (Active)
                transform.Translation += new Vector3(OriginalPos.X, x, OriginalPos.Z);

            try
            {
                RawSetGlobalTransform(transform);
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
            finally
            {
                transform.Translation = originalOffset;
                RawSetGlobalTransform(transform);
            }
        }
    }
}