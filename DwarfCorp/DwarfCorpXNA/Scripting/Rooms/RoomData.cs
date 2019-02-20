// RoomData.cs
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
                    NumResources = (int)(numVoxels * resources.Value.NumResources * 0.25f)
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
                    NumResources = (int) (numVoxels*resources.Value.NumResources*0.25f)
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

            if (Faction.GetRooms().Where(room => room.RoomData.Name == this.Name).Count() + 1 > MaxNumRooms)
            {
                World.ShowTooltip(String.Format("We can only build {0} {1}. Destroy the existing to build a new one.", MaxNumRooms, Name));
                return false;
            }

            if (Voxels.Any(v => Faction.Designations.GetVoxelDesignation(v, DesignationType._All) != null))
            {
                World.ShowTooltip("Something else is designated for this area.");
                return false;
            }

            List<BoundingBox> boxes = Voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
            BoundingBox box = MathFunctions.GetBoundingBox(boxes);

            Vector3 extents = box.Max - box.Min;

            float maxExtents = Math.Max(extents.X, extents.Z);
            float minExtents = Math.Min(extents.X, extents.Z);

            if (maxExtents < MinimumSideLength || minExtents < MinimumSideWidth)
            {
                World.ShowTooltip("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth + ")!");
                return false;
            }

            int height = Voxels[0].Coordinate.Y;

            foreach (var voxel in Voxels)
            {
                if (voxel.IsEmpty)
                {
                    World.ShowTooltip("Room must be built on solid ground.");
                    return false;
                }

                var above = VoxelHelpers.GetVoxelAbove(voxel);

                if (above.IsValid && !above.IsEmpty)
                {
                    World.ShowTooltip("Room must be built in free space.");
                    return false;
                }

                if (voxel.Type.IsInvincible) continue;

                if (height != (int)voxel.Coordinate.Y)
                {
                    World.ShowTooltip("Room must be on flat ground!");
                    return false;
                }
                
                if (!CanBuildAboveGround && voxel.Sunlight)
                {
                    World.ShowTooltip("Room can't be built aboveground!");
                    return false;
                }

                if (!CanBuildBelowGround && !voxel.Sunlight)
                {
                    World.ShowTooltip("Room can't be built belowground!");
                    return false;
                }

                if (Faction.RoomBuilder.IsInRoom(voxel) || Faction.RoomBuilder.IsBuildDesignation(voxel))
                {
                    World.ShowTooltip("Room's can't overlap!");
                    return false;
                }
            }

            return true;
        }
    }

}
