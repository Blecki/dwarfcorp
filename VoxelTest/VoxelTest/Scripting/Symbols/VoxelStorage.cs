using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class VoxelStorage
    {
        public VoxelRef Voxel { get; set; }
        public VoxelType OriginalType { get; set; }
        public bool IsOccupied { get; set; }
        public bool IsReserved { get; set;}

        private Item m_ownedItem = null;
        public Item OwnedItem { get { return m_ownedItem; } set { m_ownedItem = value; if (m_ownedItem != null) { m_ownedItem.Zone = ParentZone; IsOccupied = true; } else { IsOccupied = false; } } }
        public Zone ParentZone { get; set; }
        public StorageType StoreType { get; set;}

        public enum StorageType { InVoxel, OnVoxel }


        public VoxelStorage(VoxelRef voxel, Zone parentZone, StorageType storeType)
        {
            Voxel = voxel;
            IsOccupied = false;
            ParentZone = parentZone;
            StoreType = storeType;
            IsReserved = false;
            OriginalType = VoxelLibrary.GetVoxelType(Voxel.TypeName);
        }

        public void SetType(ChunkManager chunks, VoxelType type)
        {
            Voxel vox = Voxel.GetVoxel(chunks, false);

            if (vox != null)
            {
                vox.Type = type;
                vox.Primitive = VoxelLibrary.GetPrimitive(vox.Type);
                vox.Chunk.ShouldRebuild = true;
            }
        }

        public void RevertType(ChunkManager chunks)
        {
            Voxel vox = Voxel.GetVoxel(chunks, false);

            if (vox != null)
            {
                vox.Type = OriginalType;
                vox.Primitive = VoxelLibrary.GetPrimitive(vox.Type);
                vox.Chunk.ShouldRebuild = true;
            }
        }

        public void RemoveItem()
        {
            if (m_ownedItem != null)
            {
                OwnedItem.Zone = null;
                m_ownedItem = null;
            }
            IsOccupied = false;
        }



    }
}
