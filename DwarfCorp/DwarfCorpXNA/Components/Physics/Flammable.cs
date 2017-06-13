// Flammable.cs
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
    /// <summary>
    /// Component causes the object its attached to to become flammable. Flammable objects have "heat"
    /// when the heat is above a "flashpoint" they get damaged until they are destroyed, and emit flames.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Flammable : GameComponent, IUpdateableComponent
    {
        public Health Health { get; set; }

        public float Heat { get; set; }
        public float Flashpoint { get; set; }
        public float Damage { get; set; }

        public Timer CheckLavaTimer { get; set; }
        public Timer SoundTimer { get; set; }
        public Timer DamageTimer { get; set; }

        public Flammable()
        {
            
        }

        // Todo: Discover health component rather than passing it in.
        public Flammable(ComponentManager manager, string name, Health health) :
            base(name, manager)
        {
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 5.0f;
            Health = health;
            CheckLavaTimer = new Timer(1.0f, false);
            SoundTimer = new Timer(1.0f, false);
            DamageTimer = new Timer(1.0f, false);
        }


        public void CheckForLavaAndWater(Body Body, DwarfTime gameTime, ChunkManager chunks)
        {
            BoundingBox expandedBoundingBox = Body.BoundingBox.Expand(0.5f);

            List<Voxel> voxels = chunks.GetVoxelsIntersecting(expandedBoundingBox);

            foreach(Voxel currentVoxel in voxels)
            {
                WaterCell cell = currentVoxel.Water;

                if (cell.WaterLevel == 0) continue;
                else if (cell.Type == LiquidType.Lava)
                {
                    Heat += 100;
                }
                else if (cell.Type == LiquidType.Water)
                {
                    Heat -= 100;
                    Heat = Math.Max(0.0f, Heat);
                }
            }
        }

        public int GetNumTrigger(Body Body)
        {
            return
                (int)
                    MathFunctions.Clamp((int) (Math.Abs(1*Body.BoundingBox.Max.Y - Body.BoundingBox.Min.Y)), 1,
                        3);
        }

        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!IsActive) return;

            var body = Parent as Body;
            System.Diagnostics.Debug.Assert(body != null);

            DamageTimer.Update(gameTime);
            CheckLavaTimer.Update(gameTime);
            SoundTimer.Update(gameTime);
            if(CheckLavaTimer.HasTriggered)
            {
                CheckForLavaAndWater(body, gameTime, chunks);
            }
            Heat *= 0.999f;

            if(Heat > Flashpoint)
            {
                if(DamageTimer.HasTriggered)
                    Health.Damage(Damage, Health.DamageType.Fire);

                if(SoundTimer.HasTriggered)
                    SoundManager.PlaySound(ContentPaths.Audio.fire, body.Position, true, 1.0f);
                double totalSize = (body.BoundingBox.Max - body.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 4.0f) + 1;

                for(int i = 0; i < numFlames; i++)
                {
                    Vector3 extents = (body.BoundingBox.Max - body.BoundingBox.Min);
                    Vector3 randomPoint = body.BoundingBox.Min + new Vector3(extents.X * MathFunctions.Rand(), extents.Y * MathFunctions.Rand(), extents.Z * MathFunctions.Rand());
                    Manager.World.ParticleManager.Trigger("flame", randomPoint, Color.White, GetNumTrigger(body));
                }
            }
        }
    }

}