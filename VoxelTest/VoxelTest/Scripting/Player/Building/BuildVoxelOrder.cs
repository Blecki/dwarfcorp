using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies that a given voxel from a given BuildRoom should be built.
    /// A BuildRoom build designation is actually a colletion of these.
    /// </summary>
    public class BuildVoxelOrder
    {
        public Room ToBuild { get; set; }
        public VoxelRef Voxel { get; set; }
        public BuildRoomOrder Order { get; set; }

        public BuildVoxelOrder(BuildRoomOrder order, Room toBuild, VoxelRef voxel)
        {
            Order = order;
            ToBuild = toBuild;
            Voxel = voxel;
        }



        public void Build()
        {
            Order.Build();
        }

        public Resource GetNextRequiredResource()
        {
            IEnumerable<string> randomKeys = Datastructures.RandomKeys<string, ResourceAmount>(ToBuild.RoomType.RequiredResources);
            foreach(string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if(!Order.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
                else if(Order.PutResources[s].NumResources < Math.Max((int) (ToBuild.RoomType.RequiredResources[s].NumResources * Order.VoxelOrders.Count), 1))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
            }

            return null;
        }


    }

}