// DeathComponentSpawner.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// When an entity dies, this component releases other components (such as resources)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DeathComponentSpawner : Body
    {
        public List<Body> Spawns { get; set; }
        public float ThrowSpeed { get; set; }

        public DeathComponentSpawner(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingExtents, Vector3 boundingBoxPos, List<Body> spawns) :
            base(Manager, name, localTransform, boundingExtents, boundingBoxPos, false)
        {
            Spawns = spawns;
            ThrowSpeed = 5.0f;
            AddToCollisionManager = false;
        }

        public override void Die()
        {
            if(IsDead)
            {
                return;
            }

            foreach(Body locatable in Spawns)
            {
                locatable.SetVisibleRecursive(true);
                locatable.SetActiveRecursive(true);
                locatable.HasMoved = true;
                locatable.WasAddedToOctree = false;
                locatable.AddToCollisionManager = true;

                var component = locatable as Physics;
                if(component != null)
                {
                    Vector3 radialThrow = MathFunctions.RandVector3Cube() * ThrowSpeed;
                    component.Velocity += radialThrow;
                }

                Manager.AddComponent(locatable);
                Manager.RootComponent.AddChild(locatable);
            }

            base.Die();
        }
    }

}