using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Input;


namespace DwarfCorp
{
    public static class ObjectHelper
    {
        public static bool IsValidPlacement(
            VoxelHandle Location, 
            ResourceType CraftType, 
            WorldManager World, 
            GameComponent PreviewBody,
            String Verb,
            String PastParticple)
        {            
            if (CraftType == null)
                return false;

            switch (CraftType.Placement_PlacementRequirement)
            {
                case ResourceType.PlacementRequirement.NearWall:
                    {
                        var neighborFound = VoxelHelpers.EnumerateManhattanNeighbors2D(Location.Coordinate)
                                .Select(c => new VoxelHandle(World.ChunkManager, c))
                                .Any(v => v.IsValid && !v.IsEmpty);

                        if (!neighborFound)
                        {
                            World.UserInterface.ShowTooltip("Must be " + PastParticple + " next to wall!");
                            return false;
                        }

                        break;
                    }
                case ResourceType.PlacementRequirement.OnGround:
                    {
                        var below = VoxelHelpers.GetNeighbor(Location, new GlobalVoxelOffset(0, -1, 0));

                        if (!below.IsValid || below.IsEmpty)
                        {
                            World.UserInterface.ShowTooltip("Must be " + PastParticple + " on solid ground!");
                            return false;
                        }
                        break;
                    }
            }

            if (PreviewBody != null)
            {
                // Just check for any intersecting body in octtree.

                var previewBox = PreviewBody.GetRotatedBoundingBox();
                var sensorBox = previewBox;

                GenericVoxelListener sensor;
                if (PreviewBody.GetComponent<GenericVoxelListener>().HasValue(out sensor))
                    sensorBox = sensor.GetRotatedBoundingBox();

                if (Debugger.Switches.DrawToolDebugInfo)
                    Drawer3D.DrawBox(sensorBox, Color.Yellow, 0.1f, false);

                foreach (var intersectingObject in World.EnumerateIntersectingAnchors(sensorBox))
                {
                    if (Object.ReferenceEquals(intersectingObject, sensor)) continue;

                    if (Debugger.Switches.DrawToolDebugInfo)
                        Drawer3D.DrawBox(intersectingObject.GetRotatedBoundingBox(), Color.Violet, 0.1f, false);

                    if (intersectingObject.IsDead) continue;
                    World.UserInterface.ShowTooltip("Can't " + Verb + " here: intersects " + intersectingObject.Name);
                    return false;
                }

                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox
                    (PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)).Any(
                    v =>
                    {
                        var tvh = new VoxelHandle(World.ChunkManager, v);
                        return tvh.IsValid && !tvh.IsEmpty;
                    });

                var current = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(PreviewBody.Position));
                bool underwater = current.IsValid && current.LiquidType != 0;

                if (underwater)
                {
                    World.UserInterface.ShowTooltip("Can't " + Verb + " here: underwater or in lava.");
                    return false;
                }

                if (intersectsWall && CraftType.Placement_PlacementRequirement != ResourceType.PlacementRequirement.NearWall)
                {
                    World.UserInterface.ShowTooltip("Can't " + Verb + " here: intersects wall.");
                    return false;
                }

            }
            World.UserInterface.ShowTooltip("");
            return true;
        }
    }
}
