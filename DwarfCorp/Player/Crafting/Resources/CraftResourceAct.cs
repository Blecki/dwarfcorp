using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ResourceDes
    {
        public bool Finished = false;
        public float Progress = 0.0f;
        public bool HasResources = false;
        public CreatureAI ResourcesReservedFor = null;
    }


    internal class CraftResourceAct : CompoundCreatureAct
    {
        public CraftItem ItemType;
        public List<ResourceTypeAmount> RawMaterials;
        public string Noise { get; set; }
        public ResourceDes Des;
        public MaybeNull<Resource> ActualCreatedResource = null;

        

        public CraftResourceAct()
        {
            if (Des.ResourcesReservedFor != null && Des.ResourcesReservedFor.IsDead)
                Des.ResourcesReservedFor = null;
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
                        Agent.SetMessage("Failed to create resources for item (1).");
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
                    Agent.SetMessage("Waiting for resources failed.");
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
                Agent.SetMessage("Failed to create resources.");
                yield return Act.Status.Fail;
                yield break;
            }

            ActualCreatedResource = new Resource(ItemType.ResourceCreated);

            switch (ItemType.CraftActBehavior)
            {
                
                // Todo: This switch sucks.
                // Transform into mod hook functions
                case CraftItem.CraftActBehaviors.Trinket:
                    ActualCreatedResource = Library.CreateTrinketResource(RawMaterials[0].Type, (Agent.Stats.Dexterity + Agent.Stats.Intelligence) / 15.0f * MathFunctions.Rand(0.5f, 1.75f));
                    break;
                case CraftItem.CraftActBehaviors.Meal:
                    {
                        if (RawMaterials.Count < 2)
                        {
                            Agent.SetMessage("Failed to get resources for meal.");
                            yield return Act.Status.Fail;
                            yield break;
                        }

                        ActualCreatedResource = Library.CreateMealResource(RawMaterials.ElementAt(0).Type, RawMaterials.ElementAt(1).Type);
                    }
                    break;
                case CraftItem.CraftActBehaviors.Ale:
                    {
                        Resource _base = null;
                        foreach (var stashedResource in Agent.Blackboard.GetData<List<Resource>>("stashed-materials"))
                            if (stashedResource.TypeName == RawMaterials.ElementAt(0).Type)
                                _base = stashedResource;
                        ActualCreatedResource = Library.CreateAleResource(_base);
                    }
                    break;
                case CraftItem.CraftActBehaviors.Bread:
                    {
                        Resource _base = null;
                        foreach (var stashedResource in Agent.Blackboard.GetData<List<Resource>>("stashed-materials"))
                            if (stashedResource.TypeName == RawMaterials.ElementAt(0).Type)
                                _base = stashedResource;
                        ActualCreatedResource = Library.CreateBreadResource(_base);
                    }
                    break;
                case CraftItem.CraftActBehaviors.GemTrinket:
                    {
                        Resource gem = null;
                        Resource trinket = null;
                        foreach (var stashedResource in Agent.Blackboard.GetData<List<Resource>>("stashed-materials"))
                        {
                            if (stashedResource.ResourceType.HasValue(out var res) && res.Tags.Contains("Craft"))
                                trinket = stashedResource;

                            if (stashedResource.ResourceType.HasValue(out var _res) && _res.Tags.Contains("Gem"))
                                gem = stashedResource;
                        }

                        if (gem == null || trinket == null)
                        {
                            Agent.SetMessage("Failed to get resources for trinket.");
                            yield return Status.Fail;
                            yield break;
                        }

                        ActualCreatedResource = Library.CreateEncrustedTrinketResourceType(trinket, gem);
                    }
                    break;
                case CraftItem.CraftActBehaviors.Normal:
                default:
                    break;
            }

            yield return Status.Success;
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
                for (var i = 0; i < ItemType.CraftedResultsCount; ++i)
                    Creature.Inventory.AddResource(res);
            Creature.AI.AddXP((int)ItemType.BaseCraftTime);
            Des.Finished = true;
            yield return Status.Success;
        }


        public CraftResourceAct(CreatureAI creature, CraftItem CraftItem, List<ResourceTypeAmount> RawMaterials, ResourceDes Des) :
            base(creature)
        {
            this.ItemType = CraftItem;
            this.RawMaterials = RawMaterials;
            this.Des = Des;
            Name = "Build craft item";
        }

        public bool ResourceStateValid()
        {
            bool valid = Des.HasResources || Des.ResourcesReservedFor != null;
            if (!valid)
                Agent.SetMessage("Resource state not valid.");

            var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.CraftLocation);
            if (location != null && location.IsDead)
                return false;

            return valid;
        }

        public override void Initialize()
        {
            var unreserveAct = new Wrap(UnReserve);
            var time = 3 * (ItemType.BaseCraftTime / Creature.AI.Stats.Intelligence);
            var getResources = new Select(new Domain(() => Des.HasResources || Des.ResourcesReservedFor != null, true),
                                          new Domain(() => !Des.HasResources && (Des.ResourcesReservedFor == Agent || Des.ResourcesReservedFor == null),
                                                new Sequence(
                                                    new Wrap(ReserveResources),
                                                    new GetResourcesOfType(Agent, RawMaterials) { BlackboardEntry = "stashed-materials" })
                                                | (new Wrap(UnReserve)) 
                                                & false),
                                            new Domain(() => Des.HasResources || Des.ResourcesReservedFor != null, true));

            if (!String.IsNullOrEmpty(ItemType.CraftLocation))
            {
                Tree = new Sequence(
                    new Wrap(() => Creature.FindAndReserve(ItemType.CraftLocation, "craft-location")),
                    new ClearBlackboardData(Agent, "ResourcesStashed"),
                    getResources,
                    new Domain(ResourceStateValid,
                        new Sequence(
                            ActHelper.CreateToolCheckAct(Agent, "Hammer"),
                            new GoToTaggedObjectAct(Agent)
                            {
                                Teleport = true,
                                TeleportOffset = new Vector3(0.5f, 0.0f, 0),
                                ObjectBlackboardName = "craft-location",
                                CheckForOcclusion = true
                            },
                            new Wrap(() => DestroyResources(() => Agent.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                            new Wrap(WaitForResources) { Name = "Wait for resources." },
                            new Wrap(() => Creature.HitAndWait(true,
                                () => 1.0f, // Max Progress
                                () => Des.Progress, // Current Progress
                                () =>
                                { // Increment Progress
                                        var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.CraftLocation);

                                    float workstationBuff = 1.0f;
                                    if (location != null)
                                    {
                                        Creature.Physics.Face(location.Position);
                                        if (location.GetComponent<SteamPipes.BuildBuff>().HasValue(out var buff))
                                            workstationBuff = buff.GetBuffMultiplier();
                                    }

                                    // Todo: Account for environment buff & 'anvil' buff.

                                    Des.Progress += (Creature.Stats.BuildSpeed * workstationBuff) / ItemType.BaseCraftTime;
                                },
                                () =>
                                { // Get Position
                                        var location = Creature.AI.Blackboard.GetData<GameComponent>(ItemType.CraftLocation);
                                    if (location != null)
                                        return location.Position;
                                    return Agent.Position;
                                },
                                Noise))
                            { Name = "Construct object." },
                            unreserveAct,
                            new Wrap(() => CreateResources()),
                            new Wrap(Creature.RestockAll)
                            ))
                            | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                        )
                        | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
            }
            else
            {
                Tree = new Sequence(
                    new ClearBlackboardData(Agent, "ResourcesStashed"),
                    getResources,
                    new Domain(ResourceStateValid, new Sequence(
                        ActHelper.CreateToolCheckAct(Agent, "Hammer"),
                        new Wrap(() => DestroyResources(() => Creature.Physics.Position + MathFunctions.RandVector3Cube() * 0.5f)),
                        new Wrap(WaitForResources) { Name = "Wait for resources." },
                        new Wrap(() => Creature.HitAndWait(time, true, () => Creature.Physics.Position)) { Name = "Construct object." },
                        new Wrap(() => CreateResources()))
                    )
                ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false);
            }
            base.Initialize();
        }


        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            Creature.Physics.IsSleeping = false;
            foreach (var statuses in Creature.Unreserve(ItemType.CraftLocation))
                continue;

            if (Des.ResourcesReservedFor == Agent)
                Des.ResourcesReservedFor = null;

            base.OnCanceled();
        }       
    }
}