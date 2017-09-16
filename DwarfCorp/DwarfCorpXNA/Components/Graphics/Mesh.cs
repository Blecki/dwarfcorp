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
    /// <summary>
    /// This component represents an instance of a particular primitive (such as intersecting billboards, or a box), or a mesh. 
    /// Efficiently drawn by the instance manager using state batching.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Mesh : Tinter, IUpdateableComponent
    {
        public string ModelType { get; set; }
        [JsonIgnore]
        public NewInstanceData Instance { get; set; }
        private bool instanceVisible = true;
        private bool checkHeight = false;

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Instance = new NewInstanceData(
                (context.Context as WorldManager).NewInstanceManager,
                ModelType,
                Vector3.One,
                GlobalTransform,
                Tint,
                true);
            Instance.SelectionBufferColor = GetGlobalIDColor();
            instanceVisible = true;
        }

        public Mesh()
        {

        }

        public Mesh(ComponentManager Manager, string name, Matrix localTransform, string modelType, bool addToCollisionManager) :
            base(Manager, name, localTransform, Vector3.Zero, Vector3.Zero, addToCollisionManager)
        {
            ModelType = modelType;
            Instance = new NewInstanceData(Manager.World.NewInstanceManager, ModelType,
                Vector3.One, GlobalTransform, Tint, true);
            Instance.SelectionBufferColor = GetGlobalIDColor();
            instanceVisible = true;
        }

        private bool firstIter = true;

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            bool saveHasMoved = HasMoved || ParentMoved;

            base.Update(gameTime, chunks, camera);

            if (Instance != null && IsVisible && (saveHasMoved || firstIter || Instance.Color != Tint))
            {
                Instance.Color = Tint;
                Instance.Transform = GlobalTransform;
                Instance.SelectionBufferColor = GetGlobalIDColor();
                firstIter = false;
            }
            
            if (IsVisible != instanceVisible)
            {
                SetVisible(IsVisible);
            }
        }

        public override void Die()
        {
            Manager.World.NewInstanceManager.RemoveInstance(Instance);
            base.Die();
        }

        public void SetVisible(bool value)
        {
            if (Instance != null)
            {
                if (value && !instanceVisible)
                {
                    Manager.World.NewInstanceManager.AddInstance(Instance);
                }
                else if (!value && instanceVisible)
                {
                    Manager.World.NewInstanceManager.RemoveInstance(Instance);
                }
            }

            instanceVisible = value;
        }
    }
}