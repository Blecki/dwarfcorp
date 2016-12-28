// CraftItemAct.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftItemAct()
        {
        }

        public CraftItemAct(CreatureAI creature, CraftItem type) :
            base(creature)
        {
            ItemType = type;
            Voxel = null;
            Name = "Build craft item";
        }

        public CraftItemAct(CreatureAI creature, Voxel voxel, CraftItem type) :
            base(creature)
        {
            ItemType = type;
            Voxel = voxel;
            Name = "Build craft item";
        }

        public CraftItem ItemType { get; set; }
        public Voxel Voxel { get; set; }

        public IEnumerable<Status> DestroyResources()
        {
            Creature.Inventory.Remove(ItemType.SelectedResources);
            yield return Status.Success;
        }


        public IEnumerable<Status> CreateResources()
        {
            var stashed = Agent.Blackboard.GetData<List<ResourceAmount>>("ResourcesStashed");
            ItemType.SelectedResources = stashed;
            if (ItemType.Name == ResourceLibrary.ResourceType.Trinket)
            {
                Resource craft = ResourceLibrary.GenerateTrinket(stashed.ElementAt(0).ResourceType.Type,
                    (Agent.Stats.Dexterity + Agent.Stats.Intelligence)/15.0f*MathFunctions.Rand(0.5f, 1.75f));
                ItemType.ResourceCreated = craft.Type;
            }
            else if (ItemType.Name == ResourceLibrary.ResourceType.Meal)
            {
                if (stashed.Count < 2)
                {
                    yield return Status.Fail;
                    yield break;
                }
                Resource craft = ResourceLibrary.CreateMeal(stashed.ElementAt(0).ResourceType,
                    stashed.ElementAt(1).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            else if (ItemType.Name == ResourceLibrary.ResourceType.Ale)
            {
                Resource craft = ResourceLibrary.CreateAle(stashed.ElementAt(0).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            else if (ItemType.Name == ResourceLibrary.ResourceType.Bread)
            {
                Resource craft = ResourceLibrary.CreateBread(stashed.ElementAt(0).ResourceType);
                ItemType.ResourceCreated = craft.Type;
            }
            else if (ItemType.Name == ResourceLibrary.ResourceType.GemTrinket)
            {
                Resource gem = null;
                Resource trinket = null;
                foreach (ResourceAmount stashedResource in stashed)
                {
                    if (stashedResource.ResourceType.Tags.Contains(Resource.ResourceTags.Craft))
                    {
                        trinket = stashedResource.ResourceType;
                    }

                    if (stashedResource.ResourceType.Tags.Contains(Resource.ResourceTags.Gem))
                    {
                        gem = stashedResource.ResourceType;
                    }
                }


                if (gem == null || trinket == null)
                {
                    yield return Status.Fail;
                    yield break;
                }

                Resource craft = ResourceLibrary.EncrustTrinket(trinket, gem.Type);
                ItemType.ResourceCreated = craft.Type;
            }

            Resource resource = ResourceLibrary.Resources[ItemType.ResourceCreated];
            Creature.Inventory.Resources.AddResource(new ResourceAmount(resource, 1));
            yield return Status.Success;
        }


        public override void Initialize()
        {
            Act unreserveAct = new Wrap(() => Creature.Unreserve(ItemType.CraftLocation));
            float time = ItemType.BaseCraftTime/Creature.AI.Stats.BuffedInt;
            Act getResources = null;
            if (ItemType.SelectedResources == null || ItemType.SelectedResources.Count == 0)
            {
                getResources = new GetResourcesAct(Agent, ItemType.RequiredResources);
            }
            else
            {
                getResources = new GetResourcesAct(Agent, ItemType.SelectedResources);
            }

            if (ItemType.Type == CraftItem.CraftType.Object)
            {
                Tree = new Sequence(
                    new Wrap(() => Creature.FindAndReserve(ItemType.CraftLocation, ItemType.CraftLocation)),
                    getResources,
                    new Sequence
                        (
                        new GoToTaggedObjectAct(Agent)
                        {
                            Tag = ItemType.CraftLocation,
                            Teleport = false,
                            TeleportOffset = new Vector3(1, 0, 0),
                            ObjectName = ItemType.CraftLocation
                        },
                        new Wrap(() => Creature.HitAndWait(time, true)),
                        new Wrap(DestroyResources),
                        unreserveAct,
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                        new CreateCraftItemAct(Voxel, Creature.AI, ItemType.Name)
                        ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                    ) | new Sequence(unreserveAct, false);
            }
            else
            {
                Tree = new Sequence(
                    new Wrap(() => Creature.FindAndReserve(ItemType.CraftLocation, ItemType.CraftLocation)),
                    getResources,
                    new Sequence
                        (
                        new GoToTaggedObjectAct(Agent)
                        {
                            Tag = ItemType.CraftLocation,
                            Teleport = false,
                            TeleportOffset = new Vector3(1, 0, 0),
                            ObjectName = ItemType.CraftLocation
                        },
                        new Wrap(() => Creature.HitAndWait(time, true)),
                        new Wrap(DestroyResources),
                        unreserveAct,
                        new Wrap(CreateResources),
                        new Wrap(Creature.RestockAll)
                        ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                    ) | new Sequence(unreserveAct, false);
            }
            base.Initialize();
        }


        public override void OnCanceled()
        {
            foreach (Status statuses in Creature.Unreserve(ItemType.CraftLocation))
            {
            }
            base.OnCanceled();
        }
    }
}