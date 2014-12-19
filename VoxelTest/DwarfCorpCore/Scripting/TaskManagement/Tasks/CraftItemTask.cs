using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemTask : Task
    {
        public CraftLibrary.CraftItemType CraftType { get; set; }
        public Voxel Voxel { get; set; }

        public CraftItemTask()
        {
            Priority = PriorityType.Low;
        }

        public CraftItemTask(Voxel voxel, CraftLibrary.CraftItemType type)
        {
            Name = "Craft item " + voxel.GridPosition + " " + voxel.ChunkID.X + " " + voxel.ChunkID.Y + " " + voxel.ChunkID.Z;
            Voxel = voxel;
            CraftType = type;
            Priority = PriorityType.Low;
        }

        public override Task Clone()
        {
            Voxel v = new Voxel(new Point3(Voxel.GridPosition), Voxel.Chunk);
            return new CraftItemTask(v, CraftType);
        }

        public override float ComputeCost(Creature agent)
        {
            return Voxel == null ? 1000 : (agent.AI.Position - Voxel.Position).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new CraftItemAct(creature.AI, Voxel, CraftType);
        }

        public override bool ShouldRetry(Creature agent)
        {
            if (!agent.Faction.CraftBuilder.IsDesignation(Voxel))
            {
                return false;
            }

            return true;
        }
    }

}