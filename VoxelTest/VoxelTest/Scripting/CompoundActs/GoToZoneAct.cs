using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature takes an item to an open stockpile and leaves it there.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToZoneAct : CompoundCreatureAct
    {
        public Zone Destination { get; set; }

        public GoToZoneAct()
        {

        }

        public GoToZoneAct(CreatureAI agent, Zone zone) :
            base(agent)
        {
            Tree = null;
            Name = "Goto zone : " + zone.ID;
            Destination = zone;
        }

        public override void Initialize()
        {
            base.Initialize();
        }




        public override IEnumerable<Status> Run()
        {
            if (Tree == null)
            {
                VoxelRef voxel = Destination.GetNearestVoxel(Agent.Position);

                Tree = new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, Agent);

                Tree.Initialize();
            }

            if (Tree == null)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach (Status s in base.Run())
                {
                    yield return s;
                }
            }
        }
    }

}