using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item with a particular tag, and then puts it into a build zone
    /// for a BuildRoom. (This is used to construct rooms)
    /// </summary>
    public class BuildRoomAct : CompoundCreatureAct
    {
        public BuildRoomOrder BuildRoom { get; set; }
        public RoomBuilder Builder;
        public List<Quantitiy<Resource.ResourceTags>> Resources { get; set; } 

        public IEnumerable<Status> SetTargetVoxelFromRoom(BuildRoomOrder buildRoom, string target)
        {
            if (buildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                var closestVoxel = VoxelHandle.InvalidHandle;
                float closestDist = float.MaxValue;
                foreach (var order in buildRoom.VoxelOrders)
                {
                    float dist = (order.Voxel.WorldPosition - Agent.Position).LengthSquared();

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestVoxel = order.Voxel;
                    }
                }

                Agent.Blackboard.SetData(target, closestVoxel);
                yield return Status.Success;
            }
        }

        public Act SetTargetVoxelFromRoomAct(BuildRoomOrder buildRoom, string target)
        {
            return new Wrap(() => SetTargetVoxelFromRoom(buildRoom, target));
        }

        public bool IsRoomBuildOrder(BuildRoomOrder buildRooom)
        {
            return Builder.BuildDesignations.Contains(buildRooom);
        }

        public BuildRoomAct()
        {

        }

        public IEnumerable<Act.Status> PutResources()
        {
            if (BuildRoom.MeetsBuildRequirements())
            {
                yield return Act.Status.Success;
            }

            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                Agent.Creature.Inventory.Remove(Resources, Inventory.RestockType.None);
                BuildRoom.AddResources(Resources);
                BuildRoom.ResourcesReservedFor = null;
            }
            yield return Act.Status.Success;
        }
        public IEnumerable<Status> WaitForResources()
        {
            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                yield return Act.Status.Success;
                yield break;
            }

            while (!BuildRoom.MeetsBuildRequirements())
            {
                if (BuildRoom.ResourcesReservedFor == null || BuildRoom.ResourcesReservedFor.IsDead)
                {
                    Agent.SetMessage("Failed to wait for resources.");
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> Reserve()
        {
            if (BuildRoom.ResourcesReservedFor == null)
            {
                BuildRoom.ResourcesReservedFor = Agent;
                yield return Act.Status.Success;
                yield break;
            }
            Agent.SetMessage("Failed to reserve resources for room.");
            yield return Act.Status.Fail;
        }

        private bool ValidResourceState()
        {
            return BuildRoom.MeetsBuildRequirements() || (BuildRoom.ResourcesReservedFor != null);
        }



        public IEnumerable<Act.Status> OnFailOrCancel()
        {
            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                BuildRoom.ResourcesReservedFor = null;
            }
            foreach(var status in Creature.RestockAll())
            {

            }
            Agent.Creature.OverrideCharacterMode = false;
            Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
            Agent.Physics.Active = true;
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            foreach(var status in OnFailOrCancel())
            {

            }

            base.OnCanceled(); 
        }


        public BuildRoomAct(CreatureAI agent, BuildRoomOrder buildRoom, RoomBuilder Builder) :
            base(agent)
        {
            this.Builder = Builder;
            Name = "Build BuildRoom " + buildRoom.ToString();
            Resources = buildRoom.ListRequiredResources();
            BuildRoom = buildRoom;
            if (BuildRoom.ResourcesReservedFor != null && BuildRoom.ResourcesReservedFor.IsDead)
            {
                BuildRoom.ResourcesReservedFor = null;
            }

            Tree = new Sequence(new Select(new Domain(buildRoom.MeetsBuildRequirements() || buildRoom.ResourcesReservedFor != null, true), 
                                           new Domain(!buildRoom.MeetsBuildRequirements() && (buildRoom.ResourcesReservedFor == null || buildRoom.ResourcesReservedFor == Agent), new Sequence(new Wrap(Reserve), new GetResourcesAct(Agent, Resources))),
                                           new Domain(buildRoom.MeetsBuildRequirements() || buildRoom.ResourcesReservedFor != null, true)),
                new Domain(() => IsRoomBuildOrder(buildRoom) && !buildRoom.IsBuilt && !buildRoom.IsDestroyed && ValidResourceState(), 
                new Sequence(
                    SetTargetVoxelFromRoomAct(buildRoom, "ActionVoxel"),
                    new GoToNamedVoxelAct("ActionVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new Wrap(PutResources),
                    new Wrap(WaitForResources) {  Name = "Wait for resources..."},
                    new Wrap(() => Creature.HitAndWait(true, () => 1.0f, () => buildRoom.BuildProgress,
                    () => buildRoom.BuildProgress += agent.Stats.BuildSpeed / buildRoom.VoxelOrders.Count * 0.5f,
                    () => MathFunctions.RandVector3Box(buildRoom.GetBoundingBox()), ContentPaths.Audio.Oscar.sfx_ic_dwarf_craft, () => !buildRoom.IsBuilt && !buildRoom.IsDestroyed))
                    { Name = "Build room.." },
                    new CreateRoomAct(Agent, buildRoom)
                    , new Wrap(OnFailOrCancel))) | new Wrap(OnFailOrCancel)
                );
        }

    }



}