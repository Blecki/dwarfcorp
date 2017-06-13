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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component causes its parent to move up and down in a sinusoid pattern.
    /// </summary>
    public class Bobber : GameComponent, IUpdateableComponent
    {
        public float Magnitude { get; set; }
        public float Rate { get; set; }
        public float Offset { get; set; }
        public float OrigY { get; set; }

        public Bobber()
        {
            
        }

        public Bobber(ComponentManager Manager, float mag, float rate, float offset, float OrigY) :
            base("Sinmover", Manager)
        {
            Magnitude = mag;
            Rate = rate;
            Offset = offset;
            this.OrigY = OrigY;
            //OrigY = component.LocalTransform.Translation.Y;
        }

        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            var body = Parent as Body;
            System.Diagnostics.Debug.Assert(body != null);

            float x = (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + Offset) * Rate) * Magnitude;
            Matrix trans = body.LocalTransform;

            trans.Translation = new Vector3(trans.Translation.X, OrigY + x, trans.Translation.Z);
            body.LocalTransform = trans;

            body.HasMoved = true;

            if (Parent.Parent is Body)
                (Parent.Parent as Body).HasMoved = true;
        }
    }
}