using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

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
            Body item = EntityFactory.CreateEntity<Body>(CraftLibrary.CraftItems[ItemType].Name, Voxel.Position + Vector3.One * 0.5f);
            PlayState.ParticleManager.Trigger("puff", Voxel.Position + Vector3.One * 0.5f, Color.White, 10);
            if (item == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Creature.Faction.CraftBuilder.RemoveDesignation(Voxel);
                Creature.AI.AddXP(10);
                yield return Status.Success;
            }
        }
    }

}