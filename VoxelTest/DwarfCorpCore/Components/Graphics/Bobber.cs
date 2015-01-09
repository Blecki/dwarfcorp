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
        public Body Component { get; set; }
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float OrigY { get; set; }

        public Bobber()
        {
            
        }

        public Bobber(float mag, float rate, float offset, Body component) :
            base("Sinmover", component)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            Component = component;
            OrigY = component.LocalTransform.Translation.Y;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            Matrix trans = Component.LocalTransform;

            trans.Translation = new Vector3(trans.Translation.X, OrigY + x, trans.Translation.Z);
            Component.LocalTransform = trans;

            Component.HasMoved = true;

            if(Component.Parent is Body)
            {
                (Component.Parent as Body).HasMoved = true;
            }


            base.Update(gameTime, chunks, camera);
        }
    }

}