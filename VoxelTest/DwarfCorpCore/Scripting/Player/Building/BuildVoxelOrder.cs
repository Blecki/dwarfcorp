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
        public Voxel Voxel { get; set; }
        public BuildRoomOrder Order { get; set; }

        public BuildVoxelOrder(BuildRoomOrder order, Room toBuild, Voxel voxel)
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
 
            foreach (ResourceLibrary.ResourceType s in ToBuild.RoomData.RequiredResources.Keys)
            {
                if(!Order.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomData.RequiredResources[s].ResourceType;
                }
                else if(Order.PutResources[s].NumResources < Math.Max((int) (ToBuild.RoomData.RequiredResources[s].NumResources * Order.VoxelOrders.Count * 0.25f), 1))
                {
                    return ToBuild.RoomData.RequiredResources[s].ResourceType;
                }
            }

            return null;
        }


    }

}