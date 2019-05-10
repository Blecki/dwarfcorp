using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Generic component with a box that fires when other components enter it.
    /// </summary>
    public class Sensor : GameComponent
    {
        public delegate void Sense(IEnumerable<GameComponent> sensed);
        
        public event Sense OnSensed;
        public Timer FireTimer { get; set; }

        public Sensor()
        {
            CollisionType = CollisionType.None;
        }

        public Sensor(
            ComponentManager Manager, 
            String name, 
            Matrix localTransform, 
            Vector3 boundingBoxExtents, 
            Vector3 boundingBoxPos) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            CollisionType = CollisionType.None;
            Tags.Add("Sensor");
            FireTimer = new Timer(1.0f, false);
        }
        
        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            FireTimer.Update(gameTime);
            if (FireTimer.HasTriggered && OnSensed != null)
                OnSensed(Manager.World.EnumerateIntersectingObjects(BoundingBox, CollisionType.Dynamic));
        }
    }

}