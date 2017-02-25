// WorldGUIObject.cs
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
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class WorldGUIObject : Body
    {
        [JsonIgnore]
        public GUIComponent GUIObject { get; set; }

        public bool Enabled { get; set; }
        public WorldGUIObject()
        {
            Enabled = false;
        }

        public WorldGUIObject(Body parent, GUIComponent guiObject) :
            base(parent.Manager, "GUIObject", parent, Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            GUIObject = guiObject;
            AddToCollisionManager = false;
            FrustrumCull = false;
            IsVisible = false;
            Enabled = false;
        }


        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, DwarfCorp.Shader effect, bool renderingForWater)
        {
            if (GUIObject != null)
            {
                if (Enabled && IsVisible && camera.IsInView(GetBoundingBox()))
                {
                    Vector3 screenPos = camera.Project(GlobalTransform.Translation);
                    GUIObject.LocalBounds = new Rectangle((int) screenPos.X - GUIObject.LocalBounds.Width/2,
                        (int) screenPos.Y - GUIObject.LocalBounds.Height/2, GUIObject.LocalBounds.Width,
                        GUIObject.LocalBounds.Height);

                    GUIObject.IsVisible = true;
                }
                else
                {
                    GUIObject.IsVisible = false;
                }
            }
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public override void Die()
        {
            if(GUIObject != null)
                GUIObject.Destroy();
            base.Die();
        }

        public override void Delete()
        {
            if (GUIObject != null)
                GUIObject.Destroy();
            base.Delete();
        }

        
    }
}

