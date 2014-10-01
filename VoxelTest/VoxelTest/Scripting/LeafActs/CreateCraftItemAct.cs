using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class CreateCraftItemAct : CreatureAct
    {
        public Voxel Voxel { get; set; }
        public CraftLibrary.CraftItemType ItemType { get; set; }
        public CreateCraftItemAct(Voxel voxel, CreatureAI agent, CraftLibrary.CraftItemType itemType) :
            base(agent)
        {
            Agent = agent;
            Voxel = voxel;
            Name = "Create craft item";
            ItemType = itemType;
        }

        public override IEnumerable<Status> Run()
        {
            Body item = EntityFactory.GenerateCraftItem(ItemType, Voxel.Position);

            if (item == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Creature.Faction.CraftBuilder.RemoveDesignation(Voxel);

                yield return Status.Success;
            }
        }
    }

}