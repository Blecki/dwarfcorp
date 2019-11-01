using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class PlaceObjectAct : CompoundCreatureAct
    {
        public PlacementDesignation Item { get; set; }
        public Stockpile ItemSource;
        public VoxelHandle Voxel { get; set; }
        public string Noise { get; set; }

        public PlaceObjectAct()
        {
        }

        public IEnumerable<Status> LocateResources()
        {
            var resources = Agent.World.FindUnreservedResource(Item.ItemType.TypeName);
            if (resources.HasValue(out var res))
            {
                res.Item2.ReservedFor = Agent;
                Item.SelectedResource = res.Item2;
                ItemSource = res.Item1;
                yield return Status.Success;
            }
            else
                yield return Status.Fail;
        }

        public IEnumerable<Status> AcquireResources()
        {
            var stash = new StashResourcesAct(Agent, ItemSource, Item.SelectedResource);
            foreach (var status in stash.Run())
                yield return status;
        }

        public IEnumerable<Status> UnReserve()
        {

            if (Item.SelectedResource.ReservedFor == Agent)
                Item.SelectedResource.ReservedFor = null;

            Agent.Physics.Active = true;
            Agent.Physics.IsSleeping = false;

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> DestroyResources(Func<Vector3> pos)
        {
            if (Item.SelectedResource != null)
                if (!Creature.Inventory.RemoveAndCreateWithToss(Item.SelectedResource, pos(), Inventory.RestockType.None))
                {
                    Agent.SetMessage("Failed to create resources for item (1).");
                    yield return Act.Status.Fail;
                    yield break;
                }

            Item.HasResources = true;
            yield return Status.Success;
        }
        
        public IEnumerable<Status> WaitForResources()
        {
            if (Item.HasResources)
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
                if (Item.SelectedResource.ReservedFor == null || Item.SelectedResource.ReservedFor.IsDead)
                {
                    Agent.SetMessage("Waiting for resources failed.");
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public PlaceObjectAct(CreatureAI creature, PlacementDesignation type) :
            base(creature)
        {
            Item = type;
            Voxel = type.Location;
            Name = "Build craft item";
        }

        public bool IsNotCancelled()
        {
            return Creature.World.PersistentData.Designations.IsDesignation(Item.Entity, DesignationType.PlaceObject);
        }

        public bool ResourceStateValid()
        {
            bool valid =  Item.HasResources || Item.SelectedResource != null;
            if (!valid)
                Agent.SetMessage("Resource state not valid.");

            return valid;
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(UnReserve);
            float time = 3 * (Item.ItemType.Placement_PlaceTime / Creature.AI.Stats.Intelligence);
            Act getResources = null;

            getResources = new Select(
                new Domain(() => Item.HasResources || Item.SelectedResource != null, true),
                new Domain(() => !Item.HasResources && (Item.SelectedResource == null || Item.SelectedResource.ReservedFor == Agent || Item.SelectedResource.ReservedFor == null),
                    new Sequence(
                        new Wrap(LocateResources),
                        new Wrap(AcquireResources)
                    ) | (new Wrap(UnReserve))
                                            & false),
                    new Domain(() => Item.HasResources || Item.SelectedResource.ReservedFor != null, true));
            Act buildAct = null;

            buildAct = new Wrap(() => Creature.HitAndWait(true, () => 1.0f,
                                () => Item.Progress, () => Item.Progress += (Creature.Stats.BuildSpeed * 8) / Item.ItemType.Placement_PlaceTime, // Todo: Account for creature debuffs, environment buffs
                                () => Item.Location.WorldPosition + Vector3.One * 0.5f, "Craft"))
            { Name = "Construct object." };

            Tree = new Domain(IsNotCancelled, new Sequence(
                new ClearBlackboardData(Agent, "ResourcesStashed"),
                getResources,
                new Sequence(new Domain(ResourceStateValid,
                    new Sequence(
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                        new Wrap(() => DestroyResources(() => Item.Location.WorldPosition)),
                        new Wrap(WaitForResources) { Name = "Wait for resources." },
                        buildAct,
                        new Wrap(FinallyPlaceObject) { Name = "Place the object." }
                    )
                ))
                )) |
                new Sequence(new Wrap(Creature.RestockAll), unreserveAct, false);

            base.Initialize();
        }

        private IEnumerable<Status> FinallyPlaceObject()
        {
            Item.Finished = true;

            if (Item.WorkPile != null)
                Item.WorkPile.Die();

            var blackboard = new Blackboard();
            blackboard.SetData("Resource", Item.SelectedResource);

            var previewBody = EntityFactory.CreateEntity<GameComponent>(
                Item.ItemType.Placement_EntityToCreate,
                Item.Location.Center, blackboard).GetRoot();

            previewBody.SetFlagRecursive(GameComponent.Flag.Active, true);
            previewBody.SetVertexColorRecursive(Color.White);
            previewBody.SetFlagRecursive(GameComponent.Flag.Visible, true);

            foreach (var tinter in previewBody.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = false;

            if (Item.ItemType.Placement_MarkDestructable)
                previewBody.Tags.Add("Deconstructable");

            if (Item.ItemType.Placement_AddToOwnedPool)
                Creature.Faction.OwnedObjects.Add(previewBody);

            Creature.Manager.World.ParticleManager.Trigger("puff", Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, 10);
            Creature.AI.AddXP((int)(5 * (Item.ItemType.Placement_PlaceTime / Creature.AI.Stats.Intelligence)));


            Item.Entity.Delete();

            yield return Status.Success;
        }


        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            Creature.Physics.IsSleeping = false;

            if (Item.SelectedResource != null && Item.SelectedResource.ReservedFor == Agent)
                Item.SelectedResource.ReservedFor = null;

            base.OnCanceled();
        }       
    }
}