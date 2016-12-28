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
    ///     This designation specifies a list of voxels which are to be turned
    ///     into a built room and the resources to put into the room's construction.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuildRoomOrder
    {
        /// <summary>
        ///     Decorations that appear around the construction site.
        /// </summary>
        public List<GameComponent> WorkObjects = new List<GameComponent>();

        /// <summary>
        ///     Create a build room order.
        /// </summary>
        /// <param name="toBuild">The room type to create.</param>
        /// <param name="faction">The faction that owns the room.</param>
        public BuildRoomOrder(Room toBuild, Faction faction)
        {
            ToBuild = toBuild;
            PutResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            VoxelOrders = new List<BuildVoxelOrder>();
            IsBuilt = false;
            Faction = faction;
        }

        /// <summary>
        ///     The kind of room to create.
        /// </summary>
        public Room ToBuild { get; set; }

        /// <summary>
        ///     The resources required to build the room.
        /// </summary>
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> PutResources { get; set; }

        /// <summary>
        ///     The voxels that make up the room.
        /// </summary>
        public List<BuildVoxelOrder> VoxelOrders { get; set; }

        /// <summary>
        ///     The faction the room belongs to.
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        ///     If true, the room has been constructed.
        /// </summary>
        public bool IsBuilt { get; set; }


        /// <summary>
        ///     Creates a bunch of work fences around the room during construction.
        ///     These are delted after the room has been built.
        /// </summary>
        public void CreateFences()
        {
            var neighbor = new Voxel();

            Vector3 half = Vector3.One*0.5f;
            Vector3 off = half + Vector3.Up;

            // Check each voxel and determine if it is on an edge of the room.
            // For each edge voxel, put a fence along its border.
            foreach (BuildVoxelOrder order in VoxelOrders)
            {
                Voxel voxel = order.Voxel;
                if (voxel.GetNeighbor(new Vector3(0, 0, 1), ref neighbor) &&
                    !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0, 0, 0.45f),
                        (float) Math.Atan2(0, 1)));
                }

                if (voxel.GetNeighbor(new Vector3(0, 0, -1), ref neighbor) &&
                    !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0, 0, -0.45f),
                        (float) Math.Atan2(0, -1)));
                }


                if (voxel.GetNeighbor(new Vector3(1, 0, 0), ref neighbor) &&
                    !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(0.45f, 0, 0.0f),
                        (float) Math.Atan2(1, 0)));
                }


                if (voxel.GetNeighbor(new Vector3(-1, 0, 0), ref neighbor) &&
                    !VoxelOrders.Any(o => o.Voxel.Equals(neighbor)))
                {
                    WorkObjects.Add(new WorkFence(voxel.Position + off + new Vector3(-0.45f, 0, 0.0f),
                        (float) Math.Atan2(-1, 0)));
                }

                if (MathFunctions.RandEvent(0.1f))
                {
                    WorkObjects.Add(new WorkPile(voxel.Position + off));
                }
            }
        }

        /// <summary>
        ///     Takes a list of resources and destroys them, putting them into the room construction site.
        ///     Once all the required resources have been found, the room can be built.
        /// </summary>
        /// <param name="resources">The resources to put into the construction site.</param>
        public void AddResources(List<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (var resource in resources)
            {
                if (PutResources.ContainsKey(resource.ResourceType))
                {
                    Quantitiy<Resource.ResourceTags> amount = PutResources[resource.ResourceType];
                    amount.NumResources += resource.NumResources;
                }
                else
                {
                    var amount = new Quantitiy<Resource.ResourceTags>();
                    amount.NumResources += resource.NumResources;
                    amount.ResourceType = resource.ResourceType;

                    PutResources[resource.ResourceType] = amount;
                }
            }

            if (MeetsBuildRequirements())
            {
                Build();
            }
        }

        /// <summary>
        ///     Determine if the room can be built already.
        /// </summary>
        /// <returns>If the room has all the required resources, returns true.</returns>
        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach (Resource.ResourceTags s in ToBuild.RoomData.RequiredResources.Keys)
            {
                if (!PutResources.ContainsKey(s))
                {
                    return false;
                }
                toReturn = toReturn &&
                           (PutResources[s].NumResources >=
                            Math.Max(
                                (int) (ToBuild.RoomData.RequiredResources[s].NumResources*VoxelOrders.Count*0.25f), 1));
            }

            return toReturn;
        }

        /// <summary>
        ///     Create the room, destroying any construction related decorations.
        /// </summary>
        public virtual void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            foreach (BuildVoxelOrder vox in VoxelOrders)
            {
                ToBuild.AddVoxel(vox.Voxel);
            }
            IsBuilt = true;
            ToBuild.IsBuilt = true;
            RoomLibrary.GenerateRoomComponentsTemplate(ToBuild, Faction.Components, PlayState.ChunkManager.Content,
                PlayState.ChunkManager.Graphics);
            ToBuild.OnBuilt();

            PlayState.AnnouncementManager.Announce("Built room!", ToBuild.ID + " was built");

            foreach (GameComponent fence in WorkObjects)
            {
                fence.Die();
            }
        }

        /// <summary>
        ///     Get the room's bounding box.
        /// </summary>
        /// <returns>A box encompassing all the room's voxels.</returns>
        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = VoxelOrders.Select(vox => vox.Voxel.GetBoundingBox()).ToList();

            return MathFunctions.GetBoundingBox(components);
        }

        /// <summary>
        ///     Determine if the construction site has enough of the required resources.
        /// </summary>
        /// <param name="name">The resource tag that must be satisfied.</param>
        /// <returns>True if the construciton site has enough of the resource, False otherwise.</returns>
        public bool IsResourceSatisfied(Resource.ResourceTags name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if (PutResources.ContainsKey(name))
            {
                current = PutResources[name].NumResources;
            }

            return current >= required;
        }

        /// <summary>
        ///     Gets the number of a specific kind of resource required to build the room.
        /// </summary>
        /// <param name="name">The kind of resource needed to build the room.</param>
        /// <returns>The number of resources of that kind needed.</returns>
        public int GetNumRequiredResources(Resource.ResourceTags name)
        {
            if (ToBuild.RoomData.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int) (ToBuild.RoomData.RequiredResources[name].NumResources*VoxelOrders.Count*0.25f), 1);
            }
            return 0;
        }

        /// <summary>
        ///     Gets a list of resource tags needed by this room.
        /// </summary>
        /// <returns>A list of resource tags required to build the room.</returns>
        public List<Quantitiy<Resource.ResourceTags>> ListRequiredResources()
        {
            var toReturn = new List<Quantitiy<Resource.ResourceTags>>();
            foreach (Resource.ResourceTags s in ToBuild.RoomData.RequiredResources.Keys)
            {
                int needed = Math.Max(
                    (int) (ToBuild.RoomData.RequiredResources[s].NumResources*VoxelOrders.Count*0.25f), 1);
                int currentResources = 0;

                if (PutResources.ContainsKey(s))
                {
                    currentResources = PutResources[s].NumResources;
                }

                if (currentResources >= needed)
                {
                    continue;
                }

                toReturn.Add(new Quantitiy<Resource.ResourceTags>(s, needed - currentResources));
            }

            return toReturn;
        }
    }
}