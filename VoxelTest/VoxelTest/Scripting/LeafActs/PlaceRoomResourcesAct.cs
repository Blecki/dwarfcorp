using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature uses the item currently in its hands to build a room.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceRoomResourcesAct : CreatureAct
    {
        public RoomBuildDesignation Room { get; set; }
        public List<ResourceAmount> Resources { get; set; } 

        public PlaceRoomResourcesAct(CreatureAIComponent agent, RoomBuildDesignation room, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Place room resources";
            Room = room;
            Resources = resources;
        }

        public override IEnumerable<Status> Run()
        {
            if (Room == null || Room.IsBuilt || Room.VoxelBuildDesignations.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                Room.AddResources(Resources);
                Creature.Inventory.Remove(Resources);
                yield return Status.Success;
            }
        }
    }

}