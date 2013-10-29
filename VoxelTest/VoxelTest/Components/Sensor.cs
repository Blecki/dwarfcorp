using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    public class Sensor : LocatableComponent
    {
        public delegate void Sense(List<LocatableComponent> sensed);
        public event Sense OnSensed;
        public Timer FireTimer { get; set; }

        public Sensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += new Sense(Sensor_OnSensed);
            Tags.Add("Sensor");
            FireTimer = new Timer(5.0f, false);
        }

        void Sensor_OnSensed(List<LocatableComponent> sensed)
        {
            ;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (FireTimer.HasTriggered)
            {
                List<LocatableComponent> sensedItems = new List<LocatableComponent>();
                Manager.GetComponentsIntersecting(BoundingBox, sensedItems);

                if (sensedItems.Count > 0)
                {
                    OnSensed.Invoke(sensedItems);
                }
            }
            else
            {
                FireTimer.Update(gameTime);
            }

            base.Update(gameTime, chunks, camera);
        }


    }
}

