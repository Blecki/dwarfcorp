using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class CraftResourceAct : CompoundCreatureAct
    {
        public ResourceType ItemType;
        public List<ResourceApparentTypeAmount> RawMaterials;
        public string Noise { get; set; }
        public ResourceDes Des;
        public MaybeNull<Resource> ActualCreatedResource = null;
        
        public CraftResourceAct()
        {
            if (Des.ResourcesReservedFor != null && Des.ResourcesReservedFor.IsDead)
                Des.ResourcesReservedFor = null;
        }

        public CraftResourceAct(CreatureAI creature, ResourceType CraftItem, List<ResourceApparentTypeAmount> RawMaterials, ResourceDes Des) :
            base(creature)
        {
            this.ItemType = CraftItem;
            this.RawMaterials = RawMaterials;
            this.Des = Des;
            Name = "Build craft item";
        }

        public IEnumerable<Status> ReserveResources()
        {
            if (Des.ResourcesReservedFor != null && Des.ResourcesReservedFor.IsDead)
                Des.ResourcesReservedFor = null;

            if (!Des.HasResources && Des.ResourcesReservedFor == null)
                Des.ResourcesReservedFor = Agent;

            yield return Status.Success;
        }

        public IEnumerable<Status> UnReserve()
        {
            if (Des.ResourcesReservedFor != null && Des.ResourcesReservedFor.IsDead)
                Des.ResourcesReservedFor = null;

            foreach (var status in Creature.Unreserve("craft-location")) ;

            if (Des.ResourcesReservedFor == Agent)
                Des.ResourcesReservedFor = null;

            Agent.Physics.Active = true;
            Agent.Physics.IsSleeping = false;

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> DestroyResources(Func<Vector3> pos)
        {
            if (!Des.HasResources && Des.ResourcesReservedFor == Agent)
            {
                var stashed = Agent.Blackboard.GetData<List<Resource>>("stashed-materials");
                foreach (var res in stashed)
                    if (!Creature.Inventory.RemoveAndCreateWithToss(res, pos(), Inventory.RestockType.None))
                    {
                        Agent.SetTaskFailureReason("Failed to create resources for item (1).");
                        yield return Act.Status.Fail;
                        yield break;
                    }

                Des.HasResources = true;
            }
            yield return Status.Success;
        }
        
        public IEnumerable<Status> WaitForResources()
        {
            if (Des.ResourcesReservedFor == Agent)
            {
                yield return Act.Status.Success;
                yield break;
            }

            WanderAct wander = new WanderAct(Agent, 60.0f, 5.0f, 1.0f);
            wander.Initialize();
            var enumerator = wander.Enumerator;
            while (!Des.HasResources)
            {
                enumerator.MoveNext();
                if (Des.ResourcesReservedFor == null || Des.ResourcesReservedFor.IsDead)
                {
                    Agent.SetTaskFailureReason("Waiting for resources failed.");
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Status> CreateResource()
        {
            if (RawMaterials == null || RawMaterials.Count == 0)
            {
                Agent.SetTaskFailureReason("Failed to create resources.");
                yield return Act.Status.Fail;
                yield break;
            }

            ActualCreatedResource = Library.CreateMetaResource(ItemType.Craft_MetaResourceFactory, Agent, new Resource(ItemType), Agent.Blackboard.GetData<List<Resource>>("stashed-materials"));
            
            if (ActualCreatedResource.HasValue(out var res))
                yield return Status.Success;
            else
            {
                Agent.SetTaskFailureReason("Failed to create meta resource.");
                yield return Act.Status.Fail;
            }
        }

        public IEnumerable<Status> CreateResources()
        {
            foreach (var status in CreateResource())
            {
                if (status == Status.Fail)
                {
                    yield return Status.Fail;
                    yield break;
                }
            }

            if (ActualCreatedResource.HasValue(out var res))
            {
                for (var i = 0; i < ItemType.Craft_ResultsCount; ++i)
                    Creature.Inventory.AddResource(res);
                Creature.AI.AddXP((int)ItemType.Craft_BaseCraftTime);
                ActHelper.ApplyWearToTool(Creature.AI, GameSettings.Current.Wear_Craft);
                Des.Finished = true;
                yield return Status.Success;
            }
            else
            {
                Agent.SetTaskFailureReason("Invalid meta resource");
                yield return Act.Status.Fail;
                yield break;
            }
        }

        public bool ResourceStateValid()
        {
            bool valid = Des.HasResources || Des.ResourcesReservedFor != null;
            if (!valid)
                Agent.SetTaskFailureReason("Resource state not valid.");

            var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.Craft_Location);
            if (location != null && location.IsDead)
                return false;

            return valid;
        }

        public override void Initialize()
        {
            var unreserveAct = new Wrap(UnReserve);
            var time = 3 * (ItemType.Craft_BaseCraftTime / Creature.AI.Stats.Intelligence);
            var getResources = new Select(
                new Domain(() => Des.HasResources || Des.ResourcesReservedFor != null, 
                    new Always(Status.Success)),
                new Domain(() => !Des.HasResources && (Des.ResourcesReservedFor == Agent || Des.ResourcesReservedFor == null),
                    new Sequence(      
                        new Select(
                            new Sequence(
                                new Wrap(ReserveResources),
                                new GetResourcesOfApparentType(Agent, RawMaterials) { BlackboardEntry = "stashed-materials" }),
                            (new Wrap(UnReserve))),
                        new Always(Status.Fail))),
                new Domain(() => Des.HasResources || Des.ResourcesReservedFor != null, 
                    new Always(Status.Success)));

            if (!String.IsNullOrEmpty(ItemType.Craft_Location))
            {
                Tree = new Select(new Sequence(
                    new Wrap(() => Creature.FindAndReserve(ItemType.Craft_Location, "craft-location")),
                    new ClearBlackboardData(Agent, "ResourcesStashed"),
                    getResources,
                    new Select(new Domain(ResourceStateValid,
                        new Sequence(
                            ActHelper.CreateEquipmentCheckAct(Agent, "Tool", ActHelper.EquipmentFallback.AllowDefault, "Hammer"),
                            new GoToTaggedObjectAct(Agent)
                            {
                                Teleport = true,
                                TeleportOffset = new Vector3(0.5f, 0.0f, 0),
                                ObjectBlackboardName = "craft-location",
                                CheckForOcclusion = true
                            },
                            new Wrap(() => DestroyResources(() => Agent.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            new Wrap(() => Creature.HitAndWait(
                                true,
                                () => 1.0f, // Max Progress
                                () => Des.Progress, // Current Progress
                                () =>
                                { // Increment Progress
                                    var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.Craft_Location);

                                    float workstationBuff = 1.0f;
                                    if (location != null)
                                    {
                                        Creature.Physics.Face(location.Position);
                                        if (location.GetComponent<SteamPipes.BuildBuff>().HasValue(out var buff))
                                            workstationBuff = buff.GetBuffMultiplier();
                                    }

                                    // Todo: Account for environment buff & 'anvil' buff.

                                    Des.Progress += (Creature.Stats.BuildSpeed * workstationBuff) / ItemType.Craft_BaseCraftTime;
                                },
                                () =>
                                { // Get Position
                                    var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.Craft_Location);
                                    if (location != null)
                                        return location.Position;
                                    return Agent.Position;
                                },
                                Noise))
                            { Name = "Construct object." },
                            unreserveAct,
                            new Wrap(() => CreateResources()),
                            new Wrap(Creature.RestockAll)
                            )),
                            new Sequence(
                                unreserveAct,
                                new Wrap(Creature.RestockAll), 
                                new Always(Status.Fail)))
                        ),
                        new Sequence(
                            unreserveAct, 
                            new Wrap(Creature.RestockAll), 
                            new Always(Status.Fail)));
            }
            else
            {
                Tree = new Select(new Sequence(
                    new ClearBlackboardData(Agent, "ResourcesStashed"),
                    getResources,
                    new Domain(ResourceStateValid, new Sequence(
                        ActHelper.CreateEquipmentCheckAct(Agent, "Tool", ActHelper.EquipmentFallback.AllowDefault, "Hammer"),
                        new Wrap(() => DestroyResources(() => Creature.Physics.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                        new Wrap(WaitForResources) { Name = "Wait for resources." },
                        new Wrap(() => Creature.HitAndWait(time, true, () => Creature.Physics.Position)) { Name = "Construct object." },
                        new Wrap(() => CreateResources()))
                    )
                ), new Sequence(
                    unreserveAct, 
                    new Wrap(Creature.RestockAll), 
                    new Always(Status.Fail)));
            }

            base.Initialize();
        }


        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            Creature.Physics.IsSleeping = false;
            foreach (var statuses in Creature.Unreserve(ItemType.Craft_Location))
                continue;

            if (Des.ResourcesReservedFor == Agent)
                Des.ResourcesReservedFor = null;

            base.OnCanceled();
        }       
    }
}