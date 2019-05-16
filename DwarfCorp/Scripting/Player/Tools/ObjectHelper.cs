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
            CraftItem CraftType, 
            WorldManager World, 
            GameComponent PreviewBody,
            String Verb,
            String PastParticple)
        {            
            if (CraftType == null)
            {
                return false;
            }

            if (!String.IsNullOrEmpty(CraftType.CraftLocation) 
                && World.PlayerFaction.FindNearestItemWithTags(CraftType.CraftLocation, Location.WorldPosition, false, null) == null)
            {
                World.ShowTooltip("Can't " + Verb + ", need " + CraftType.CraftLocation);
                return false;
            }

            foreach (var req in CraftType.Prerequisites)
            {
                switch (req)
                {
                    case CraftItem.CraftPrereq.NearWall:
                        {
                            var neighborFound = VoxelHelpers.EnumerateManhattanNeighbors2D(Location.Coordinate)
                                    .Select(c => new VoxelHandle(World.ChunkManager, c))
                                    .Any(v => v.IsValid && !v.IsEmpty);

                            if (!neighborFound)
                            {
                                World.ShowTooltip("Must be " + PastParticple + " next to wall!");
                                return false;
                            }

                            break;
                        }
                    case CraftItem.CraftPrereq.OnGround:
                        {
                            var below = VoxelHelpers.GetNeighbor(Location, new GlobalVoxelOffset(0, -1, 0));

                            if (!below.IsValid || below.IsEmpty)
                            {
                                World.ShowTooltip("Must be " + PastParticple + " on solid ground!");
                                return false;
                            }
                            break;
                        }
                }
            }

            if (PreviewBody != null)
            {
                // Just check for any intersecting body in octtree.

                var previewBox = PreviewBody.GetRotatedBoundingBox();
                var sensorBox = previewBox;
                var sensor = PreviewBody.GetComponent<GenericVoxelListener>();
                if (sensor != null)
                    sensorBox = sensor.GetRotatedBoundingBox();
                if (Debugger.Switches.DrawToolDebugInfo)
                    Drawer3D.DrawBox(sensorBox, Color.Yellow, 0.1f, false);

                foreach (var intersectingObject in World.EnumerateIntersectingObjects(sensorBox, CollisionType.Static))
                {
                    if (Object.ReferenceEquals(intersectingObject, sensor)) continue;
                    var objectRoot = intersectingObject.GetRoot() as GameComponent;
                    if (objectRoot is WorkPile) continue;
                    if (objectRoot == PreviewBody) continue; 
                    if (objectRoot != null && objectRoot.GetRotatedBoundingBox().Intersects(previewBox))
                    {
                        World.ShowTooltip("Can't " + Verb + " here: intersects " + objectRoot.Name);
                        return false;
                    }
                }

                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox
                    (PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)).Any(
                    v =>
                    {
                        var tvh = new VoxelHandle(World.ChunkManager, v);
                        return tvh.IsValid && !tvh.IsEmpty;
                    });
                var current = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(PreviewBody.Position));
                bool underwater = current.IsValid && current.LiquidType != LiquidType.None;
                if (underwater)
                {
                    World.ShowTooltip("Can't " + Verb + " here: underwater or in lava.");
                    return false;
                }
                if (intersectsWall && !CraftType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall))
                {
                    World.ShowTooltip("Can't " + Verb + " here: intersects wall.");
                    return false;
                }

            }
            World.ShowTooltip("");
            return true;
        }
    }
}
