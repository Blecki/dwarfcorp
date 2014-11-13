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
        public int ResourcesPerVoxel { get { return ResPerVoxel; } set { ResPerVoxel = value; RecalculateMaxResources(); } }
        
        public bool ReplaceVoxelTypes
        {
            get { return ReplacementType != null; }
        }

        public VoxelType ReplacementType { get; set; }

        [JsonIgnore]
        public ChunkManager Chunks { get; set; }

        public ResourceContainer Resources { get; set; }

        public Zone(string id, ChunkManager chunks)
        {
            ID = id;
            ReplacementType = null;
            Chunks = chunks;
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
    }

}