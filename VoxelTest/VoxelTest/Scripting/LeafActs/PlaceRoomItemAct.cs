using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceRoomItemAct : CreatureAct
    {
        public RoomBuildDesignation Room { get; set; }

        public PlaceRoomItemAct(CreatureAIComponent agent, RoomBuildDesignation room) :
            base(agent)
        {
            Name = "Place room item";
            Room = room;
        }

        public override IEnumerable<Status> Run()
        {
            if(Room == null || Room.IsBuilt)
            {
                yield return Status.Fail;
            }
            else
            {
                LocatableComponent grabbedComponent = Creature.Hands.GetFirstGrab();

                if(grabbedComponent == null || Room.VoxelBuildDesignations.Count == 0)
                {
                    yield return Status.Fail;
                }
                else
                {
                    Room.VoxelBuildDesignations[0].AddResource(grabbedComponent.Tags[0]);
                    Creature.Hands.UnGrab(grabbedComponent);
                    grabbedComponent.Die();
                    Agent.Blackboard.SetData<object>("HeldObject", null);

                    if(Room.VoxelBuildDesignations[0].MeetsBuildRequirements())
                    {
                        Room.VoxelBuildDesignations[0].Build();
                    }
                    yield return Status.Success;
                }
            }
        }
    }

}