using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class PutTaggedRoomItemAct : CompoundCreatureAct
    {
        public RoomBuildDesignation Room { get; set; }

        public IEnumerable<Status> SetTargetVoxelFromRoom(RoomBuildDesignation room)
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

                Agent.TargetVoxel = closestVoxel;
                yield return Status.Success;
            }
        }

        public Act SetTargetVoxelFromRoomAct(RoomBuildDesignation room)
        {
            return new Wrap(() => { return SetTargetVoxelFromRoom(room);  });
        }

        public PutTaggedRoomItemAct(CreatureAIComponent agent, RoomBuildDesignation room, TagList tags) :
            base(agent)
        {
            Name = "Build room " + room.ToString();
            Tree = new Sequence(new GetItemWithTagsAct(Agent, tags),
                                    new Sequence(
                                        SetTargetVoxelFromRoomAct(room),
                                    new GoToVoxelAct(null, Agent),
                                    new PlaceRoomItemAct(Agent, room)) | new GatherItemAct(Agent, "HeldObject")
                                   );

                                              
        }

        public override IEnumerable<Act.Status> Run()
        {
            return base.Run();
        }
    }
}
