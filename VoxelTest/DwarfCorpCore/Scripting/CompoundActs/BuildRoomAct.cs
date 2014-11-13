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
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class BuildRoomAct : CompoundCreatureAct
    {
        public BuildRoomOrder BuildRoom { get; set; }
        public List<ResourceAmount> Resources { get; set; } 

        public IEnumerable<Status> SetTargetVoxelFromRoom(BuildRoomOrder buildRoom, string target)
        {
            if (buildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel closestVoxel = null;
                float closestDist = float.MaxValue;
                foreach (BuildVoxelOrder voxDes in buildRoom.VoxelOrders)
                {
                    float dist = (voxDes.Voxel.Position - Agent.Position).LengthSquared();

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestVoxel = voxDes.Voxel;
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

        public IEnumerable<Status> IsRoomBuildOrder(BuildRoomOrder buildRooom)
        {
            yield return Creature.Faction.RoomBuilder.BuildDesignations.Contains(buildRooom) ? Status.Success : Status.Fail;
        }


        public BuildRoomAct()
        {

        }

        public BuildRoomAct(CreatureAI agent, BuildRoomOrder buildRoom) :
            base(agent)
        {
            Name = "Build BuildRoom " + buildRoom.ToString();
            Resources = buildRoom.ListRequiredResources();

            Tree = new Sequence(new GetResourcesAct(Agent, Resources),
                new Sequence(
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    SetTargetVoxelFromRoomAct(buildRoom, "TargetVoxel"),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new GoToVoxelAct("TargetVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new Wrap(() => Creature.HitAndWait(buildRoom.VoxelOrders.Count * 0.5f, true)),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new PlaceRoomResourcesAct(Agent, buildRoom, Resources)
                    , new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                );
        }

    }



}