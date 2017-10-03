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
using System.Runtime.Serialization;

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
        [JsonIgnore]
        private WorldManager World { get; set; }
        public bool IsDestroyed { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = (WorldManager)ctx.Context;
        }

        public BuildRoomOrder()
        {

        }


        public BuildRoomOrder(Room toBuild, Faction faction, WorldManager world)
        {
            World = world;
            ToBuild = toBuild;
            PutResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            VoxelOrders = new List<BuildVoxelOrder>();
            IsBuilt = false;
            Faction = faction;
            IsDestroyed = false;
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
                ToBuild.AddVoxel(vox.Voxel);

            IsBuilt = true;
            ToBuild.IsBuilt = true;
            List<Body> components = RoomLibrary.GenerateRoomComponentsTemplate(
                ToBuild.RoomData, ToBuild.Voxels,
                World.ComponentManager, World.ChunkManager.Content, World.ChunkManager.Graphics);
            RoomLibrary.BuildAllComponents(components, ToBuild, World.ParticleManager);
            ToBuild.OnBuilt();

            if (!silent)
            {
                World.MakeAnnouncement(String.Format("{0} was built", ToBuild.ID), null);
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
                World.GoalManager.OnGameEvent(new Goals.Events.BuiltRoom(ToBuild.ID));
            }

            foreach (GameComponent fence in WorkObjects)
            {
                fence.Die();
            }
        }

        public void Destroy()
        {
            ToBuild.Destroy();
            foreach (GameComponent fence in WorkObjects)
            {
                fence.Die();
            }
            IsDestroyed = true;
        }

        public void SetTint(Color color)
        {
            foreach (var fence in WorkObjects)
            {
                SetDisplayColor(fence, color);
            }
        }

        private void SetDisplayColor(GameComponent body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>().ToList())
                sprite.VertexColorTint = color;
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
