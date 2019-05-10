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
        public float Bob;
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
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
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

    public class LayeredBobber : LayeredSimpleSprite
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float Bob;

        public LayeredBobber(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            List<Layer> Layers,
            float mag,
            float rate,
            float offset)
            : base(Manager, Name, LocalTransform, Layers)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            var transform = GlobalTransform;
            var originalOffset = transform.Translation;
            transform.Translation += new Vector3(0, x, 0);

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