using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToNamedVoxelAct : CompoundCreatureAct
    {
        public string Voxel { get; set; }

        public GoToNamedVoxelAct()
        {

        }

        public GoToNamedVoxelAct(string voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Go to Voxel " + voxel;
        }

        public override void Initialize()
        {
            Tree = new Sequence(
                new ForLoop(new Sequence( 
                                  new PlanAct(Agent, "PathToVoxel", Voxel),
                                  new FollowPathAct(Agent, "PathToVoxel")
                                 )
                                   , 3, true),
                                  new StopAct(Agent));

            base.Initialize();
        }


    }

}