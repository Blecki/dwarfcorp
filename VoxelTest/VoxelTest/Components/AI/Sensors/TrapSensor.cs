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
    [JsonObject(IsReference = true)]
    class TrapSensor : Sensor
    {
        public TrapSensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += TrapSensor_OnSensed;
            Tags.Add("Sensor");
            DrawBoundingBox = true;
            FireTimer = new Timer(0.5f, false);
        }

        private void TrapSensor_OnSensed(List<Body> sensed)
        {
            foreach (Body lc in sensed)
            {
                foreach (string t in lc.Tags)
                {
                    Console.WriteLine(t);
                }
                List<HealthComponent> hcList = lc.GetChildrenOfType<HealthComponent>();
                foreach (HealthComponent hc in hcList)
                {
                    hc.Damage(1000000);
                }
            }
        }
    }
}
