using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies that a given voxel from a given room should be built.
    /// A room build designation is actually a colletion of these.
    /// </summary>
    public class VoxelBuildDesignation
    {
        public Room ToBuild { get; set; }
        public VoxelRef Voxel { get; set; }
        public RoomBuildDesignation BuildDesignation { get; set; }

        public VoxelBuildDesignation(RoomBuildDesignation buildDesignation, Room toBuild, VoxelRef voxel)
        {
            BuildDesignation = buildDesignation;
            ToBuild = toBuild;
            Voxel = voxel;
        }

        public void AddResource(string resource)
        {
            if(BuildDesignation.PutResources.ContainsKey(resource))
            {
                ResourceAmount amount = BuildDesignation.PutResources[resource];
                amount.NumResources++;
            }
            else
            {
                ResourceAmount amount = new ResourceAmount();
                amount.NumResources++;
                amount.ResourceType = ResourceLibrary.Resources[resource];

                BuildDesignation.PutResources[resource] = amount;
            }
        }

        public void Build()
        {
            BuildDesignation.Build();
        }

        public Resource GetNextRequiredResource()
        {
            IEnumerable<string> randomKeys = Datastructures.RandomKeys<string, ResourceAmount>(ToBuild.RoomType.RequiredResources);
            foreach(string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if(!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
                else if(BuildDesignation.PutResources[s].NumResources < Math.Max((int) (ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
            }

            return null;
        }

        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach(string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if(!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return false;
                }
                else
                {
                    toReturn = toReturn && (BuildDesignation.PutResources[s].NumResources >= Math.Max((int) (ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1));
                }
            }

            return toReturn;
        }
    }

}