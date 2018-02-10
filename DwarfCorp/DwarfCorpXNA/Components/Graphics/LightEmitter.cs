// LightEmitter.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This component dynamically lights up voxels around it with torch light.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class LightEmitter : Body, IUpdateableComponent
    {
        public DynamicLight Light { get; set; }


        public LightEmitter()
        {
            
        }

        public LightEmitter(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float intensity, float range) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Light = new DynamicLight(intensity, range);
        }

        public void UpdateLight()
        {
            if (Active)
                Light.Position = GlobalTransform.Translation;
            else
                Light.Position = new Vector3(-9999, -9999, -9999);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            UpdateLight();

            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            Light.Destroy();
            base.Die();
        }

        public override void Delete()
        {
            Light.Destroy();
            base.Delete();
        }
    }

}