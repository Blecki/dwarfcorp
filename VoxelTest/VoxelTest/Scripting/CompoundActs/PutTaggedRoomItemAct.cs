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
    public class PutTaggedRoomItemAct : CompoundCreatureAct
    {
        public RoomBuildDesignation Room { get; set; }

        public IEnumerable<Status> SetTargetVoxelFromRoom(RoomBuildDesignation room, string target)
        {
            if(room.VoxelBuildDesignations.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                VoxelRef closestVoxel = null;
                float closestDist = float.MaxValue;
                foreach(VoxelBuildDesignation voxDes in room.VoxelBuildDesignations)
                {
                    float dist = (voxDes.Voxel.WorldPosition - Agent.Position).LengthSquared();

                    if(dist < closestDist)
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

        public PutTaggedRoomItemAct()
        {

        }

        public PutTaggedRoomItemAct(CreatureAIComponent agent, RoomBuildDesignation room, TagList tags) :
            base(agent)
        {
            Name = "Build room " + room.ToString();
            Tree = new Sequence(new GetItemWithTagsAct(Agent, tags),
                new Sequence(
                    SetTargetVoxelFromRoomAct(room, "TargetVoxel"),
                    new GoToVoxelAct("TargetVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new PlaceRoomItemAct(Agent, room)) | new GatherItemAct(Agent, "HeldObject")
                );
        }

        public override IEnumerable<Act.Status> Run()
        {
            return base.Run();
        }
    }

}