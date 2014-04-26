using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item with a particular tag, and then puts it into a build zone
    /// for a room. (This is used to construct rooms)
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class BuildRoomAct : CompoundCreatureAct
    {
        public RoomBuildDesignation Room { get; set; }
        public List<ResourceAmount> Resources { get; set; } 

        public IEnumerable<Status> SetTargetVoxelFromRoom(RoomBuildDesignation room, string target)
        {
            if (room.VoxelBuildDesignations.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                VoxelRef closestVoxel = null;
                float closestDist = float.MaxValue;
                foreach (VoxelBuildDesignation voxDes in room.VoxelBuildDesignations)
                {
                    float dist = (voxDes.Voxel.WorldPosition - Agent.Position).LengthSquared();

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

        public Act SetTargetVoxelFromRoomAct(RoomBuildDesignation room, string target)
        {
            return new Wrap(() => SetTargetVoxelFromRoom(room, target));
        }


        public BuildRoomAct()
        {

        }

        public BuildRoomAct(CreatureAIComponent agent, RoomBuildDesignation room) :
            base(agent)
        {
            Name = "Build room " + room.ToString();
            Resources = room.ListRequiredResources();

            Tree = new Sequence(new GetResourcesAct(Agent, Resources),
                new Sequence(
                    SetTargetVoxelFromRoomAct(room, "TargetVoxel"),
                    new GoToVoxelAct("TargetVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new PlaceRoomResourcesAct(Agent, room, Resources), new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                );
        }

    }



}