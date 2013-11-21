using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class SinMover : GameComponent
    {
        public LocatableComponent Component { get; set; }
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }

        public SinMover(float mag, float rate, float offset, LocatableComponent component) :
            base(component.Manager, "Sinmover", component)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            Component = component;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            float x = (float) Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            Matrix trans = Component.LocalTransform;

            trans.Translation = new Vector3(trans.Translation.X, x, trans.Translation.Z);
            Component.LocalTransform = trans;

            Component.HasMoved = true;

            if(Component.Parent is LocatableComponent)
            {
                (Component.Parent as LocatableComponent).HasMoved = true;
            }


            base.Update(gameTime, chunks, camera);
        }
    }

}