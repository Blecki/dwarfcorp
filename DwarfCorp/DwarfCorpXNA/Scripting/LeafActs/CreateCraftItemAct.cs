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
        public VoxelHandle Voxel { get; set; }
        public CraftDesignation Item { get; set; }

        public CreateCraftItemAct(VoxelHandle voxel, CreatureAI agent, CraftDesignation itemType) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Create craft item";
            Item = itemType;
        }

        public override IEnumerable<Status> Run()
        {
            Item.Finished = true;
            var item = Item.Entity;
            item.SetFlagRecursive(GameComponent.Flag.Active, true);
            item.SetVertexColorRecursive(Color.White);
            var tinters = item.EnumerateAll().OfType<Tinter>();
            foreach(var tinter in tinters)
                tinter.Stipple = false;

            item.SetFlagRecursive(GameComponent.Flag.Visible, true);

            if (Item.ItemType.Moveable)
                item.Tags.Add("Moveable");

            if (Item.ItemType.Deconstructable)
                item.Tags.Add("Deconstructable");

            if (Item.WorkPile != null)
                Item.WorkPile.Die();

            CraftDetails details = item.GetComponent<CraftDetails>();
            
            if (details == null)
            {
                float strength = Item.SelectedResources.Average(r => ResourceLibrary.GetResourceByName(r.ResourceType).MaterialStrength);
                var health = item.GetRoot().GetComponent<Health>();
                if (health != null)
                {
                    health.MaxHealth = strength;
                    health.Hp = strength;
                }
                item.AddChild(new CraftDetails(Creature.Manager)
                {
                    Resources = Item.SelectedResources.ConvertAll(p => new ResourceAmount(p)),
                    CraftType = Item.ItemType.Name
                });

                if (Item.SelectedResources.Count > 0)
                    item.Name = Item.SelectedResources.FirstOrDefault().ResourceType + " " + item.Name;
            }
            else
            {
                details.CraftType = Item.ItemType.Name;
                details.Resources = Item.SelectedResources.ConvertAll(p => new ResourceAmount(p));
            }

            if (Item.ItemType.AddToOwnedPool)
                Creature.Faction.OwnedObjects.Add(item);

            Creature.Manager.World.ParticleManager.Trigger("puff", Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, 10);
            Creature.AI.AddXP((int)(5 * (Item.ItemType.BaseCraftTime / Creature.AI.Stats.BuffedInt)));
            yield return Status.Success;
        }
    }

}