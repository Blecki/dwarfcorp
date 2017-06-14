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

    [JsonObject(IsReference = true)]
    public class Seedling : Fixture, IUpdateableComponent
    {
        public DateTime FullyGrownDay { get; set; }
        public DateTime Birthday { get; set; }
        public Body Adult { get; set; }
        public bool IsGrown { get; set; }
        public Seedling()
        {
            IsGrown = false;
        }

        public Seedling(ComponentManager Manager, Body adult, Vector3 position, SpriteSheet asset, Point frame) :
            base(Manager, position, asset, frame)
        {
            IsGrown = false;
            Adult = adult;
            Name = adult.Name + " seedling";
            AddChild(new Health(Manager, "HP", 1.0f, 0.0f, 1.0f));
            AddChild(new Flammable(Manager, "Flames"));
            Voxel voxelUnder = new Voxel();

            if (Manager.World.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
                AddChild(new VoxelListener(Manager, Manager.World.ChunkManager, voxelUnder));
        }

        public override void Delete()
        {
            if (!IsGrown)
            {
                Adult.Delete();
            }
            base.Delete();
        }

        public override void Die()
        {
            if (!IsGrown)
            {
                Adult.Delete();
            }
            base.Die();
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Manager.World.Time.CurrentDate >= FullyGrownDay)
            {
                CreateAdult();
            }
            base.Update(gameTime, chunks, camera);
        }

        public void CreateAdult()
        {
            IsGrown = true;
            Adult.SetVisibleRecursive(true);
            Adult.SetActiveRecursive(true);
            Die();
        }
    }

    [JsonObject(IsReference = true)]
    public class Plant : Body
    {
        public SpriteSheet Seedlingsheet { get; set; }
        public Point SeedlingFrame { get; set; }
        public int GrowthDays { get; set; }
        public int GrowthHours { get; set; }

        public Plant()
        {
            GrowthDays = 0;
            GrowthHours = 12;
        }

        public Plant(ComponentManager Manager, string name, Matrix localTransform, Vector3 bboxSize,
            Vector3 bboxLocation) :
            base(Manager, name, localTransform, bboxSize, bboxLocation)
        {
            GrowthDays = 0;
            GrowthHours = 12;
        }

        public virtual Seedling BecomeSeedling()
        {
            UpdateTransformsRecursive(Parent as Body);
            SetActiveRecursive(false);
            SetVisibleRecursive(false);

            return AddChild(new Seedling(Manager, this, LocalTransform.Translation, Seedlingsheet, SeedlingFrame)
            {
                FullyGrownDay = Manager.World.Time.CurrentDate.AddHours(GrowthHours).AddDays(GrowthDays)
            }) as Seedling;
        }
    }

    [JsonObject(IsReference = true)]
    public class Tree : Plant
    {
        public Timer HurtTimer { get; set; }
        public ParticleTrigger Particles { get; set; }
        public Tree() { }

        public Tree(string name, ComponentManager manager, Vector3 position, string asset, ResourceLibrary.ResourceType seed, float treeSize) :
            base(manager, name, Matrix.Identity, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize * 0.5f, treeSize * 0.25f, treeSize * 0.5f))
        {
            Seedlingsheet = new SpriteSheet(ContentPaths.Entities.Plants.vine, 32, 32);
            SeedlingFrame = new Point(0, 0);
            HurtTimer = new Timer(1.0f, false);
            ComponentManager componentManager = Manager.World.ComponentManager;
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            LocalTransform = matrix;

            AddChild(new Mesh(manager, "Model", Matrix.CreateRotationY((float)(MathFunctions.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(treeSize, treeSize, treeSize) * Matrix.CreateTranslation(new Vector3(0.7f, 0.0f, 0.7f)), asset, false));

            AddChild(new Health(componentManager, "HP", 100.0f * treeSize, 0.0f, 100.0f * treeSize));

            AddChild(new Flammable(componentManager, "Flames"));


            Tags.Add("Vegetation");
            Tags.Add("EmitsWood");

            //new MinimapIcon(this, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 1, 0));
            Voxel voxelUnder = new Voxel();

            if (Manager.World.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
                AddChild(new VoxelListener(componentManager, componentManager.World.ChunkManager, voxelUnder));

            Inventory inventory = AddChild(new Inventory(componentManager, "Inventory", BoundingBox.Extents(), BoundingBoxPos)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 500
                }
            }) as Inventory;

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int)(treeSize * 10),
                ResourceType = ResourceLibrary.ResourceType.Wood
            });


            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int)(treeSize * 2),
                ResourceType = seed
            });


            Particles = AddChild(new ParticleTrigger("Leaves", componentManager, "LeafEmitter",
                Matrix.Identity, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize * 0.5f, treeSize * 0.25f, treeSize * 0.5f))
            {
                SoundToPlay = ContentPaths.Audio.vegetation_break
            }) as ParticleTrigger;


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
