// Tree.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Tree : Body
    {
        public Timer HurtTimer { get; set; }
        public ParticleTrigger Particles { get; set; }
        public Tree() { }

        public Tree(Vector3 position, string asset, float treeSize) :
            base("Tree", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize * 0.5f, treeSize * 0.25f, treeSize * 0.5f))
        {
            HurtTimer = new Timer(1.0f, false);
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            LocalTransform = matrix;

            new Mesh(componentManager, "Model", this, Matrix.CreateRotationY((float)(PlayState.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(treeSize, treeSize, treeSize) * Matrix.CreateTranslation(new Vector3(0.7f, 0.0f, 0.7f)), asset, false);

            Health health = new Health(componentManager, "HP", this, 100.0f * treeSize, 0.0f, 100.0f * treeSize);
            
            new Flammable(componentManager, "Flames", this, health);


            Tags.Add("Vegetation");
            Tags.Add("EmitsWood");

            new MinimapIcon(this, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 1, 0));
            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Inventory inventory = new Inventory("Inventory", this)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = (int)(treeSize * 10)
                }
            };

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int)(treeSize * 10),
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Wood]
            });


            Particles = new ParticleTrigger("Leaves", componentManager, "LeafEmitter", this,
                Matrix.Identity, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize * 0.5f, treeSize * 0.25f, treeSize * 0.5f))
            {
                SoundToPlay = ContentPaths.Audio.vegetation_break
            };


            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if (messageToReceive.Type == Message.MessageType.OnHurt)
            {
                HurtTimer.Update(DwarfTime.LastTime);

                if(HurtTimer.HasTriggered)
                    Particles.Trigger(1);   
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }

       
    }
}
