using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<VoxelStorage> Storage = new List<VoxelStorage>();
        public VoxelStorage.StorageType StoreType = VoxelStorage.StorageType.OnVoxel;


        public bool ReplaceVoxelTypes
        {
            get { return ReplacementType != null; }
        }

        public VoxelType ReplacementType { get; set; }

        [JsonIgnore]
        public ChunkManager Chunks { get; set; }

        public Zone(string id, ChunkManager chunks)
        {
            ID = id;
            ReplacementType = null;
            Chunks = chunks;
        }

        public Zone()
        {

        }

        public VoxelStorage GetStorage(VoxelRef voxel)
        {
            return Storage.FirstOrDefault(storage => storage.Voxel == voxel);
        }

        public void SetReserved(VoxelRef voxel, bool reserved)
        {
            VoxelStorage storage = GetStorage(voxel);

            if(storage != null)
            {
                storage.IsReserved = reserved;
            }
        }

        public void Destroy()
        {
            ClearItems();
            Storage.Clear();
        }

        public void ClearItems()
        {
            foreach(VoxelStorage storage in Storage)
            {
                storage.RemoveItem();
            }
        }

        public virtual bool IsFull()
        {
            return Storage.All(storage => storage.IsOccupied || storage.IsReserved);
        }

        public Item GetItemAt(VoxelRef vox)
        {
            return (from storage in Storage
                where storage.Voxel == vox && !storage.IsOccupied
                select storage.OwnedItem).FirstOrDefault();
        }

        public Item FindItemWithTag(string tag)
        {
            return (from storage in Storage
                where storage.IsOccupied && storage.OwnedItem.UserData.Tags.Contains(tag)
                select storage.OwnedItem).FirstOrDefault();
        }

        public LocatableComponent FindItemWithTag(string tag, List<LocatableComponent> ignores)
        {
            return (from storage in Storage
                where storage.IsOccupied && storage.OwnedItem.UserData.Tags.Contains(tag) && !ignores.Contains(storage.OwnedItem.UserData)
                select storage.OwnedItem.UserData).FirstOrDefault();
        }

        public List<Item> GetItemsWithTags(TagList tags)
        {
            return (from storage in Storage
                    where storage.IsOccupied && tags.Contains(storage.OwnedItem.UserData.Tags)
                    select storage.OwnedItem).ToList();
        }

        public Item FindItemWithTag(string tag, List<Item> ignores)
        {
            return (from storage in Storage
                where storage.IsOccupied && storage.OwnedItem.UserData.Tags.Contains(tag) && !ignores.Contains(storage.OwnedItem)
                select storage.OwnedItem).FirstOrDefault();
        }


        public Item FindNearestItemWithTags(TagList tags, Vector3 position, bool filterReserved = false)
        {
            List<Item> items = ListItems();

            float closestDist = float.MaxValue;
            Item closestItem = null;

            foreach(Item i in items)
            {
                if(!tags.Contains(i.UserData.Tags) || i.UserData.IsDead)
                {
                    continue;
                }

                if(filterReserved && i.ReservedFor != null)
                {
                    continue;
                }

                float dist = (i.UserData.GlobalTransform.Translation - position).LengthSquared();

                if(!(dist < closestDist))
                {
                    continue;
                }

                closestDist = dist;
                closestItem = i;
            }

            return closestItem;
        }

        public Item FindItemWithTags(List<string> tags)
        {
            return tags.Select(FindItemWithTag).FirstOrDefault(component => component != null);
        }

        public List<Item> ListItems()
        {
            return (from storage in Storage
                where storage.IsOccupied && storage.OwnedItem != null
                select storage.OwnedItem).ToList();
        }

        public bool ContainsVoxel(VoxelRef voxel)
        {
            return Storage.Any(store => store.Voxel.Equals(voxel));
        }

        public virtual void RemoveVoxel(VoxelRef voxel)
        {
            VoxelStorage toRemove = Storage.FirstOrDefault(store => store.Voxel.Equals(voxel));

            if(toRemove == null)
            {
                return;
            }

            toRemove.RemoveItem();
            Storage.Remove(toRemove);

            if(ReplaceVoxelTypes)
            {
                toRemove.RevertType(Chunks);
            }
        }

        public virtual void AddVoxel(VoxelRef voxel)
        {
            if(ContainsVoxel(voxel))
            {
                return;
            }

            VoxelStorage storage = new VoxelStorage(voxel, this, StoreType);
            Storage.Add(storage);

            if(ReplaceVoxelTypes)
            {
                storage.SetType(Chunks, ReplacementType);
            }
        }

        public VoxelRef GetNearestFreeVoxel(Vector3 position)
        {
            VoxelRef closest = null;
            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);
            double closestDist = double.MaxValue;

            foreach(VoxelStorage v in Storage)
            {
                if(v.IsOccupied || v.IsReserved)
                {
                    continue;
                }

                double d = (v.Voxel.WorldPosition - position + halfSize).LengthSquared();

                if(d < closestDist)
                {
                    closestDist = d;
                    closest = v.Voxel;
                }
            }

            return closest;
        }

        public bool ContainsItem(Item i)
        {
            return Storage.Any(store => store.OwnedItem == i);
        }

        public bool ContainsItem(LocatableComponent component)
        {
            return Storage.Any(store => store.IsOccupied && store.OwnedItem.UserData == component);
        }

        public virtual bool RemoveItem(LocatableComponent item)
        {
            foreach(VoxelStorage store in Storage.Where(store => store.IsOccupied && store.OwnedItem.UserData == item))
            {
                store.RemoveItem();
                return true;
            }

            return false;
        }

        public VoxelRef GetNearestVoxelTo(Vector3 location)
        {
            VoxelRef closest = null;
            float closestDist = float.MaxValue;

            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);

            foreach(VoxelStorage storage in Storage)
            {
                float dist = (storage.Voxel.WorldPosition - location + halfSize).LengthSquared();

                if(dist < closestDist)
                {
                    closestDist = dist;
                    closest = storage.Voxel;
                }
            }

            return closest;
        }

        public virtual bool AddItem(LocatableComponent component)
        {
            return AddItem(component, GetNearestFreeVoxel(component.LocalTransform.Translation + component.BoundingBoxPos));
        }

        public virtual bool AddItem(LocatableComponent component, VoxelRef voxel)
        {
            return AddItem(Item.CreateItem(component.Name + " " + component.GlobalID, this, component), voxel);
        }



        public virtual bool AddItem(Item i, VoxelRef voxel)
        {
            if(i == null)
            {
                return false;
            }

            foreach(VoxelStorage store in Storage.Where(store => store.Voxel == voxel && !store.IsOccupied))
            {
                store.OwnedItem = i;
                store.IsOccupied = true;
                return true;
            }

            return false;
        }


        public Item GetItemWithName(string name, bool remove)
        {
            foreach(VoxelStorage store in Storage)
            {
                if(!store.IsOccupied || store.OwnedItem.ID != name)
                {
                    continue;
                }

                Item i = store.OwnedItem;

                if(remove)
                {
                    store.RemoveItem();
                }

                return i;
            }

            return null;
        }


        public bool Intersects(BoundingBox box)
        {
            BoundingBox larger = new BoundingBox(box.Min - new Vector3(0.1f, 0.1f, 0.1f), box.Max + new Vector3(0.1f, 0.1f, 0.1f));

            return Storage.Any(storage => storage.Voxel.GetBoundingBox().Intersects(larger));
        }

        public bool Intersects(Voxel v)
        {
            return Intersects(v.GetBoundingBox());
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> boxes = Storage.Select(storage => storage.Voxel.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(boxes);
        }

        public bool IsInZone(Vector3 worldCoordinate)
        {
            return GetBoundingBox().Contains(worldCoordinate) != ContainmentType.Disjoint;
        }
    }

}