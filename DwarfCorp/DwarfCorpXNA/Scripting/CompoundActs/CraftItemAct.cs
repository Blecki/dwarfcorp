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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftBuilder.CraftDesignation Item { get; set; }
        public VoxelHandle Voxel { get; set; }
        public string Noise { get; set; }
        public CraftItemAct()
        {
            if (Item.ResourcesReservedFor != null && Item.ResourcesReservedFor.IsDead)
            {
                Item.ResourcesReservedFor = null;
            }
        }

        public IEnumerable<Status> ReserveResources()
        {
            if (Item.ResourcesReservedFor != null && Item.ResourcesReservedFor.IsDead)
            {
                Item.ResourcesReservedFor = null;
            }

            if (!Item.HasResources && Item.ResourcesReservedFor == null)
            {
                Item.ResourcesReservedFor = Agent;
            }
            yield return Status.Success;
        }

        public IEnumerable<Status> UnReserve()
        {
            if (Item.ResourcesReservedFor != null && Item.ResourcesReservedFor.IsDead)
            {
                Item.ResourcesReservedFor = null;
            }

            foreach (var status in Creature.Unreserve(Item.ItemType.CraftLocation))
            {
            
            }
            if (Item.ResourcesReservedFor == Agent)
                Item.ResourcesReservedFor = null;
            yield return Act.Status.Success;
        }

        public IEnumerable<Status> DestroyResources(Vector3 pos)
        {
            if (!Item.HasResources && Item.ResourcesReservedFor == Agent)
            {
                Creature.Inventory.RemoveAndCreateWithToss(Item.ItemType.SelectedResources, pos, Inventory.RestockType.None);
                Item.HasResources = true;
            }
            yield return Status.Success;
        }
        
        public IEnumerable<Status> WaitForResources()
        {
            if (Item.ResourcesReservedFor == Agent)
            {
                yield return Act.Status.Success;
                yield break;
            }

            while (!Item.HasResources)
            {
                if (Item.ResourcesReservedFor == null || Item.ResourcesReservedFor.IsDead)
                {
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> CreateResources()
        {
            List<ResourceAmount> stashed = Agent.Blackboard.GetData<List<ResourceAmount>>("ResourcesStashed");

            if (stashed == null)
            {
                yield return Act.Status.Fail;
                yield break;
            }

            Item.ItemType.SelectedResources = stashed;
            if (Item.ItemType.Name == ResourceLibrary.ResourceType.Trinket)
            {
                Resource craft = ResourceLibrary.GenerateTrinket(stashed.ElementAt(0).ResourceType,
                    (Agent.Stats.Dexterity + Agent.Stats.Intelligence)/15.0f*MathFunctions.Rand(0.5f, 1.75f));
                Item.ItemType.ResourceCreated = craft.Type;
            }
            else if (Item.ItemType.Name == ResourceLibrary.ResourceType.Meal)
            {
                if (stashed.Count < 2)
                {
                    yield return Act.Status.Fail;
                    yield break;
                }
                Resource craft = ResourceLibrary.CreateMeal(stashed.ElementAt(0).ResourceType, stashed.ElementAt(1).ResourceType);
                Item.ItemType.ResourceCreated = craft.Type;
            }
            else if (Item.ItemType.Name == ResourceLibrary.ResourceType.Ale)
            {
                Resource craft = ResourceLibrary.CreateAle(stashed.ElementAt(0).ResourceType);
                Item.ItemType.ResourceCreated = craft.Type;
            }
            else if (Item.ItemType.Name == ResourceLibrary.ResourceType.Bread)
            {
                Resource craft = ResourceLibrary.CreateBread(stashed.ElementAt(0).ResourceType);
                Item.ItemType.ResourceCreated = craft.Type;
            }
            else if (Item.ItemType.Name == ResourceLibrary.ResourceType.GemTrinket)
            {
                Resource gem = null;
                Resource trinket = null;
                foreach (ResourceAmount stashedResource in stashed)
                {
                    if (ResourceLibrary.GetResourceByName(stashedResource.ResourceType).Tags.Contains(Resource.ResourceTags.Craft))
                    {
                        trinket = ResourceLibrary.GetResourceByName(stashedResource.ResourceType);
                    }

                    if (ResourceLibrary.GetResourceByName(stashedResource.ResourceType).Tags.Contains(Resource.ResourceTags.Gem))
                    {
                        gem = ResourceLibrary.GetResourceByName(stashedResource.ResourceType);
                    }
                }


                if (gem == null || trinket == null)
                {
                    yield return Status.Fail;
                    yield break;
                }

                Resource craft = ResourceLibrary.EncrustTrinket(trinket.Type, gem.Type);
                Item.ItemType.ResourceCreated = craft.Type;
            }

            Resource resource = ResourceLibrary.Resources[Item.ItemType.ResourceCreated];
            Creature.Inventory.AddResource(new ResourceAmount(resource, 1));
            Creature.AI.AddXP((int)Item.ItemType.BaseCraftTime);
            yield return Status.Success;
        }


        public CraftItemAct(CreatureAI creature, CraftBuilder.CraftDesignation type) :
            base(creature)
        {
            Item = type;
            Voxel = type.Location;
            Name = "Build craft item";
        }

        public bool IsNotCancelled()
        {
            return Creature.Faction.CraftBuilder.IsDesignation(Voxel);
        }

        public bool ResourceStateValid()
        {
            return Item.HasResources || Item.ResourcesReservedFor != null;
        }


        public override void Initialize()
        {
            Act unreserveAct = new Wrap(UnReserve);
            float time = 3 * (Item.ItemType.BaseCraftTime / Creature.AI.Stats.BuffedInt);
            Act getResources = null;
            if (Item.ItemType.SelectedResources == null || Item.ItemType.SelectedResources.Count == 0)
            {
                getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                          new Domain(() => !Item.HasResources && (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                                     new Sequence(new Wrap(ReserveResources), new GetResourcesAct(Agent, Item.ItemType.RequiredResources))),
                                          new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            }
            else
            {
                getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                          new Domain(() => !Item.HasResources && (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                                     new Sequence(new Wrap(ReserveResources), new GetResourcesAct(Agent, Item.ItemType.SelectedResources))),
                                          new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            }

            if (Item.ItemType.Type == CraftItem.CraftType.Object)
            {
                if (!String.IsNullOrEmpty(Item.ItemType.CraftLocation))
                {
                    Tree = new Domain(IsNotCancelled, new Sequence(
                        new Wrap(() => Creature.FindAndReserve(Item.ItemType.CraftLocation, Item.ItemType.CraftLocation)),
                        getResources,
                        new Domain(ResourceStateValid, new Sequence
                            (
                            new GoToTaggedObjectAct(Agent)
                            {
                                Tag = Item.ItemType.CraftLocation,
                                Teleport = true,
                                TeleportOffset = new Vector3(0.5f, -0.5f, 0),
                                ObjectName = Item.ItemType.CraftLocation,
                                CheckForOcclusion = true
                            },
                            new Wrap(() => DestroyResources(Item.Location.WorldPosition)),
                            new Wrap(WaitForResources) { Name = "Wait for resources."},
                            new Wrap(() => Creature.HitAndWait(true, () => 1.0f, 
                            () => Item.Progress, () => Item.Progress += Creature.Stats.BuildSpeed / Item.ItemType.BaseCraftTime,
                            () => Item.Location.WorldPosition + Vector3.One * 0.5f, "Craft")),
                            new CreateCraftItemAct(Voxel, Creature.AI, Item))),
                            unreserveAct
                            ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                        ) | new Sequence(unreserveAct, false);
                }
                else
                {
                    Tree = new Domain(IsNotCancelled, new Sequence(
                        getResources,
                        new Sequence(new Domain(ResourceStateValid, 
                            new Sequence(
                                new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                                new Wrap(() => DestroyResources(Item.Location.WorldPosition)),
                                new Wrap(WaitForResources) { Name = "Wait for resources." },
                                new Wrap(() => Creature.HitAndWait(true, () => 1.0f,
                                         () => Item.Progress, () => Item.Progress += Creature.Stats.BuildSpeed / Item.ItemType.BaseCraftTime,
                                         () => Item.Location.WorldPosition + Vector3.One * 0.5f, "Craft")) { Name = "Construct object." },
                                new CreateCraftItemAct(Voxel, Creature.AI, Item)
                            )
                        ))
                     )) |
                       new Sequence(new Wrap(Creature.RestockAll), unreserveAct, false);
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(Item.ItemType.CraftLocation))
                {
                    Tree = new Sequence(
                        new Wrap(() => Creature.FindAndReserve(Item.ItemType.CraftLocation, Item.ItemType.CraftLocation)),
                        getResources,
                        new Domain(ResourceStateValid, new Sequence
                            (
                            new GoToTaggedObjectAct(Agent)
                            {
                                Tag = Item.ItemType.CraftLocation,
                                Teleport = true,
                                TeleportOffset = new Vector3(0.5f, -0.5f, 0),
                                ObjectName = Item.ItemType.CraftLocation,
                                CheckForOcclusion = true
                            },
                            new Wrap(() => DestroyResources(Agent.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            new Wrap(() => Creature.HitAndWait(true, () => 1.0f,
                                () => Item.Progress, () => Item.Progress += Creature.Stats.BuildSpeed / Item.ItemType.BaseCraftTime,
                                () => Agent.Position, Noise)) { Name = "Construct object." },
                            unreserveAct,
                            new Wrap(CreateResources),
                            new Wrap(Creature.RestockAll)
                            )) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                        ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
                }
                else
                {
                    Tree = new Sequence(
                        getResources,
                        new Domain(ResourceStateValid, new Sequence(
                            new Wrap(() => DestroyResources(Creature.Physics.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            new Wrap(() => Creature.HitAndWait(time, true, () => Creature.Physics.Position)) { Name = "Construct object."},
                            new Wrap(CreateResources))
                        )
                    ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
                }
            }
            base.Initialize();
        }


        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            foreach (var statuses in Creature.Unreserve(Item.ItemType.CraftLocation))
            {
                continue;
            }
            if (Item.ResourcesReservedFor == Agent)
            {
                Item.ResourcesReservedFor = null;
            }
            base.OnCanceled();
        }

       
    }
}