using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is a sensor for traps that damages HealthComponents that collide with the sensor
    /// </summary>
    class TrapSensor : Sensor
    {

        public TrapSensor()
        {
            CollisionType = CollisionType.None;
        }

        public TrapSensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += TrapSensor_OnSensed;
            Tags.Add("Sensor");
            FireTimer = new Timer(0.5f, false);
        }

        private void TrapSensor_OnSensed(IEnumerable<GameComponent> sensed)
        {
            foreach (GameComponent lc in sensed)
            {
                foreach (Health hc in lc.EnumerateAll().OfType<Health>())
                {
                    hc.Damage(DwarfTime.LastTimeX, 1000000);
                }
            }
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            Drawer3D.DrawBox(BoundingBox, Color.White, 0.02f, false);

            base.Update(gameTime, chunks, camera);
        }
    }
}
