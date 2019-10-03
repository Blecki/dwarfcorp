using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftDesignation Item { get; set; }
        public Stockpile ItemSource;
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

            if (Item.PreviewResource != null)
            {
                Item.PreviewResource.Delete();
                Item.PreviewResource = null;
            }

            foreach (var status in Creature.Unreserve(Item.ItemType.CraftLocation))
            {
            
            }

            if (Item.ResourcesReservedFor == Agent)
                Item.ResourcesReservedFor = null;

            Agent.Physics.Active = true;
            Agent.Physics.IsSleeping = false;

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> DestroyResources(Func<Vector3> pos)
        {
            if (!Item.HasResources && Item.ResourcesReservedFor == Agent)
            {
                if (Item.SelectedResource != null)
                {
                    if (!Creature.Inventory.RemoveAndCreateWithToss(Item.SelectedResource, pos(), Inventory.RestockType.None))
                    {
                        Agent.SetMessage("Failed to create resources for item (1).");
                        yield return Act.Status.Fail;
                        yield break;
                    }
                }
                else if (!Creature.Inventory.RemoveAndCreateWithToss(Item.SelectedResource, pos(), Inventory.RestockType.None))
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

        public CraftItemAct(CreatureAI creature, CraftDesignation type) :
            base(creature)
        {
            Item = type;
            Voxel = type.Location;
            Name = "Build craft item";
        }

        public bool IsNotCancelled()
        {
            return Creature.World.PersistentData.Designations.IsDesignation(Item.Entity, DesignationType.Craft);
        }

        public bool ResourceStateValid()
        {
            bool valid =  Item.HasResources || Item.ResourcesReservedFor != null;
            if (!valid)
                Agent.SetMessage("Resource state not valid.");

            var location = Creature.AI.Blackboard.GetData<GameComponent>(Item.ItemType.CraftLocation);
            if (location != null && location.IsDead)
                return false;

            return valid;
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(UnReserve);
            float time = 3 * (Item.ItemType.BaseCraftTime / Creature.AI.Stats.Intelligence);
            Act getResources = null;

            getResources = new Select(new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true),
                                        new Domain(() => !Item.HasResources && (Item.ResourcesReservedFor == Agent || Item.ResourcesReservedFor == null),
                                            new Sequence(
                                                new Wrap(ReserveResources),
                                                new StashResourcesAct(Agent, ItemSource, Item.SelectedResource))
                                            | (new Wrap(UnReserve))
                                            & false),
                                        new Domain(() => Item.HasResources || Item.ResourcesReservedFor != null, true));
            Act buildAct = null;

            buildAct = new Wrap(() => Creature.HitAndWait(true, () => 1.0f,
                                () => Item.Progress, () => Item.Progress += (Creature.Stats.BuildSpeed * 8) / Item.ItemType.BaseCraftTime, // Todo: Account for creature debuffs, environment buffs
                                () => Item.Location.WorldPosition + Vector3.One * 0.5f, "Craft"))
            { Name = "Construct object." };

            Tree = new Domain(IsNotCancelled, new Sequence(
                new ClearBlackboardData(Agent, "ResourcesStashed"),
                getResources,
                new Sequence(new Domain(ResourceStateValid,
                    new Sequence(
                        ActHelper.CreateToolCheckAct(Agent, "Hammer"),
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                        new Wrap(() => DestroyResources(() => Item.Location.WorldPosition)),
                        new Wrap(WaitForResources) { Name = "Wait for resources." },
                        buildAct,
                        new CreateCraftItemAct(Voxel, Creature.AI, Item)
                    )
                ))
                )) |
                new Sequence(new Wrap(Creature.RestockAll), unreserveAct, false);

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
                Item.PreviewResource.Delete();
                Item.PreviewResource = null;
            }

            base.OnCanceled();
        }       
    }
}