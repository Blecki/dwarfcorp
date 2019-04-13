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
    {
        public Timer HurtTimer { get; set; }

        public Tree() { }

        public Tree(string name, ComponentManager manager, Vector3 position, string asset, String seed, float treeSize, bool emitWood = true) :
            base(manager, name, position, MathFunctions.Rand(-0.1f, 0.1f),
                new Vector3(
                    PrimitiveLibrary.Primitives[asset].Width * 0.75f * treeSize,
                    PrimitiveLibrary.Primitives[asset].Height * treeSize,
                    PrimitiveLibrary.Primitives[asset].Width * 0.75f * treeSize),
             asset, treeSize)
        {
            HurtTimer = new Timer(1.0f, false);

            AddChild(new Health(Manager, "HP", 100.0f * treeSize, 0.0f, 100.0f * treeSize));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Vegetation");
            if (emitWood)
                Tags.Add("EmitsWood");

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBoxSize, LocalBoundingBoxOffset)) as Inventory;

            // Can these be spawned when the tree dies rather than when it is created?
            if (emitWood)
            {
                var wood = new Resource(ResourceLibrary.Resources[ResourceType.Wood]);
                wood.Name = String.Format("{0} Wood", Name.Split(' ').First());
                wood.ShortName = wood.Name;
                if (!ResourceLibrary.Resources.ContainsKey(wood.Name))
                {
                    ResourceLibrary.Add(wood);
                }

                for (int i = 0; i < treeSize * 2; i++)
                {
                    inventory.Resources.Add(new Inventory.InventoryItem()
                    {
                        MarkedForRestock = false,
                        MarkedForUse = false,
                        Resource = wood.Name
                    });
                }
            }

            for (int i = 0; i < treeSize * 2; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = seed
                });
            }

            AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_tree_cut_down_1
            });

            CollisionType = CollisionType.Static;
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
    }
}
