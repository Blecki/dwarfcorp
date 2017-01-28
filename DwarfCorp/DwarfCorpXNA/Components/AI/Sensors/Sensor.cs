// Sensor.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            FireTimer = new Timer(1.0f, false);
        }

        public Sensor()
        {
            OnSensed += Sensor_OnSensed;
        }

        private void Sensor_OnSensed(List<Body> sensed)
        {
            ;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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