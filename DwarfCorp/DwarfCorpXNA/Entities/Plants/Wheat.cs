// Wheat.cs
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
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Wheat : Plant
    {
        public Wheat()
        {
            
        }

        public Wheat(ComponentManager Manager, Vector3 position) :
            base(Manager, "Wheat", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(1.0f, 1.0f, 1.0f),  "wheat", 1.0f)
        {
            SeedlingAsset = "wheatsprout";

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)) as Inventory;

            for (int i = 0; i < MathFunctions.RandInt(1, 5); i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = ResourceType.Grain
                });
            }

            AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            });

            AddChild(new Health(Manager, "HP", 30, 0.0f, 30));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Wheat");
            Tags.Add("Vegetation");
            CollisionType = CollisionManager.CollisionType.Static;
            PropogateTransforms();
        }
    }
}
