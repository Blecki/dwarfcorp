// BuildRoomOrder.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies a list of voxels which are to be turned
    /// into a BuildRoom.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuildRoomOrder
    {
        public Room ToBuild { get; set; }
        public Dictionary<ResourceLibrary.ResourceType, ResourceAmount> PutResources { get; set; }
        public List<BuildVoxelOrder> VoxelOrders { get; set; }
        public Faction Faction { get; set; }

        public bool IsBuilt { get; set; }

        public BuildRoomOrder(Room toBuild, Faction faction)
        {
            ToBuild = toBuild;
            PutResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            VoxelOrders = new List<BuildVoxelOrder>();
            IsBuilt = false;
            Faction = faction;
        }


        public void AddResources(List<ResourceAmount> resources)
        {
            foreach(ResourceAmount resource in resources)
            {
                ResourceLibrary.ResourceType resourceName = resource.ResourceType.Type;
                if(PutResources.ContainsKey(resourceName))
                {
                    ResourceAmount amount = PutResources[resourceName];
                    amount.NumResources += resource.NumResources;
                }
                else
                {
                    ResourceAmount amount = new ResourceAmount();
                    amount.NumResources += resource.NumResources;
                    amount.ResourceType = resource.ResourceType;

                    PutResources[resourceName] = amount;
                }
            }

            if(MeetsBuildRequirements())
            {
                Build();
            }
        }


        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach (ResourceLibrary.ResourceType s in ToBuild.RoomData.RequiredResources.Keys)
            {
                if (!PutResources.ContainsKey(s))
                {
                    return false;
                }
                else
                {
                    toReturn = toReturn && (PutResources[s].NumResources >= Math.Max((int)(ToBuild.RoomData.RequiredResources[s].NumResources * VoxelOrders.Count * 0.25f), 1));
                }
            }

            return toReturn;
        }

        public virtual void Build()
        {
            if(IsBuilt)
            {
                return;
            }

            foreach(BuildVoxelOrder vox in VoxelOrders)
            {
                ToBuild.AddVoxel(vox.Voxel);
            }
            IsBuilt = true;
            ToBuild.IsBuilt = true;
            RoomLibrary.GenerateRoomComponentsTemplate(ToBuild, Faction.Components, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics);
            ToBuild.OnBuilt();

            PlayState.AnnouncementManager.Announce("Built room!", ToBuild.ID + " was built");
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = VoxelOrders.Select(vox => vox.Voxel.GetBoundingBox()).ToList();

            return MathFunctions.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(ResourceLibrary.ResourceType name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if(PutResources.ContainsKey(name))
            {
                current = (int) PutResources[name].NumResources;
            }

            return current >= required;
        }

        public int GetNumRequiredResources(ResourceLibrary.ResourceType name)
        {
            if(ToBuild.RoomData.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int) (ToBuild.RoomData.RequiredResources[name].NumResources * VoxelOrders.Count * 0.25f), 1);
            }
            else
            {
                return 0;
            }
        }

        public string GetTextDisplay()
        {
            string toReturn = ToBuild.RoomData.Name;

            foreach(ResourceAmount amount in ToBuild.RoomData.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if(PutResources.ContainsKey(amount.ResourceType.Type))
                {
                    numResource = (int) (PutResources[amount.ResourceType.Type].NumResources);
                }
                toReturn += amount.ResourceType.ResourceName + " : " + numResource + "/" + Math.Max((int) (amount.NumResources * VoxelOrders.Count * 0.25f), 1);
            }

            return toReturn;
        }

        public List<ResourceAmount> ListRequiredResources()
        {
            List<ResourceAmount> toReturn = new List<ResourceAmount>();
            foreach (ResourceLibrary.ResourceType s in ToBuild.RoomData.RequiredResources.Keys)
            {
                int needed = Math.Max((int) (ToBuild.RoomData.RequiredResources[s].NumResources * VoxelOrders.Count * 0.25f), 1);
                int currentResources = 0;

                if(PutResources.ContainsKey(s))
                {
                    currentResources = PutResources[s].NumResources;
                }

                if(currentResources >= needed)
                {
                    continue;
                }

                toReturn.Add(new ResourceAmount(ResourceLibrary.Resources[s], needed - currentResources));
            }

            return toReturn;
        }
    }

}