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
        public Dictionary<ResourceLibrary.ResourceType, ResourceAmount> RequiredResources { get; set; }
        public List<RoomTemplate> Templates { get; set; }
        public ImageFrame Icon { get; set; }
        public string Description { get; set; }
        public bool CanBuildAboveGround = true;
        public bool CanBuildBelowGround = true;
        public bool CanBuildOnMultipleLevels = false;
        public bool MustBeBuiltOnSoil = false;
        public int MinimumSideLength = 3;
        public int MinimumSideWidth = 3;

        public RoomData(string name, uint id, string floorTexture, Dictionary<ResourceLibrary.ResourceType, ResourceAmount> requiredResources, List<RoomTemplate> templates, ImageFrame icon)
        {
            Name = name;
            ID = id;
            FloorType = floorTexture;
            RequiredResources = requiredResources;
            Templates = templates;
            Icon = icon;
            Description = "";
        }

        public bool HasAvailableResources(int numVoxels, Faction faction)
        {
            foreach (KeyValuePair<ResourceLibrary.ResourceType, ResourceAmount> resources in RequiredResources)
            {
                ResourceAmount required = new ResourceAmount(resources.Value)
                {
                    NumResources = (int) (numVoxels*resources.Value.NumResources*0.25f)
                };
                if (!faction.HasResources(new List<ResourceAmount>() {required}))
                {
                    return false;
                }
            }

            return true;
        }


        public bool Verify(List<Voxel> refs, Faction faction)
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
                PlayState.GUI.ToolTipManager.Popup("Room is too small (minimum is " + MinimumSideLength + " x " + MinimumSideWidth +")!");
                return false;
            }

            if (!HasAvailableResources(refs.Count, faction))
            {
                PlayState.GUI.ToolTipManager.Popup("Not enough resources for this room.");
                return false;
            }

            int height = -1;
            foreach (Voxel voxel in refs)
            {

                if (voxel.IsEmpty) continue;

                if (height == -1)
                {
                    height = (int)voxel.GridPosition.Y;
                }
                else if (height != (int) voxel.GridPosition.Y && !CanBuildOnMultipleLevels)
                {
                    PlayState.GUI.ToolTipManager.Popup("Room must be on flat ground!");
                    return false;
                }

                if (MustBeBuiltOnSoil)
                {
                    if (!voxel.Type.IsSoil)
                    {
                        PlayState.GUI.ToolTipManager.Popup("Room must be built on soil!");
                        return false;
                    }
                }

                if (!CanBuildAboveGround)
                {
                    if (voxel.Chunk.Data.SunColors[voxel.Index] <= 5) continue;

                    PlayState.GUI.ToolTipManager.Popup("Room can't be built aboveground!");
                    return false;
                } 
                else if (!CanBuildBelowGround)
                {
                    if (voxel.Chunk.Data.SunColors[voxel.Index] >= 5) continue;

                    PlayState.GUI.ToolTipManager.Popup("Room can't be built belowground!");
                    return false;
                }


            }

            return true;
        }
    }

}