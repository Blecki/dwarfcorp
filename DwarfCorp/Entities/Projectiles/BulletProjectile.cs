// Projectile.cs
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
    public class BulletProjectile : Projectile
    {
        [EntityFactory("Bullet")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new BulletProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<Body>("Target", null));
        }

        public BulletProjectile()
        {

        }

        public BulletProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, ContentPaths.Particles.stone_particle, null, ContentPaths.Audio.Oscar.sfx_ic_dwarf_musket_bullet_explode_1, target)
        {
            HitAnimation = AnimationLibrary.CreateSimpleAnimation(ContentPaths.Effects.explode);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = AnimationLibrary.CreateSimpleAnimation(ContentPaths.Effects.explode);
        }
    }
}
