using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature uses the item currently in its hands to build a BuildRoom.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceRoomResourcesAct : CreatureAct
    {
        public BuildRoomOrder BuildRoom { get; set; }
        public List<ResourceAmount> Resources { get; set; } 

        public PlaceRoomResourcesAct(CreatureAI agent, BuildRoomOrder buildRoom, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Place BuildRoom resources";
            BuildRoom = buildRoom;
            Resources = resources;
        }

        public override IEnumerable<Status> Run()
        {
            if (BuildRoom == null || BuildRoom.IsBuilt || BuildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                BuildRoom.AddResources(Resources);
                Creature.Inventory.Remove(Resources);
                Creature.Stats.NumRoomsBuilt++;
                Creature.AI.AddXP(10);
                yield return Status.Success;
            }
        }
    }

}