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
    public class Seedling : Plant, IUpdateableComponent
    {
        public DateTime FullyGrownDay { get; set; }
        public String AdultName;

        public Seedling()
        {
            IsGrown = false;
        }

        public Seedling(ComponentManager Manager, String AdultName, Vector3 position, String Asset, int GrowthHours) :
            base(Manager, "seedling", position, 0.0f, Vector3.One, Asset, 1.0f)
        {
            FullyGrownDay = Manager.World.Time.CurrentDate.AddHours(GrowthHours);
            IsGrown = false;
            Name = AdultName + " seedling";
            this.AdultName = AdultName;
            AddChild(new Health(Manager, "HP", 1.0f, 0.0f, 1.0f));
            AddChild(new Flammable(Manager, "Flames"));
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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
            var adult = EntityFactory.CreateEntity<Plant>(AdultName, LocalPosition);
            adult.IsGrown = true;
            if (Farm != null)
            {
                Farm.Plant = adult;
                Farm.TriggerAutoHarvest();
                adult.Farm = Farm;
            }
            Die();
        }
    }
}
