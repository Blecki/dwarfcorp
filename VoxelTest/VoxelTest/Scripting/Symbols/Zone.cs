using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class Zone
    {
        public string ID = "";
        public List<VoxelStorage> Storage = new List<VoxelStorage>();
        public VoxelStorage.StorageType StoreType = VoxelStorage.StorageType.OnVoxel;
        public bool ReplaceVoxelTypes { get { return ReplacementType != null; } }
        public VoxelType ReplacementType { get; set; }
        public ChunkManager Chunks { get; set; }

        public Zone(string id, ChunkManager chunks)
        {
            ID = id;
            ReplacementType = null;
            Chunks = chunks;
        }

        public VoxelStorage GetStorage(VoxelRef voxel)
        {
            foreach (VoxelStorage storage in Storage)
            {
                if (storage.Voxel == voxel)
                {
                    return storage;
                }
            }

            return null;
        }

        public void SetReserved(VoxelRef voxel, bool reserved)
        {
            VoxelStorage storage = GetStorage(voxel);

            if (storage != null)
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
            foreach (VoxelStorage storage in Storage)
            {
                storage.RemoveItem();
            }
        }

        public virtual bool IsFull()
        {
            foreach (VoxelStorage storage in Storage)
            {
                if (!storage.IsOccupied && !storage.IsReserved)
                {
                    return false;
                }
            }

            return true;
        }

        public Item GetItemAt(VoxelRef vox)
        {
            foreach (VoxelStorage storage in Storage)
            {
                if (storage.Voxel == vox && !storage.IsOccupied)
                {
                    return storage.OwnedItem;
                }
            }

            return null;
        }

        public Item FindItemWithTag(string tag)
        {

            foreach (VoxelStorage storage in Storage)
            {
                if (storage.IsOccupied && storage.OwnedItem.userData.Tags.Contains(tag))
                {
                    return storage.OwnedItem;
                }
            }

            return null;
        }

        public LocatableComponent FindItemWithTag(string tag, List<LocatableComponent> ignores)
        {
            foreach (VoxelStorage storage in Storage)
            {
                if (storage.IsOccupied && storage.OwnedItem.userData.Tags.Contains(tag) && !ignores.Contains(storage.OwnedItem.userData))
                {
                    return storage.OwnedItem.userData;
                }
            }

            return null;
        }

        public Item FindItemWithTag(string tag, List<Item> ignores)
        {
            foreach (VoxelStorage storage in Storage)
            {
                if (storage.IsOccupied && storage.OwnedItem.userData.Tags.Contains(tag) && !ignores.Contains(storage.OwnedItem))
                {
                    return storage.OwnedItem;
                }
            }

            return null;
        }

        public Item FindNearestItemWithTags(TagList tags, Vector3 position)
        {
            List<Item> items = ListItems();

            float closestDist = float.MaxValue;
            Item closestItem = null;

            foreach (Item i in items)
            {
                if (tags.Contains(i.userData.Tags) && !i.userData.IsDead)
                {
                    float dist = (i.userData.GlobalTransform.Translation - position).LengthSquared();

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestItem = i;
                    }
                }
            }

            return closestItem;
        }

        public Item FindItemWithTags(List<string> tags)
        {
            foreach (string r in tags)
            {
                Item component = FindItemWithTag(r);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public List<Item> ListItems()
        {
            List<Item> toReturn = new List<Item>();

            foreach (VoxelStorage storage in Storage)
            {
                if (storage.IsOccupied && storage.OwnedItem != null)
                {
                    toReturn.Add(storage.OwnedItem);
                }
            }


            return toReturn;
        }

        public bool ContainsVoxel(VoxelRef voxel)
        {
            foreach (VoxelStorage store in Storage)
            {
                if (store.Voxel.Equals(voxel))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void RemoveVoxel(VoxelRef voxel)
        {
            VoxelStorage toRemove = null;
            foreach (VoxelStorage store in Storage)
            {
                if (store.Voxel.Equals(voxel))
                {
                    toRemove = store;
                    break;
                }
            }

            if (toRemove != null)
            {
                toRemove.RemoveItem();
                Storage.Remove(toRemove);

                if (ReplaceVoxelTypes)
                {
                    toRemove.RevertType(Chunks);
                }
            }

        }

        public virtual void AddVoxel(VoxelRef voxel)
        {
            if (!ContainsVoxel(voxel))
            {
                VoxelStorage storage = new VoxelStorage(voxel, this, StoreType);
                Storage.Add(storage);

                if (ReplaceVoxelTypes)
                {
                    storage.SetType(Chunks, ReplacementType);
                }
            }
        }

        public VoxelRef GetNearestFreeVoxel(Vector3 position)
        {
            VoxelRef closest = null;
            double closestDist = double.MaxValue;

            foreach (VoxelStorage v in Storage)
            {
                if (!v.IsOccupied && !v.IsReserved)
                {
                    double d = (v.Voxel.WorldPosition - position).LengthSquared();

                    if (d < closestDist)
                    {
                        closestDist = d;
                        closest = v.Voxel;
                    }
                }
            }

            return closest;
        }

        public bool ContainsItem(Item i)
        {
            foreach (VoxelStorage store in Storage)
            {
                if (store.OwnedItem == i)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItem(LocatableComponent component)
        {
            foreach (VoxelStorage store in Storage)
            {
                if (store.IsOccupied && store.OwnedItem.userData == component)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool RemoveItem(LocatableComponent item)
        {
            foreach (VoxelStorage store in Storage)
            {
                if (store.IsOccupied && store.OwnedItem.userData == item)
                {
                    Item i = store.OwnedItem;
                    store.RemoveItem();
                    return true;
                }
            }

            return false;
        }

        public virtual bool AddItem(LocatableComponent component, VoxelRef voxel)
        {
            return AddItem(Item.CreateItem(component.Name + " " + component.GlobalID, this, component), voxel);
        }

        public virtual bool AddItem(Item i, VoxelRef voxel)
        {
            if (i == null)
            {
                return false;
            }

            foreach (VoxelStorage store in Storage)
            {
                if (store.Voxel == voxel && !store.IsOccupied)
                {
                    store.OwnedItem = i;
                    store.IsOccupied = true;
                    return true;
                }
            }

            return false;
        }


        public Item GetItemWithName(string name, bool remove)
        {
            foreach (VoxelStorage store in Storage)
            {
                if (store.IsOccupied && store.OwnedItem.ID == name)
                {
                    Item i = store.OwnedItem;

                    if (remove)
                    {
                        store.RemoveItem();
                    }

                    return i;
                }
            }

            return null;
        }



        public bool Intersects(BoundingBox box)
        {
            BoundingBox larger = new BoundingBox(box.Min - new Vector3(0.1f, 0.1f, 0.1f), box.Max + new Vector3(0.1f, 0.1f, 0.1f));

            foreach (VoxelStorage storage in Storage)
            {
                if (storage.Voxel.GetBoundingBox().Intersects(larger))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Intersects(Voxel v)
        {
            return Intersects(v.GetBoundingBox());
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> boxes = new List<BoundingBox>();
            foreach (VoxelStorage storage in Storage)
            {
                boxes.Add(storage.Voxel.GetBoundingBox());
            }


            return LinearMathHelpers.GetBoundingBox(boxes);
        }

        public bool IsInZone(Vector3 worldCoordinate)
        {
            return GetBoundingBox().Contains(worldCoordinate) != ContainmentType.Disjoint;
        }
    }

}
