using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class GatherManager
    {
        public struct StockOrder
        {
            public ResourceAmount Resource;
            public Zone Destination;
        }

        public struct BuildVoxelOrder 
        {
            public Voxel Voxel { get; set; }
            public VoxelType Type { get; set; }
        }


        public CreatureAI Creature
        {
            get; set;
        }


        public List<Body> ItemsToGather { get; set; }
        public List<StockOrder> StockOrders { get; set; }
        public List<BuildVoxelOrder> VoxelOrders { get; set; } 

        public GatherManager(CreatureAI creature)
        {
            Creature = creature;
            ItemsToGather = new List<Body>();
            StockOrders = new List<StockOrder>();
            VoxelOrders = new List<BuildVoxelOrder>();
        }


        public void AddVoxelOrder(BuildVoxelOrder buildVoxelOrder)
        {

            foreach (BuildVoxelOrder order in VoxelOrders)
            {
                if (order.Voxel.Equals(buildVoxelOrder.Voxel))
                {
                    return;
                }
            }

            VoxelOrders.Add(buildVoxelOrder);
        }
    }
}
