using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature uses the item currently in its hands to build a BuildRoom.
    /// </summary>
    public class CreateRoomAct : CreatureAct
    {
        public BuildZoneOrder BuildRoom { get; set; }

        public CreateRoomAct(CreatureAI agent, BuildZoneOrder buildRoom) :
            base(agent)
        {
            Name = "Build room.";
            BuildRoom = buildRoom;
        }

        public override IEnumerable<Status> Run()
        {
            if (BuildRoom == null || BuildRoom.IsBuilt || BuildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                BuildRoom.Build();
                Creature.Stats.NumRoomsBuilt++;
                Creature.AI.AddXP(GameSettings.Current.XP_craft);
                yield return Status.Success;
            }
        }
    }

}