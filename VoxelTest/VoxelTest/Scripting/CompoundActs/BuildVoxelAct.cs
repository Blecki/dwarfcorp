using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }
        public TagList Tags { get; set; }

        public BuildVoxelAct()
        {
            
        }

        public BuildVoxelAct(CreatureAIComponent creature, VoxelRef voxel, TagList tags) :
            base(creature)
        {
            Voxel = voxel;
            Tags = tags;
            Name = "Build voxel";

            if(Agent.Faction.PutDesignator.IsDesignation(voxel))
            {
                Tree = new Sequence(new GetItemWithTagsAct(creature, tags),
                    (new GoToVoxelAct(voxel, creature) | new GatherItemAct(creature, "HeldObject")),
                    (new PlaceVoxelAct(voxel, creature) | new GatherItemAct(creature, "HeldObject")));
            }
            else
            {
                Tree = null;
            }
        }
    }

}