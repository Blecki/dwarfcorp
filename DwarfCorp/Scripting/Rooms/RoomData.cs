using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class RoomData
    {
        public string Name;
        public string Description;
        public string DisplayName;
        public uint ID;
        public string FloorType;
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> RequiredResources;
        public Gui.TileReference NewIcon;
        public string Descraiption;
        public bool CanBuildAboveGround = true;
        public bool CanBuildBelowGround = true;
        public int MinimumSideLength = 3;
        public int MinimumSideWidth = 3;
        public int MaxNumRooms = int.MaxValue;

        public List<Quantitiy<Resource.ResourceTags> > GetRequiredResources(int numVoxels, Faction faction)
        {
            List<Quantitiy<Resource.ResourceTags> > toReturn = new List<Quantitiy<Resource.ResourceTags>>();
            foreach (var resources in RequiredResources)
            {
                Quantitiy<Resource.ResourceTags> required = new Quantitiy<Resource.ResourceTags>(resources.Value)
                {
                    Count = (int)(numVoxels * resources.Value.Count * 0.25f)
                };

                toReturn.Add(required);
            }

            return toReturn;
        }

        public bool HasAvailableResources(int numVoxels, Faction faction)
        {
            foreach (var resources in RequiredResources)
            {
                Quantitiy<Resource.ResourceTags> required = new Quantitiy<Resource.ResourceTags>(resources.Value)
                {
                    Count = (int) (numVoxels*resources.Value.Count*0.25f)
                };

                if (!faction.HasResources(new List<Quantitiy<Resource.ResourceTags>>() { required }))
                {
                    return false;
                }
            }

            return true;
        }


        public bool Verify(
            List<VoxelHandle> Voxels, 
            Faction Faction, 
            WorldManager World)
        {
            if (Voxels.Count == 0)
                return false;

            if (Faction.GetRooms().Where(room => room.Type.Name == this.Name).Count() + 1 > MaxNumRooms)
            {
                World.UserInterface.ShowTooltip(String.Format("We can only build {0} {1}. Destroy the existing to build a new one.", MaxNumRooms, Name));
                return false;
            }

            if (Voxels.Any(v => Faction.Designations.GetVoxelDesignation(v, DesignationType._All) != null))
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

                if (Faction.RoomBuilder.IsInRoom(voxel) || Faction.RoomBuilder.IsBuildDesignation(voxel))
                {
                    World.UserInterface.ShowTooltip("Room's can't overlap!");
                    return false;
                }
            }

            return true;
        }
    }

}
