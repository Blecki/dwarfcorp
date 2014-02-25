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
        }

        private void TrapSensor_OnSensed(List<LocatableComponent> sensed)
        {
            foreach (LocatableComponent lc in sensed)
            {
                foreach (string t in lc.Tags)
                {
                    Console.WriteLine(t);
                }
                List<HealthComponent> hcList = lc.GetChildrenOfType<HealthComponent>();
                //if (hcList.Count > 0) Console.WriteLine("Sensed Health Component");
            }
        }
    }
}
