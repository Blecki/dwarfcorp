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
           string meshAsset, float meshScale) :
            base(Manager, name, localTransform, bboxSize, new Vector3(0.0f, bboxSize.Y / 2, 0.0f))
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
            VoxelHandle below = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(Manager.World.ChunkManager.ChunkData, 
                new GlobalVoxelCoordinate((int)LocalTransform.Translation.X, 
                (int)LocalTransform.Translation.Y, (int)LocalTransform.Translation.Z)));
            Vector3 pos = LocalTransform.Translation;
            pos += new Vector3(0, 0.5f, 0);
            if (below.IsValid)
            {
                pos = below.WorldPosition + new Vector3(0.0f, 1.0f, 0.0f);
            }
            return Parent.AddChild(new Seedling(Manager, this, pos, Seedlingsheet, SeedlingFrame)
            {
                FullyGrownDay = Manager.World.Time.CurrentDate.AddHours(GrowthHours).AddDays(GrowthDays)
            }) as Seedling;
        }

        public void CreateMesh(ComponentManager manager)
        {
            PropogateTransforms();
            var mesh = AddChild(new InstanceMesh(manager, "Model", Matrix.CreateRotationY((float)(MathFunctions.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(MeshScale, MeshScale, MeshScale) * Matrix.CreateTranslation(GetBoundingBox().Center() - Position), MeshAsset, false));
            mesh.SetFlag(Flag.ShouldSerialize, false);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateMesh(manager);
            base.CreateCosmeticChildren(manager);
        }
    }
}
