using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component causes its parent to move up and down in a sinusoid pattern.
    /// </summary>
    public class Bobber : GameComponent
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float OrigY { get; set; }

        public Bobber()
        {
            
        }

        public Bobber(ComponentManager Manager, float mag, float rate, float offset, float OrigY) :
            base("Sinmover", Manager)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            this.OrigY = OrigY;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Parent.HasValue(out var body))
            {
                float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
                Matrix trans = body.LocalTransform;

                trans.Translation = new Vector3(trans.Translation.X, OrigY + x, trans.Translation.Z);
                body.LocalTransform = trans;

                body.HasMoved = true;

                if (body.Parent.HasValue(out var grandparent))
                    grandparent.HasMoved = true;
            }
        }
    }
}