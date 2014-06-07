using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// This act finds the nearest unoccupied and unreserved voxel in a zone,
    /// and fills the blackboard with it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GetNearestFreeVoxelInZone : CreatureAct
    {
        public Zone TargetZone { get; set; }
        public string OutputVoxel { get; set; }
        public bool ReserveVoxel { get; set; }

        public GetNearestFreeVoxelInZone(CreatureAI agent, Zone targetZone, string outputVoxel, bool reserve) :
            base(agent)
        {
            Name = "Get Free Voxel";
            OutputVoxel = outputVoxel;
            TargetZone = targetZone;
            ReserveVoxel = reserve;
        }

        public override IEnumerable<Status> Run()
        {
            if(TargetZone == null)
            {
                yield return Status.Fail;
            }
            else
            {
                VoxelRef v = TargetZone.GetNearestVoxel(Agent.Position);

                if(v != null)
                {
                    Agent.Blackboard.SetData(OutputVoxel, v);
                    yield return Status.Success;
                }
                else
                {
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                }
            }
        }
    }

}