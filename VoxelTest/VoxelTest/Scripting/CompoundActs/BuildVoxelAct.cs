using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildVoxelAct : CompoundCreatureAct
    {
        public Voxel Voxel { get; set; }
   
        public BuildVoxelAct()
        {
            
        }

        public BuildVoxelAct(CreatureAI creature, Voxel voxel, VoxelType type) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Build voxel";

            List<ResourceAmount> resources = new List<ResourceAmount>()
            {
                new ResourceAmount(ResourceLibrary.Resources[type.ResourceToRelease], 1)
            };

            if(Agent.Faction.WallBuilder.IsDesignation(voxel))
            {

                Tree = new Sequence(new GetResourcesAct(Agent, resources),
                    new Sequence(
                        new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, Agent),
                        new PlaceVoxelAct(voxel, creature, resources.First()), new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                    );
            }
            else
            {

                Tree = null;
            }
        }
    }

}