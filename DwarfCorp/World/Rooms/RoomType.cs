using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class RoomType
    {
        public string Name;
        public string Description;
        public string DisplayName;
        public uint ID;
        public string FloorType;
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> RequiredResources;
        public Gui.TileReference NewIcon;
        public bool CanBuildAboveGround = true;
        public bool CanBuildBelowGround = true;
        public int MinimumSideLength = 3;
        public int MinimumSideWidth = 3;
        public int MaxNumRooms = int.MaxValue;

        public List<Quantitiy<Resource.ResourceTags>> GetRequiredResources(int numVoxels)
        {
            return RequiredResources.Select(r => new Quantitiy<Resource.ResourceTags>(r.Value) { Count = (int)(numVoxels * r.Value.Count * 0.25f) }).ToList();
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

            if (Voxels.Any(v => World.PersistentData.Designations.GetVoxelDesignation(v, DesignationType._All) != null))
            {
                World.UserInterface.ShowTooltip("Something else is designated for this area.");
                return false;
            }

            List<BoundingBox> boxes = Voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
            BoundingBox box = MathFunctions.GetBoundingBox(boxes);

            Vector3 extents = box.Max - box.Min;

            float maxExtents = Math.Max(extents.X, extents.Z);
            float minExtents = Math.Min(extents.X, extents.Z);

            if (maxExtents < MinimumSideLength || minExtents < MinimumSideWidth)
            {
                World.UserInterface.ShowTooltip("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth + ")!");
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

                if (World.RoomBuilder.IsInRoom(voxel) || World.RoomBuilder.IsBuildDesignation(voxel))
                {
                    World.UserInterface.ShowTooltip("Room's can't overlap!");
                    return false;
                }
            }

            return true;
        }
    }

}
