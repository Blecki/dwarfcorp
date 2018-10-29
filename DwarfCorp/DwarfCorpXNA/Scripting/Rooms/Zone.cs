// Zone.cs
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
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A zone is a collection of voxel storages.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Zone
    {
        public string ID = "";
        public List<VoxelHandle> Voxels = new List<VoxelHandle>();
        // This is a list of voxel type ids that existed before this zone
        // was created. When the zone is destroyed, the voxel types will be restored.
        public List<byte> OriginalVoxelTypes = new List<byte>(); 
        public List<Body> ZoneBodies = new List<Body>();
        
        [JsonProperty]
        protected int ResPerVoxel = 32;

        [JsonIgnore]
        public int ResourcesPerVoxel
        {
            get { return ResPerVoxel; }
            set { ResPerVoxel = value; RecalculateMaxResources(); }
        }
        
        [JsonIgnore]
        public bool ReplaceVoxelTypes
        {
            get { return ReplacementType != null; }
        }

        [JsonIgnore]
        public VoxelType ReplacementType { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        protected ChunkManager Chunks { get { return World.ChunkManager; } }

        public ResourceContainer Resources { get; set; }

        public Faction Faction { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = (WorldManager)ctx.Context;
            foreach (var body in ZoneBodies)
            {
                Body body1 = body;
                body.OnDestroyed += () => body_onDestroyed(body1);
            }
        }

        public Zone(string id, WorldManager world, Faction faction)
        {
            ID = id;
            ReplacementType = null;
            World = world;
            Resources = new ResourceContainer
            {
                MaxResources = 1
            };
            Faction = faction;

        }

        public Zone()
        {

        }

        public Body GetNearestBody(Vector3 location)
        {
            Body toReturn = null;
            float nearestDistance = float.MaxValue;

            foreach (Body body in ZoneBodies)
            {
                float dist = (location - body.GlobalTransform.Translation).LengthSquared();
                if (dist < nearestDistance)
                {
                    toReturn = body;
                    nearestDistance = dist;
                }
            }
            return toReturn;
        }

        public void SetTint(Color color)
        {
            foreach (var obj in ZoneBodies)
            {
                SetDisplayColor(obj, color);
            }
        }

        private void SetDisplayColor(GameComponent body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>())
                sprite.VertexColorTint = color;
        }

        public Body GetNearestBodyWithTag(Vector3 location, string tag, bool filterReserved)
        {
            Body toReturn = null;
            float nearestDistance = float.MaxValue;

            foreach (Body body in ZoneBodies)
            {
                if (!body.Tags.Contains(tag)) continue;
                if (filterReserved && (body.IsReserved || body.ReservedFor != null)) continue;
                float dist = (location - body.GlobalTransform.Translation).LengthSquared();
                if (dist < nearestDistance)
                {
                    toReturn = body;
                    nearestDistance = dist;
                }
            }
            return toReturn;
        }

        public void AddBody(Body body, bool addToOwnedObjects = true)
        {
            if (addToOwnedObjects)
                this.Faction.OwnedObjects.Add(body);
            ZoneBodies.Add(body);
            body.OnDestroyed += () => body_onDestroyed(body);
        }

        public void body_onDestroyed(Body body)
        {
            ZoneBodies.Remove(body);
        }

        public virtual void Destroy()
        {
            List<Body> toKill = new List<Body>();
            toKill.AddRange(ZoneBodies);
            foreach (Body body in toKill)
            {
                body.Die();
            }

            var voxelsToKill = new List<VoxelHandle>();
            voxelsToKill.AddRange(Voxels);
            foreach (var voxel in voxelsToKill)
            {
                World.ParticleManager.Trigger("dirt_particle", voxel.WorldPosition + Vector3.Up, Color.White, 1);
                RemoveVoxel(voxel);
            }

            ClearItems();
            Voxels.Clear();
        }

        public void ClearItems()
        {
            Resources.Clear();
            ZoneBodies.Clear();
        }

        public virtual bool IsFull()
        {
            return Resources.IsFull();
        }

        
        public bool ContainsVoxel(VoxelHandle voxel)
        {
            return Voxels.Any(store => store == voxel);
        }

        public virtual bool RemoveVoxel(VoxelHandle voxel)
        {
            bool removed = false;
            for (int i = 0; i < Voxels.Count; i++)
            {
                var toRemove = Voxels[i];
                if (toRemove != voxel)
                    continue;
                if (!toRemove.IsValid)
                    return true;

                Voxels.Remove(toRemove);
                toRemove.IsPlayerBuilt = false;
                removed = true;
                if (ReplaceVoxelTypes)
                {
                    if (OriginalVoxelTypes.Count > i)
                    {
                        toRemove.TypeID = OriginalVoxelTypes[i];
                        OriginalVoxelTypes.RemoveAt(i);
                    }
                }
                break;
            }
            RecalculateMaxResources();
            return removed;
        }

        public virtual void RecalculateMaxResources()
        {
            if (Voxels == null) return;
            int newResources = Voxels.Count * ResourcesPerVoxel;

            if (Resources != null)
            {
                if (newResources < Resources.CurrentResourceCount)
                {
                    while (Resources.CurrentResourceCount > newResources)
                    {
                        Resources.RemoveAnyResource();
                    }
                }

                Resources.MaxResources = newResources;
            }
        }

        public virtual void AddVoxel(VoxelHandle Voxel)
        {
            if(ContainsVoxel(Voxel))
                return;
            Voxel.IsPlayerBuilt = true;
            Voxels.Add(Voxel);
            if (ReplaceVoxelTypes)
            {
                OriginalVoxelTypes.Add(Voxel.TypeID);
                Voxel.Type = ReplacementType;
            }

            RecalculateMaxResources();
          
        }

        public VoxelHandle GetNearestVoxel(Vector3 position)
        {
            VoxelHandle closest = VoxelHandle.InvalidHandle;
            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);
            double closestDist = double.MaxValue;

            foreach (var v in Voxels)
            {
                double d = (v.WorldPosition - position + halfSize).LengthSquared();

                if(d < closestDist)
                {
                    closestDist = d;
                    closest = v;
                }
            }

            return closest;
        }


        public virtual bool AddItem(Body component)
        {
            return Resources.AddItem(component);
        }

       
        public bool Intersects(BoundingBox box)
        {
            BoundingBox larger = box.Expand(0.1f);
            return Voxels.Any(storage => storage.GetBoundingBox().Intersects(larger));
        }

        // Todo: Faster algorithm - find min and max voxel extents and create bounding box from that.
        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> boxes = Voxels.Select(storage => storage.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(boxes);
        }

        public bool IsInZone(Vector3 worldCoordinate)
        {
            return GetBoundingBox().Contains(worldCoordinate) != ContainmentType.Disjoint;
        }

        public bool IsRestingOnZone(Vector3 worldCoordinate, float expansion=1.0f)
        {
            BoundingBox box = GetBoundingBox();
            box.Max.Y += 1;
            box = box.Expand(expansion);
            return box.Contains(worldCoordinate) != ContainmentType.Disjoint;
        }
    }

}