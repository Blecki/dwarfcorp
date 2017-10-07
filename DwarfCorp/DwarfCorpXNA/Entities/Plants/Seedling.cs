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
}
