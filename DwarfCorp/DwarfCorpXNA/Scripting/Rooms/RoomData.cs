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


        public bool Verify(List<VoxelHandle> refs, Faction faction, WorldManager world)
        {
            if (refs.Count == 0)
            {
                return false;
            }

            

            List<BoundingBox> boxes = refs.Select(voxel => voxel.GetBoundingBox()).ToList();
            BoundingBox box = MathFunctions.GetBoundingBox(boxes);

            Vector3 extents = box.Max - box.Min;

            float maxExtents = Math.Max(extents.X, extents.Z);
            float minExtents = Math.Min(extents.X, extents.Z);

            if (maxExtents < MinimumSideLength || minExtents < MinimumSideWidth)
            {
                world.ShowToolPopup("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth + ")!");
                return false;
            }

            if (!HasAvailableResources(refs.Count, faction))
            {
                world.ShowToolPopup("Not enough resources for this room.");
                return false;
            }

            int height = -1;
            bool anyEmpty = false;
            foreach (VoxelHandle voxel in refs)
            {
                if (voxel.IsEmpty)
                {
                    continue;
                }

                var above = new TemporaryVoxelHandle(world.ChunkManager.ChunkData,
                    new GlobalVoxelCoordinate(voxel.Coordinate.X, voxel.Coordinate.Y + 1, voxel.Coordinate.Z));

                anyEmpty &= (above.IsValid && above.IsEmpty);

                if (voxel.Type.IsInvincible) continue;

                if (height == -1)
                {
                    height = (int)voxel.GridPosition.Y;
                }
                else if (height != (int) voxel.GridPosition.Y && !CanBuildOnMultipleLevels)
                {
                    world.ShowToolPopup("Room must be on flat ground!");
                    return false;
                }

                if (MustBeBuiltOnSoil)
                {
                    if (!voxel.Type.IsSoil)
                    {
                        world.ShowToolPopup("Room must be built on soil!");
                        return false;
                    }
                }

                if (!CanBuildAboveGround)
                {
                    if (voxel.Chunk.Data.SunColors[voxel.Index] <= 5) continue;

                    world.ShowToolPopup("Room can't be built aboveground!");
                    return false;
                } 
                else if (!CanBuildBelowGround)
                {
                    if (voxel.Chunk.Data.SunColors[voxel.Index] >= 5) continue;

                    world.ShowToolPopup("Room can't be built belowground!");
                    return false;
                }


            }

            if (!anyEmpty)
            {
                world.ShowToolPopup("Room must be built in free space.");
                return false;
            }

            return true;
        }
    }

}
