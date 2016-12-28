// Bobber.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     This component causes its parent to move up and down in a sinusoid pattern.
    /// </summary>
    public class Bobber : GameComponent
    {
        public Bobber()
        {
        }

        public Bobber(float mag, float rate, float offset, Body component) :
            base("Sinmover", component)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            Component = component;
            OrigY = component.LocalTransform.Translation.Y;
        }

        /// <summary>
        ///     Component to move up and down.
        /// </summary>
        public Body Component { get; set; }

        /// <summary>
        ///     Magnitude of the motion in voxels.
        /// </summary>
        public float Magnitude { get; set; }

        /// <summary>
        ///     Rate (in Hz) that the thing bobs up and down.
        /// </summary>
        public float Rate { get; set; }

        /// <summary>
        ///     Time-varying offset for bobbing.
        /// </summary>
        public float Offset { get; set; }

        /// <summary>
        ///     The origin of the object in Y (voxels).
        /// </summary>
        public float OrigY { get; set; }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            float x = (float) Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset)*Rate)*Magnitude;
            Matrix trans = Component.LocalTransform;

            trans.Translation = new Vector3(trans.Translation.X, OrigY + x, trans.Translation.Z);
            Component.LocalTransform = trans;

            Component.HasMoved = true;

            if (Component.Parent is Body)
            {
                (Component.Parent as Body).HasMoved = true;
            }


            base.Update(gameTime, chunks, camera);
        }
    }
}