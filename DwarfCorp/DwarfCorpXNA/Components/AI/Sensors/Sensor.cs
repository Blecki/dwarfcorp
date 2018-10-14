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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Generic component with a box that fires when other components enter it.
    /// </summary>
    public class Sensor : Body
    {
        public delegate void Sense(IEnumerable<Body> sensed);
        
        public event Sense OnSensed;
        public Timer FireTimer { get; set; }

        public Sensor()
        {
            UpdateRate = 10;
        }

        public Sensor(
            ComponentManager Manager, 
            String name, 
            Matrix localTransform, 
            Vector3 boundingBoxExtents, 
            Vector3 boundingBoxPos) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            UpdateRate = 10;
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