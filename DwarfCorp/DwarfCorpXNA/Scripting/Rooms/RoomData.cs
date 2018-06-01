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
    /// <summary>
    /// A BuildRoom data has a Name, alters the apperance of voxels, requires resources to build,
    /// and has item templates.
    /// </summary>
    public class RoomData
    {
        public string Name { get; set; }
        public uint ID { get; set; }
        public string FloorType { get; set; }
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> RequiredResources { get; set; }
        public List<RoomTemplate> Templates { get; set; }
        //public ImageFrame Icon { get; set; }
        public Gui.TileReference NewIcon { get; private set; }
        public string Description { get; set; }
        public bool CanBuildAboveGround = true;
        public bool CanBuildBelowGround = true;
        public bool CanBuildOnMultipleLevels = false;
        public bool MustBeBuiltOnSoil = false;
        public int MinimumSideLength = 3;
        public int MinimumSideWidth = 3;
        public int MaxNumRooms = int.MaxValue;

        public RoomData(string name, uint id, string floorTexture, Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> requiredResources, List<RoomTemplate> templates, Gui.TileReference icon)
        {
            Name = name;
            ID = id;
            FloorType = floorTexture;
            RequiredResources = requiredResources;
            Templates = templates;
            NewIcon = icon;
            Description = "";
        }

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
                World.ShowToolPopup(String.Format("We can only build {0} {1}. Destroy the existing to build a new one.", MaxNumRooms, Name));
                return false;
            }

            // Todo: Lift into helper function that uses better algorithm.
            List<BoundingBox> boxes = Voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
            BoundingBox box = MathFunctions.GetBoundingBox(boxes);

            Vector3 extents = box.Max - box.Min;

            float maxExtents = Math.Max(extents.X, extents.Z);
            float minExtents = Math.Min(extents.X, extents.Z);

            if (maxExtents < MinimumSideLength || minExtents < MinimumSideWidth)
            {
                World.ShowToolPopup("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth + ")!");
                return false;
            }

            int height = Voxels[0].Coordinate.Y;
            bool allEmpty = true;

            foreach (var voxel in Voxels)
            {
                if (voxel.IsEmpty)
                    continue;

                var above = VoxelHelpers.GetVoxelAbove(voxel);
                allEmpty &= (above.IsValid && above.IsEmpty);

                if (voxel.Type.IsInvincible) continue;

                if (height != (int)voxel.Coordinate.Y && !CanBuildOnMultipleLevels)
                {
                    World.ShowToolPopup("Room must be on flat ground!");
                    return false;
                }

                if (MustBeBuiltOnSoil && !voxel.Type.IsSoil)
                {
                    World.ShowToolPopup("Room must be built on soil!");
                    return false;
                }

                if (!CanBuildAboveGround && voxel.Sunlight)
                {
                    World.ShowToolPopup("Room can't be built aboveground!");
                    return false;
                }

                if (!CanBuildBelowGround && !voxel.Sunlight)
                {
                    World.ShowToolPopup("Room can't be built belowground!");
                    return false;
                }
            }

            if (!allEmpty)
            {
                World.ShowToolPopup("Room must be built in free space.");
                return false;
            }

            return true;
        }
    }

}
