using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature attacks a voxel until it is destroyed.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class DigAct : CreatureAct
    {
        public string TargetVoxelName { get; set; }
        public float EnergyLoss { get; set; }

        public DigAct(CreatureAIComponent creature, string targetVoxel) :
            base(creature)
        {
            TargetVoxelName = targetVoxel;
            Name = "Dig!";
            EnergyLoss = 10.0f;
        }

        public VoxelRef GetTargetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelRef>(TargetVoxelName);
        }

        public override IEnumerable<Status> Run()
        {
            return Creature.Dig(TargetVoxelName, EnergyLoss);
        }
    }

}