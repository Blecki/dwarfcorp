using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies a list of voxels which are to be turned
    /// into a room.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class RoomBuildDesignation
    {
        public Room ToBuild { get; set; }
        public Dictionary<string, ResourceAmount> PutResources { get; set; }
        public List<VoxelBuildDesignation> VoxelBuildDesignations { get; set; }
        public Faction Faction { get; set; }

        public bool IsBuilt { get; set; }

        public RoomBuildDesignation(Room toBuild, Faction faction)
        {
            ToBuild = toBuild;
            PutResources = new Dictionary<string, ResourceAmount>();
            VoxelBuildDesignations = new List<VoxelBuildDesignation>();
            IsBuilt = false;
            Faction = faction;
        }


        public void Build()
        {
            if(IsBuilt)
            {
                return;
            }

            foreach(VoxelBuildDesignation vox in VoxelBuildDesignations)
            {
                ToBuild.AddVoxel(vox.Voxel);
            }
            IsBuilt = true;
            ToBuild.IsBuilt = true;
            RoomLibrary.GenerateRoomComponentsTemplate(ToBuild, Faction.Components, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics);
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = new List<BoundingBox>();

            foreach(VoxelBuildDesignation vox in VoxelBuildDesignations)
            {
                components.Add(vox.Voxel.GetBoundingBox());
            }

            return MathFunctions.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(string name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if(PutResources.ContainsKey(name))
            {
                current = (int) PutResources[name].NumResources;
            }

            return current >= required;
        }

        public int GetNumRequiredResources(string name)
        {
            if(ToBuild.RoomType.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int) (ToBuild.RoomType.RequiredResources[name].NumResources * VoxelBuildDesignations.Count), 1);
            }
            else
            {
                return 0;
            }
        }

        public string GetTextDisplay()
        {
            string toReturn = ToBuild.RoomType.Name;

            foreach(ResourceAmount amount in ToBuild.RoomType.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if(PutResources.ContainsKey(amount.ResourceType.ResourceName))
                {
                    numResource = (int) (PutResources[amount.ResourceType.ResourceName].NumResources);
                }
                toReturn += amount.ResourceType.ResourceName + " : " + numResource + "/" + Math.Max((int) (amount.NumResources * VoxelBuildDesignations.Count), 1);
            }

            return toReturn;
        }
    }

}