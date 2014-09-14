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
    /// Generic component with a box that fires when other components enter it.
    /// </summary>
    public class Sensor : Body
    {

        public delegate void Sense(List<Body> sensed);
        public event Sense OnSensed;
        public Timer FireTimer { get; set; }
        private readonly List<Body> sensedItems = new List<Body>();

        public Sensor(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += Sensor_OnSensed;
            Tags.Add("Sensor");
            FireTimer = new Timer(5.0f, false);
        }

        public Sensor()
        {
            OnSensed += Sensor_OnSensed;
        }

        private void Sensor_OnSensed(List<Body> sensed)
        {
            ;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            FireTimer.Update(gameTime);
            if(FireTimer.HasTriggered)
            {
                sensedItems.Clear();
                Manager.GetBodiesIntersecting(BoundingBox, sensedItems, CollisionManager.CollisionType.Dynamic);

                if(sensedItems.Count > 0)
                {
                    OnSensed.Invoke(sensedItems);
                }
            }

            base.Update(gameTime, chunks, camera);
        }
    }

}