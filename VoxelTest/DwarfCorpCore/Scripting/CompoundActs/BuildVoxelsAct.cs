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
    internal class BuildVoxelsAct : CompoundCreatureAct
    {
        public List<KeyValuePair<Voxel, VoxelType> > Voxels { get; set; }

        public BuildVoxelsAct()
        {

        }

        public BuildVoxelsAct(CreatureAI creature, List<Voxel> voxels, List<VoxelType> types) :
            base(creature)
        {

            Voxels = new List<KeyValuePair<Voxel, VoxelType>>();
            for (int i = 0; i < voxels.Count; i++)
            {
                Voxels.Add(new KeyValuePair<Voxel, VoxelType>(voxels[i], types[i]));
            }
            Name = "Build voxels";
        }

        public override void Initialize()
        {

            List<ResourceAmount> resources = Voxels.Select(pair => new ResourceAmount(ResourceLibrary.Resources[pair.Value.ResourceToRelease], 1)).ToList();

            List<Act> children = new List<Act>()
            {
                new GetResourcesAct(Agent, resources)
            };



            int i = 0;
            foreach (KeyValuePair<Voxel, VoxelType> pair in Voxels)
            {
                children.Add(new GoToVoxelAct(pair.Key, PlanAct.PlanType.Radius, Agent, 3.0f));
                children.Add(new PlaceVoxelAct(pair.Key, Creature.AI, resources[i]));
                i++;
            }

            children.Add(new Wrap(Creature.RestockAll));

            Tree = new Sequence(children);
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Voxels.RemoveAll(pair => !Creature.Faction.WallBuilder.IsDesignation(pair.Key));
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }

}