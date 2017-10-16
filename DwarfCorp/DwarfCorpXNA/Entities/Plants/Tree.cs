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
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Tree : Plant
#if DEBUG
        , IRenderableComponent
#endif
    {
        public Timer HurtTimer { get; set; }

        public Tree() { }

#if DEBUG
        void IRenderableComponent.Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (GamePerformance.DebugVisualizationEnabled)
            {
                Drawer3D.DrawLine(this.LocalTransform.Translation, this.LocalTransform.Translation +
                    (Vector3.UnitY * 10), Color.Blue, 0.3f);
                Drawer3D.DrawLine(this.LocalTransform.Translation, this.LocalTransform.Translation +
                    (Vector3.UnitX * 10), Color.Red, 0.3f);
                Drawer3D.DrawLine(this.LocalTransform.Translation, this.LocalTransform.Translation +
                    (Vector3.UnitZ * 10), Color.Green, 0.3f);
            }
        }
#endif

        public Tree(string name, ComponentManager manager, Vector3 position, string asset, ResourceLibrary.ResourceType seed, float treeSize, string seedlingAsset) :
            base(manager, name, Matrix.Identity, 
                new Vector3(
                    PrimitiveLibrary.BatchBillboardPrimitives[asset].Width * 0.75f * treeSize, 
                    PrimitiveLibrary.BatchBillboardPrimitives[asset].Height * treeSize,
                    PrimitiveLibrary.BatchBillboardPrimitives[asset].Width * 0.75f * treeSize),
             asset, treeSize)
        {
            Seedlingsheet = new SpriteSheet(seedlingAsset, 32, 32);
            SeedlingFrame = new Point(0, 0);
            HurtTimer = new Timer(1.0f, false);
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            LocalTransform = matrix;

            //var meshTransform = GetComponent<InstanceMesh>().LocalTransform;
            //meshTransform = meshTransform*Matrix.CreateTranslation(0.0f, GetBoundingBox().Extents().Y / 2, 0.0f);
            //GetComponent<InstanceMesh>().LocalTransform = meshTransform;

            AddChild(new Health(Manager, "HP", 100.0f * treeSize, 0.0f, 100.0f * treeSize));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Vegetation");
            Tags.Add("EmitsWood");

            AddChild(new NewVoxelListener(manager, Matrix.Identity, new Vector3(0.25f, 0.25f, 0.25f),
                new Vector3(0.0f, -0.5f, 0.0f),
                (v) => Die()));

            /*
            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager,
                    voxelUnder));
            */

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), BoundingBoxPos)) as Inventory;
            
            // Can these be spawned when the tree dies rather than when it is created?
            for (int i = 0; i < treeSize * 10; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    Resource = ResourceLibrary.ResourceType.Wood
                });
            }

            for (int i = 0; i < treeSize * 2; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    Resource = seed
                });
            }

            AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, BoundingBoxPos, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_tree_cut_down_1
            });
            
            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            PropogateTransforms();
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if (messageToReceive.Type == Message.MessageType.OnHurt)
            {
                HurtTimer.Update(DwarfTime.LastTime);

                if (HurtTimer.HasTriggered)
                {
                    var particles = GetComponent<ParticleTrigger>();
                    if (particles != null)
                        particles.Trigger(1);
                }
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }


        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);
            //var meshTransform = GetComponent<InstanceMesh>().LocalTransform;
            //meshTransform = meshTransform * Matrix.CreateTranslation(0.0f, GetBoundingBox().Extents().Y / 2, 0.0f);
            //GetComponent<InstanceMesh>().LocalTransform = meshTransform;
        }
    }
}
