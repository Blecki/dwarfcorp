﻿// BuildRoomOrder.cs
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
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> PutResources { get; set; }
        public List<BuildVoxelOrder> VoxelOrders { get; set; }
        public Faction Faction { get; set; }
        public List<GameComponent> WorkObjects = new List<GameComponent>(); 
        public bool IsBuilt { get; set; }

        public BuildRoomOrder(Room toBuild, Faction faction)
        {
            ToBuild = toBuild;
            PutResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            VoxelOrders = new List<BuildVoxelOrder>();
            IsBuilt = false;
            Faction = faction;
        }


        public void CreateFences()
        {
            Voxel neighbor = new Voxel();

            Vector3 half = Vector3.One*0.5f;
            Vector3 off = half + Vector3.Up;
            foreach (BuildVoxelOrder order in VoxelOrders)
            {
                Voxel voxel = order.Voxel;
                if (voxel.GetNeighbor(new Vector3(0, 0, 1), ref neighbor) && !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0, 0, 0.45f), (float)Math.Atan2(0, 1)));
                }

                if (voxel.GetNeighbor(new Vector3(0, 0, -1), ref neighbor) && !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0, 0, -0.45f), (float)Math.Atan2(0, -1)));
                }


                if (voxel.GetNeighbor(new Vector3(1, 0, 0), ref neighbor) && !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0.45f, 0, 0.0f), (float)Math.Atan2(1, 0)));
                }


                if (voxel.GetNeighbor(new Vector3(-1, 0, 0), ref neighbor) && !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(-0.45f, 0, 0.0f), (float)Math.Atan2(-1, 0)));
                }

                if (MathFunctions.RandEvent(0.1f))
                {
                    WorkObjects.Add(new WorkPile(voxel.Position + off));
                }
            }
        }

        public void AddResources(List<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                if(PutResources.ContainsKey(resource.ResourceType))
                {
                    Quantitiy<Resource.ResourceTags> amount = PutResources[resource.ResourceType];
                    amount.NumResources += resource.NumResources;
                }
                else
                {
                    Quantitiy<Resource.ResourceTags> amount = new Quantitiy<Resource.ResourceTags>();
                    amount.NumResources += resource.NumResources;
                    amount.ResourceType = resource.ResourceType;

                    PutResources[resource.ResourceType] = amount;
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
            foreach (Resource.ResourceTags s in ToBuild.RoomData.RequiredResources.Keys)
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

        public virtual void Build(bool silent=false)
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
            RoomLibrary.GenerateRoomComponentsTemplate(ToBuild, Faction.Components, WorldManager.ChunkManager.Content, WorldManager.ChunkManager.Graphics);
            ToBuild.OnBuilt();

            if (!silent)
            {
                WorldManager.AnnouncementManager.Announce("Built room!", ToBuild.ID + " was built");
            }

            foreach (GameComponent fence in WorkObjects)
            {
                fence.Die();
            }
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = VoxelOrders.Select(vox => vox.Voxel.GetBoundingBox()).ToList();

            return MathFunctions.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(Resource.ResourceTags name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if(PutResources.ContainsKey(name))
            {
                current = (int) PutResources[name].NumResources;
            }

            return current >= required;
        }

        public int GetNumRequiredResources(Resource.ResourceTags name)
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

            foreach (Quantitiy<Resource.ResourceTags> amount in ToBuild.RoomData.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if(PutResources.ContainsKey(amount.ResourceType))
                {
                    numResource = (int) (PutResources[amount.ResourceType].NumResources);
                }
                toReturn += amount.ResourceType.ToString() + " : " + numResource + "/" + Math.Max((int) (amount.NumResources * VoxelOrders.Count * 0.25f), 1);
            }

            return toReturn;
        }

        public List<Quantitiy<Resource.ResourceTags>> ListRequiredResources()
        {
            List<Quantitiy<Resource.ResourceTags>> toReturn = new List<Quantitiy<Resource.ResourceTags>>();
            foreach (Resource.ResourceTags s in ToBuild.RoomData.RequiredResources.Keys)
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

                toReturn.Add(new Quantitiy<Resource.ResourceTags>(s, needed - currentResources));
            }

            return toReturn;
        }
    }

}