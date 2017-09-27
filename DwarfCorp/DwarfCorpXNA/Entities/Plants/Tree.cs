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
        public Plant Adult { get; set; }
        public bool IsGrown { get; set; }
        public Seedling()
        {
            IsGrown = false;
        }

        public Seedling(ComponentManager Manager, Plant adult, Vector3 position, SpriteSheet asset, Point frame) :
            base(Manager, position, asset, frame)
        {
            IsGrown = false;
            Adult = adult;
            Name = adult.Name + " seedling";
            AddChild(new Health(Manager, "HP", 1.0f, 0.0f, 1.0f));
            AddChild(new Flammable(Manager, "Flames"));

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                Manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(Manager, Manager.World.ChunkManager,
                    voxelUnder));

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
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, Position, true);
            IsGrown = true;
            Adult.IsGrown = true;
            Adult.SetFlagRecursive(Flag.Active, true);
            Adult.SetFlagRecursive(Flag.Visible, true);
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
        public bool IsGrown { get; set; }
        public string MeshAsset { get; set; }
        public float MeshScale { get; set; }

        public Plant()
        {
            GrowthDays = 0;
            GrowthHours = 12;
            IsGrown = false;
        }

        public Plant(ComponentManager Manager, string name, Matrix localTransform, Vector3 bboxSize,
            Vector3 bboxLocation, string meshAsset, float meshScale) :
            base(Manager, name, localTransform, bboxSize, bboxLocation)
        {
            MeshAsset = meshAsset;
            MeshScale = meshScale;
            GrowthDays = 0;
            GrowthHours = 12;
            IsGrown = false;
            CreateMesh(Manager);
        }

        public virtual Seedling BecomeSeedling()
        {
            UpdateTransform();
            SetFlagRecursive(Flag.Active, false);
            SetFlagRecursive(Flag.Visible, false);

            return Parent.AddChild(new Seedling(Manager, this, LocalTransform.Translation, Seedlingsheet, SeedlingFrame)
            {
                FullyGrownDay = Manager.World.Time.CurrentDate.AddHours(GrowthHours).AddDays(GrowthDays)
            }) as Seedling;
        }

        public void CreateMesh(ComponentManager manager)
        {
            var mesh = AddChild(new Mesh(manager, "Model", Matrix.CreateRotationY((float)(MathFunctions.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(MeshScale, MeshScale, MeshScale), MeshAsset, false));
            mesh.SetFlag(Flag.ShouldSerialize, false);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateMesh(manager);
            base.CreateCosmeticChildren(manager);
        }
    }

    [JsonObject(IsReference = true)]
    public class Tree : Plant
    {
        public Timer HurtTimer { get; set; }
        public ParticleTrigger Particles { get; set; }
        public Tree() { }

        public Tree(string name, ComponentManager manager, Vector3 position, string asset, ResourceLibrary.ResourceType seed, float treeSize) :
            base(manager, name, Matrix.Identity, new Vector3(PrimitiveLibrary.BatchBillboardPrimitives[asset].Width, PrimitiveLibrary.BatchBillboardPrimitives[asset].Height , PrimitiveLibrary.BatchBillboardPrimitives[asset].Width) * 0.75f * treeSize,
            new Vector3(0, 0, 0), asset, treeSize)
        {
            Seedlingsheet = new SpriteSheet(ContentPaths.Entities.Plants.vine, 32, 32);
            SeedlingFrame = new Point(0, 0);
            HurtTimer = new Timer(1.0f, false);
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            LocalTransform = matrix;

            var meshTransform = GetComponent<Mesh>().LocalTransform;
            meshTransform = meshTransform*Matrix.CreateTranslation(0.5f, 0.0f, 0.5f);
            GetComponent<Mesh>().LocalTransform = meshTransform;

            AddChild(new Health(Manager, "HP", 100.0f * treeSize, 0.0f, 100.0f * treeSize));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Vegetation");
            Tags.Add("EmitsWood");

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager,
                    voxelUnder));

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), BoundingBoxPos)) as Inventory;
            
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

            Particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, BoundingBoxPos, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_tree_cut_down_1
            }) as ParticleTrigger;


            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            PropogateTransforms();
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


        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);
            var meshTransform = GetComponent<Mesh>().LocalTransform;
            meshTransform = meshTransform * Matrix.CreateTranslation(0.5f, 0.0f, 0.5f);
            GetComponent<Mesh>().LocalTransform = meshTransform;
        }
    }
}
