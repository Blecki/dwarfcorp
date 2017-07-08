// CreateCraftItemAct.cs
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

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class CreateCraftItemAct : CreatureAct
    {
        public Voxel Voxel { get; set; }
        public string ItemType { get; set; }
        public CreateCraftItemAct(Voxel voxel, CreatureAI agent, string itemType) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Create craft item";
            ItemType = itemType;
        }

        public override IEnumerable<Status> Run()
        {
            if (!Creature.Faction.CraftBuilder.IsDesignation(Voxel))
            {
                yield return Status.Fail;
            }

            Body item = EntityFactory.CreateEntity<Body>(CraftLibrary.CraftItems[ItemType].Name, Voxel.Position + Vector3.One * 0.5f);
            item.Tags.Add("Moveable");
            Creature.Faction.OwnedObjects.Add(item);
            Creature.Manager.World.ParticleManager.Trigger("puff", Voxel.Position + Vector3.One * 0.5f, Color.White, 10);
            if (item == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Creature.Faction.CraftBuilder.RemoveDesignation(Voxel);
                Creature.AI.AddXP(10);
                yield return Status.Success;
            }
        }
    }

}