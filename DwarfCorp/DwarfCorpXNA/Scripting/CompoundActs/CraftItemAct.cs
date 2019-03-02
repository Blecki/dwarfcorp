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
        public CraftDesignation Item { get; set; }
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
            Agent.Physics.Active = true;
            Agent.Physics.IsSleeping = false;

            if (Agent.Blackboard.GetData<bool>("NoPath", false))
            {
                if (Item.Entity != null && !Item.Entity.IsDead)
                {
                    var designation = Agent.Faction.Designations.GetEntityDesignation(Item.Entity, DesignationType.Craft);
                    if (designation != null)
                    {
                        if (Agent.Faction == Agent.World.PlayerFaction)
                        {
                            Agent.World.MakeAnnouncement(String.Format("{0} cancelled crafting {1} because it is unreachable", Agent.Stats.FullName, Item.Entity.Name));
                            Agent.World.Master.TaskManager.CancelTask(designation.Task);
                        }
                    }
                }
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> DestroyResources(Func<Vector3> pos)
        {
            if (!Item.HasResources && Item.ResourcesReservedFor == Agent)
            {
                if (Item.ExistingResource != null)
                {
                    if (!Creature.Inventory.RemoveAndCreateWithToss(new List<ResourceAmount>() { new ResourceAmount(Item.ExistingResource) }, pos(), Inventory.RestockType.None))
                    {
                        Agent.SetMessage("Failed to create resources for item (1).");
                        yield return Act.Status.Fail;
                        yield break;
                    }
                }
                else if (!Creature.Inventory.RemoveAndCreateWithToss(Item.SelectedResources, pos(), Inventory.RestockType.None))
                {
                    Agent.SetMessage("Failed to create resources for item (2).");
                    yield return Act.Status.Fail;
                    yield break;
                }
                
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

            WanderAct wander = new WanderAct(Agent, 60.0f, 5.0f, 1.0f);
            wander.Initialize();
            var enumerator = wander.Enumerator;
            while (!Item.HasResources)
            {
                enumerator.MoveNext();
                if (Item.ResourcesReservedFor == null || Item.ResourcesReservedFor.IsDead)
                {
                    Agent.SetMessage("Waiting for resources failed.");
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> MaybeCreatePreviewBody(List<ResourceAmount> stashed)
        {
            if (stashed == null || stashed.Count == 0)
            {
                Agent.SetMessage("Failed to create resources.");
                yield return Act.Status.Fail;
                yield break;
            }

            if (Item.PreviewResource != null)
            {
                Item.PreviewResource.SetFlagRecursive(GameComponent.Flag.Visible, true);
                yield return Act.Status.Success;
                yield break;
            }

            Item.SelectedResources = stashed;
            String ResourceCreated = Item.ItemType.ResourceCreated;

            switch (Item.ItemType.CraftActBehavior)
            {
                // Todo: This switch sucks.
                case CraftItem.CraftActBehaviors.Object:
                    {
                        Resource craft = Item.ItemType.ToResource(Creature.World, stashed);
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.Trinket:
                    {
                        Resource craft = ResourceLibrary.GenerateTrinket(stashed.ElementAt(0).Type,
                            (Agent.Stats.Dexterity + Agent.Stats.Intelligence) / 15.0f * MathFunctions.Rand(0.5f, 1.75f));
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.Meal:
                    {
                        if (stashed.Count < 2)
                        {
                            Agent.SetMessage("Failed to get resources for meal.");
                            yield return Act.Status.Fail;
                            yield break;
                        }
                        Resource craft = ResourceLibrary.CreateMeal(stashed.ElementAt(0).Type, stashed.ElementAt(1).Type);
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.Ale:
                    {
                        Resource craft = ResourceLibrary.CreateAle(stashed.ElementAt(0).Type);
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.Bread:
                    {
                        Resource craft = ResourceLibrary.CreateBread(stashed.ElementAt(0).Type);
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.GemTrinket:
                    {
                        Resource gem = null;
                        Resource trinket = null;
                        foreach (ResourceAmount stashedResource in stashed)
                        {
                            if (ResourceLibrary.GetResourceByName(stashedResource.Type).Tags.Contains(Resource.ResourceTags.Craft))
                                trinket = ResourceLibrary.GetResourceByName(stashedResource.Type);

                            if (ResourceLibrary.GetResourceByName(stashedResource.Type).Tags.Contains(Resource.ResourceTags.Gem))
                                gem = ResourceLibrary.GetResourceByName(stashedResource.Type);
                        }


                        if (gem == null || trinket == null)
                        {
                            Agent.SetMessage("Failed to get resources for trinket.");
                            yield return Status.Fail;
                            yield break;
                        }

                        Resource craft = ResourceLibrary.EncrustTrinket(trinket.Name, gem.Name);
                        ResourceCreated = craft.Name;
                    }
                    break;
                case CraftItem.CraftActBehaviors.Normal:
                default:
                    break;
            }

            Resource resource = ResourceLibrary.Resources[ResourceCreated];
            Item.PreviewResource = EntityFactory.CreateEntity<ResourceEntity>(resource.Name + " Resource", Agent.Position);
            Item.PreviewResource.SetFlagRecursive(GameComponent.Flag.Active, false);
            Item.PreviewResource.SetVertexColorRecursive(new Color(200, 200, 255, 200));
            yield return Status.Success;
        }

        public IEnumerable<Status> CreateResources(List<ResourceAmount> stashed)
        {
            foreach (var status in MaybeCreatePreviewBody(stashed))
            {
                if (status == Status.Fail)
                {
                    yield return Status.Fail;
                    yield break;
                }
            }
            Creature.Inventory.AddResource(new ResourceAmount(Item.PreviewResource.Resource.Type, Item.ItemType.CraftedResultsCount));
            Item.PreviewResource.Delete();
            Item.PreviewResource = null;
            Creature.AI.AddXP((int)Item.ItemType.BaseCraftTime);
            Item.Finished = true;
            yield return Status.Success;
        }


        public CraftItemAct(CreatureAI creature, CraftDesignation type) :
            base(creature)
        {
            Item = type;
            Voxel = type.Location;
            Name = "Build craft item";
        }

        public bool IsNotCancelled()
        {
            return Creature.Faction.Designations.IsDesignation(Item.Entity, DesignationType.Craft);
        }

        public bool ResourceStateValid()
        {
            bool valid =  Item.HasResources || Item.ResourcesReservedFor != null;
            if (!valid)
            {
                Agent.SetMessage("Resource state not valid.");
            }
            return valid;
        }

        public IEnumerable<Act.Status> SetSelectedResources()
        {
            if (Item.ExistingResource != null)
            {
                yield return Act.Status.Success;
                yield break;
            }
            Item.SelectedResources.Clear();
            foreach (var resource in Item.ItemType.RequiredResources)
            {
                if (Creature.Inventory.HasResource(resource))
                {
                    var matchingResources = Creature.Inventory.GetResources(resource, Inventory.RestockType.Any);
                    for (int i = 0; i < resource.Count; i++)
                    {
                        foreach(var matching in matchingResources)
                        {
                            int numSelected = Math.Min(matching.Count, resource.Count - i);
                            Item.SelectedResources.Add(new ResourceAmount(matching.Type, numSelected));
                            i += numSelected;
                            if (i >= resource.Count)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Agent.SetMessage("Failed to set selected resources.");
                    yield return Act.Status.Fail;
                }
            }
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(UnReserve);
            float time = 3 * (Item.ItemType.BaseCraftTime / Creature.AI.Stats.Intelligence);
            bool factionHasResources = Item.SelectedResources != null && Item.SelectedResources.Count > 0 && Creature.AI.Faction.HasResources(Item.SelectedResources);
            Act getResources = null;
            if (Item.ExistingResource != null)
            {
                getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                          new Domain(() => !Item.HasResources &&
                                                           (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                                     new Select(
                                                            new Sequence(new Wrap(ReserveResources),
                                                                         new GetResourcesAct(Agent, new List<ResourceAmount>() { new ResourceAmount(Item.ExistingResource) } ),
                                                                         new Wrap(SetSelectedResources)),
                                                            new Sequence(new Wrap(UnReserve), Act.Status.Fail)
                                                            )
                                                    ),
                                          new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            }
            else if (!factionHasResources)
            {
                getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                          new Domain(() => !Item.HasResources &&
                                                           (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                                     new Select(
                                                            new Sequence(new Wrap(ReserveResources), 
                                                                         new GetResourcesAct(Agent, Item.ItemType.RequiredResources) { AllowHeterogenous = false }, 
                                                                         new Wrap(SetSelectedResources)),
                                                            new Sequence(new Wrap(UnReserve), Act.Status.Fail)
                                                            )
                                                    ),
                                          new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            }
            else
            {
                getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                          new Domain(() => !Item.HasResources && (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                                     new Sequence(new Wrap(ReserveResources), new GetResourcesAct(Agent, Item.SelectedResources) { AllowHeterogenous = false }) | (new Wrap(UnReserve)) & false),
                                          new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            }

            if (Item.ItemType.Type == CraftItem.CraftType.Object)
            {
                Act buildAct = null;

                if (Item.ExistingResource != null)
                {
                    buildAct = new Always(Status.Success);
                }
                else
                {
                    buildAct = new Wrap(() => Creature.HitAndWait(true, () => 1.0f,
                                        () => Item.Progress, () => Item.Progress += Creature.Stats.BuildSpeed / Item.ItemType.BaseCraftTime, // Todo: Account for creature debuffs, environment buffs
                                        () => Item.Location.WorldPosition + Vector3.One * 0.5f, "Craft"))
                                                { Name = "Construct object." };
                }

                Tree = new Domain(IsNotCancelled, new Sequence(
                    new ClearBlackboardData(Agent, "ResourcesStashed"),
                    getResources,
                    new Sequence(new Domain(ResourceStateValid, 
                        new Sequence(
                            new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                            new Wrap(() => DestroyResources(() => Item.Location.WorldPosition)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            buildAct,
                            new CreateCraftItemAct(Voxel, Creature.AI, Item)
                        )
                    ))
                    )) |
                    new Sequence(new Wrap(Creature.RestockAll), unreserveAct, false);
                
            }
            else if (Item.ItemType.Type == CraftItem.CraftType.Resource)
            {
                if (!String.IsNullOrEmpty(Item.ItemType.CraftLocation))
                {
                    Tree = new Sequence(
                        new Wrap(() => Creature.FindAndReserve(Item.ItemType.CraftLocation, Item.ItemType.CraftLocation)),
                        new ClearBlackboardData(Agent, "ResourcesStashed"),
                        getResources,
                        new Domain(ResourceStateValid, 
                            new Sequence(
                                new GoToTaggedObjectAct(Agent)
                                {
                                    Tag = Item.ItemType.CraftLocation,
                                    Teleport = true,
                                    TeleportOffset = new Vector3(0.5f, 0.0f, 0),
                                    ObjectName = Item.ItemType.CraftLocation,
                                    CheckForOcclusion = true
                                },
                                new Wrap(() => DestroyResources(() => Agent.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                                new Wrap(WaitForResources) { Name = "Wait for resources." },
                                new Wrap(() => MaybeCreatePreviewBody(Item.SelectedResources)),
                                new Wrap(() => Creature.HitAndWait(true, 
                                    () => 1.0f, // Max Progress
                                    () => Item.Progress, // Current Progress
                                    () => { // Increment Progress
                                        var location = Creature.AI.Blackboard.GetData<Body>(Item.ItemType.CraftLocation);
                                        float workstationBuff = 1.0f;
                                        if (location != null)
                                        {
                                            Creature.Physics.Face(location.Position);
                                            if (Item.PreviewResource != null)
                                                Item.PreviewResource.LocalPosition = location.Position + Vector3.Up * 0.25f;
                                            var buff = location.GetComponent<SteamPipes.BuildBuff>();
                                            if (buff != null)
                                                workstationBuff = buff.BuffMultiplier;
                                        }

                                        // Todo: Account for environment buff & 'anvil' buff.

                                        Item.Progress += (Creature.Stats.BuildSpeed * workstationBuff) / Item.ItemType.BaseCraftTime;
                                    },
                                    () => { // Get Position
                                        var location = Creature.AI.Blackboard.GetData<Body>(Item.ItemType.CraftLocation);
                                        if (location != null)
                                            return location.Position;
                                        return Agent.Position;
                                    }, 
                                    Noise)) { Name = "Construct object." },
                                unreserveAct,
                                new Wrap(() => CreateResources(Item.SelectedResources)),
                                new Wrap(Creature.RestockAll)
                                )) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                            ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
                }
                else
                {
                    Tree = new Sequence(
                        new ClearBlackboardData(Agent, "ResourcesStashed"),
                        getResources,
                        new Domain(ResourceStateValid, new Sequence(
                            new Wrap(() => DestroyResources(() => Creature.Physics.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            new Wrap(() => Creature.HitAndWait(time, true, () => Creature.Physics.Position)) { Name = "Construct object."},
                            new Wrap(() => CreateResources(Item.SelectedResources)))
                        )
                    ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
                }
            }
            base.Initialize();
        }


        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            Creature.Physics.IsSleeping = false;
            foreach (var statuses in Creature.Unreserve(Item.ItemType.CraftLocation))
            {
                continue;
            }
            if (Item.ResourcesReservedFor == Agent)
            {
                Item.ResourcesReservedFor = null;
            }

            if (Item.PreviewResource != null)
            {
                Item.PreviewResource.SetFlagRecursive(GameComponent.Flag.Visible, false);
            }
            base.OnCanceled();
        }

       
    }
}