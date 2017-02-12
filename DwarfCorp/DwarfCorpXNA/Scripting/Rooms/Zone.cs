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
        public List<Voxel> Voxels = new List<Voxel>();
        public List<Body> ZoneBodies = new List<Body>();
        
        [JsonProperty]
        protected int ResPerVoxel = 8;
        [JsonIgnore]
        public int ResourcesPerVoxel { get { return ResPerVoxel; } set { ResPerVoxel = value; RecalculateMaxResources(); } }
        
        public bool ReplaceVoxelTypes
        {
            get { return ReplacementType != null; }
        }

        public VoxelType ReplacementType { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        protected ChunkManager Chunks { get { return World.ChunkManager; } }

        public ResourceContainer Resources { get; set; }

        public Zone(string id, WorldManager world)
        {
            ID = id;
            ReplacementType = null;
            World = world;
            Resources = new ResourceContainer
            {
                MaxResources = 1
            };

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

        public void AddBody(Body body)
        {
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

            List<Voxel> voxelsToKill = new List<Voxel>();
            voxelsToKill.AddRange(Voxels);
            foreach (Voxel voxel in voxelsToKill)
            {
                voxel.Kill();
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

        
        public bool ContainsVoxel(Voxel voxel)
        {
            return Voxels.Any(store => store.Equals(voxel));
        }

        public virtual void RemoveVoxel(Voxel voxel)
        {
            Voxel toRemove = Voxels.FirstOrDefault(store => store.Equals(voxel));

            if(toRemove == null)
            {
                return;
            }

            Voxels.Remove(toRemove);

            if(ReplaceVoxelTypes)
            {
                toRemove.Kill();
            }

            RecalculateMaxResources();
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

        public virtual void AddVoxel(Voxel voxel)
        {
            if(ContainsVoxel(voxel))
            {
                return;
            }

            Voxels.Add(voxel);

            if(ReplaceVoxelTypes)
            {
                Voxel v = voxel;
                v.Type = ReplacementType;
                v.Chunk.ShouldRebuild = true;
            }

            RecalculateMaxResources();
          
        }

        public Voxel GetNearestVoxel(Vector3 position)
        {
            Voxel closest = null;
            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);
            double closestDist = double.MaxValue;

            foreach (Voxel v in Voxels)
            {
                double d = (v.Position - position + halfSize).LengthSquared();

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
            BoundingBox larger = new BoundingBox(box.Min - new Vector3(0.1f, 0.1f, 0.1f), box.Max + new Vector3(0.1f, 0.1f, 0.1f));

            return Voxels.Any(storage => storage.GetBoundingBox().Intersects(larger));
        }

        public bool Intersects(Voxel v)
        {
            return Intersects(v.GetBoundingBox());
        }

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