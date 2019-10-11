using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class ZoneType
    {
        public string Name;
        public string Description;
        public string DisplayName;
        public uint ID;
        public string FloorType;
        public Dictionary<String, ResourceTagAmount> RequiredResources; // Todo: Disgusting.
        public Gui.TileReference NewIcon;
        public bool CanBuildAboveGround = true;
        public bool CanBuildBelowGround = true;
        public int MinimumSideLength = 3;
        public int MinimumSideWidth = 3;
        public int MaxNumRooms = int.MaxValue;
        public bool RequiresSpecificFloor = false;
        public bool Outline = true;

        public List<ResourceTagAmount> GetRequiredResources(int numVoxels)
        {
            return RequiredResources.Select(r => new ResourceTagAmount(r.Value.Tag, 1) { Count = (int)(numVoxels * r.Value.Count * 0.25f) }).ToList();
        }

        public bool CanBuildHere(List<VoxelHandle> Voxels, WorldManager World)
        {
            if (Voxels.Count == 0)
                return false;

            if (World.EnumerateZones().Where(room => room.Type.Name == this.Name).Count() + 1 > MaxNumRooms)
            {
                World.UserInterface.ShowTooltip(String.Format("We can only build {0} {1}. Destroy the existing to build a new one.", MaxNumRooms, Name));
                return false;
            }

            List<BoundingBox> boxes = Voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
            BoundingBox box = MathFunctions.GetBoundingBox(boxes);

            Vector3 extents = box.Max - box.Min;

            float maxExtents = Math.Max(extents.X, extents.Z);
            float minExtents = Math.Min(extents.X, extents.Z);

            //Voxels = VoxelHelpers.EnumerateCoordinatesInBoundingBox(box).Select(c => World.ChunkManager.CreateVoxelHandle(c)).ToList();

            if (Voxels.Any(v => World.PersistentData.Designations.GetVoxelDesignation(v, DesignationType._All).HasValue(out var _)))
            {
                World.UserInterface.ShowTooltip("Something else is designated for this area.");
                return false;
            }

            

            if (maxExtents < MinimumSideLength || minExtents < MinimumSideWidth)
            {
                World.UserInterface.ShowTooltip("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth + ")!");
                return false;
            }

            if (extents.Y != 1)
            {
                World.UserInterface.ShowTooltip("Only select one layer of voxels.");
                return false;
            }

            int height = Voxels[0].Coordinate.Y;

            foreach (var voxel in Voxels)
            {
                if (voxel.IsEmpty)
                {
                    World.UserInterface.ShowTooltip("Room must be built on solid ground.");
                    return false;
                }

                var above = VoxelHelpers.GetVoxelAbove(voxel);

                if (above.IsValid && !above.IsEmpty)
                {
                    World.UserInterface.ShowTooltip("Room must be built in free space.");
                    return false;
                }

                if (voxel.Type.IsInvincible) continue;

                if (height != (int)voxel.Coordinate.Y)
                {
                    World.UserInterface.ShowTooltip("Room must be on flat ground!");
                    return false;
                }
                
                if (!CanBuildAboveGround && voxel.Sunlight)
                {
                    World.UserInterface.ShowTooltip("Room can't be built aboveground!");
                    return false;
                }

                if (!CanBuildBelowGround && !voxel.Sunlight)
                {
                    World.UserInterface.ShowTooltip("Room can't be built belowground!");
                    return false;
                }

                if (World.IsInZone(voxel) || World.IsBuildDesignation(voxel))
                {
                    World.UserInterface.ShowTooltip("Room's can't overlap!");
                    return false;
                }

                if (RequiresSpecificFloor && voxel.Type.Name != FloorType)
                {
                    World.UserInterface.ShowTooltip("Room must be built on " + FloorType);
                    return false;
                }
            }

            return true;
        }
    }

}
