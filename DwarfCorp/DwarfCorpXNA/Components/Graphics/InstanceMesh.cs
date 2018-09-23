// Mesh.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class InstanceMesh : Tinter
    {
        public string ModelType { get; set; }

        [JsonIgnore]
        public NewInstanceData Instance { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Instance = new NewInstanceData(
                ModelType,
                GlobalTransform,
                LightRamp);
            Instance.SelectionBufferColor = this.GetGlobalIDColor();
        }

        public InstanceMesh()
        {

        }

        public InstanceMesh(ComponentManager Manager, string name, Matrix localTransform, string modelType, Vector3 BoundingBoxExtents, Vector3 BoundingBoxPos) :
            base(Manager, name, localTransform, BoundingBoxExtents, BoundingBoxPos)
        {
            PropogateTransforms();
            UpdateBoundingBox();
            ModelType = modelType;
            Instance = new NewInstanceData(ModelType, GlobalTransform, LightRamp);
            Instance.SelectionBufferColor = this.GetGlobalIDColor();
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (IsVisible && !renderingForWater)
            {
                Instance.LightRamp = LightRamp;
                Instance.Transform = GlobalTransform;
                Instance.SelectionBufferColor = this.GetGlobalIDColor();
                Manager.World.InstanceRenderer.RenderInstance(Instance, graphicsDevice, effect, camera, InstanceRenderMode.Normal);
            }
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);

            if (IsVisible)
            {
                Instance.LightRamp = LightRamp;
                Instance.Transform = GlobalTransform;
                Instance.SelectionBufferColor = this.GetGlobalIDColor();
                Manager.World.InstanceRenderer.RenderInstance(Instance, graphicsDevice, effect, camera, InstanceRenderMode.SelectionBuffer);
            }
        }
    }
}