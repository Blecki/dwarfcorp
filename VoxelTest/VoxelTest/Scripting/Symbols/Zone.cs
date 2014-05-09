using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<VoxelRef> Voxels = new List<VoxelRef>();
       
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

      

        public void Destroy()
        {
            ClearItems();
            Voxels.Clear();
        }

        public void ClearItems()
        {
            Resources.Clear();
        }

        public virtual bool IsFull()
        {
            return Resources.IsFull();
        }

        
        public bool ContainsVoxel(VoxelRef voxel)
        {
            return Voxels.Any(store => store.Equals(voxel));
        }

        public virtual void RemoveVoxel(VoxelRef voxel)
        {
            VoxelRef toRemove = Voxels.FirstOrDefault(store => store.Equals(voxel));

            if(toRemove == null)
            {
                return;
            }

            Voxels.Remove(toRemove);

            if(ReplaceVoxelTypes)
            {
                toRemove.GetVoxel(false).Kill();
            }

            RecalculateMaxResources();
        }

        public virtual void RecalculateMaxResources()
        {
            int newResources = Voxels.Count * ResourcesPerVoxel;
            if(newResources < Resources.CurrentResourceCount)
            {
                while(Resources.CurrentResourceCount > newResources)
                {
                    Resources.RemoveAnyResource();
                }
            }

            Resources.MaxResources = newResources;
        }

        public virtual void AddVoxel(VoxelRef voxel)
        {
            if(ContainsVoxel(voxel))
            {
                return;
            }

            Voxels.Add(voxel);

            if(ReplaceVoxelTypes)
            {
                Voxel v = voxel.GetVoxel(false);
                v.Type = ReplacementType;
                v.Chunk.ShouldRebuild = true;
            }

            RecalculateMaxResources();
          
        }

        public VoxelRef GetNearestVoxel(Vector3 position)
        {
            VoxelRef closest = null;
            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);
            double closestDist = double.MaxValue;

            foreach (VoxelRef v in Voxels)
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
            return Resources.AddItem(component); //AddItem(component, GetNearestFreeVoxel(component.LocalTransform.Translation + component.BoundingBoxPos, false));
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