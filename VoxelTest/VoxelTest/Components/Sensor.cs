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
        private readonly List<LocatableComponent> sensedItems = new List<LocatableComponent>();

        public Sensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += Sensor_OnSensed;
            Tags.Add("Sensor");
            FireTimer = new Timer(5.0f, false);
        }

        private void Sensor_OnSensed(List<LocatableComponent> sensed)
        {
            ;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            FireTimer.Update(gameTime);
            if(FireTimer.HasTriggered)
            {
                sensedItems.Clear();
                Manager.GetComponentsIntersecting(BoundingBox, sensedItems, CollisionManager.CollisionType.Dynamic);

                if(sensedItems.Count > 0)
                {
                    OnSensed.Invoke(sensedItems);
                }
            }

            base.Update(gameTime, chunks, camera);
        }
    }

}