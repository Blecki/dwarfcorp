using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class SaveData
    {

        public class VoxelPtr
        {
            public int[] Ptr { get; set;}

            public VoxelPtr()
            {
                Ptr = new int[6];
            }

            public VoxelPtr(VoxelRef voxel)
            {
                Ptr = new int[6];
                Ptr[0] = voxel.ChunkID.X;
                Ptr[1] = voxel.ChunkID.Y;
                Ptr[2] = voxel.ChunkID.Z;
                Ptr[3] = (int)voxel.GridPosition.X;
                Ptr[4] = (int)voxel.GridPosition.Y;
                Ptr[5] = (int)voxel.GridPosition.Z;
            }
        }

        public class EntityPtr
        {
            public uint ID { get; set; }
            public VoxelPtr Voxel { get; set; }


            public EntityPtr()
            {

            }

            public EntityPtr(uint id)
            {
                Voxel = null;
                ID = id;
            }

            public EntityPtr(uint id, VoxelPtr voxel)
            {
                ID = id;
                Voxel = voxel;
            }
        }

        public class ZoneData
        {
            public string Name { get; set; }
            public string Type { get; set; }

            public List<VoxelPtr> Voxels { get; set; }
            public List<EntityPtr> AttachedEntities { get; set; }

            public ZoneData()
            {
                Voxels = new List<VoxelPtr>();
                AttachedEntities = new List<EntityPtr>();
            }

            public ZoneData(Zone zone)
            {
                Name = zone.ID;
                Type = zone.GetType().ToString();
                Voxels = new List<VoxelPtr>();
                AttachedEntities = new List<EntityPtr>();


                foreach (VoxelStorage v in zone.Storage)
                {
                    VoxelPtr vox = new VoxelPtr(v.Voxel);
                    Voxels.Add(vox);

                    if (v.IsOccupied)
                    {
                        AttachedEntities.Add(new EntityPtr(v.OwnedItem.userData.GlobalID, vox));
                    }
                }
                
            }
        }


        public static string Extension = "json";
        public static string CompressedExtension = "zip"; 
        public virtual bool ReadFile(string filePath, bool isCompressed) { return false;  }
        public virtual bool WriteFile(string filePath, bool compress) { return false; }
        public static string[] GetFilesInDirectory(string dir, bool compressed, string compressedExtension, string extension)
        {
            if (compressed)
            {
                return System.IO.Directory.GetFiles(dir, "*." + compressedExtension);
            }
            else
            {
                return System.IO.Directory.GetFiles(dir, "*." + extension);
            }
        }
            
    }
}
