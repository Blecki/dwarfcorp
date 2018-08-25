// BuildTool.cs
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
            GameMaster Player, 
            Body PreviewBody,
            String Verb,
            String PastParticple)
        {            
            if (CraftType == null)
            {
                return false;
            }

            if (!String.IsNullOrEmpty(CraftType.CraftLocation) 
                && Player.Faction.FindNearestItemWithTags(CraftType.CraftLocation, Location.WorldPosition, false, null) == null)
            {
                Player.World.ShowToolPopup("Can't " + Verb + ", need " + CraftType.CraftLocation);
                return false;
            }

            foreach (var req in CraftType.Prerequisites)
            {
                switch (req)
                {
                    case CraftItem.CraftPrereq.NearWall:
                        {
                            var neighborFound = VoxelHelpers.EnumerateManhattanNeighbors2D(Location.Coordinate)
                                    .Select(c => new VoxelHandle(Player.World.ChunkManager.ChunkData, c))
                                    .Any(v => v.IsValid && !v.IsEmpty);

                            if (!neighborFound)
                            {
                                Player.World.ShowToolPopup("Must be " + PastParticple + " next to wall!");
                                return false;
                            }

                            break;
                        }
                    case CraftItem.CraftPrereq.OnGround:
                        {
                            var below = VoxelHelpers.GetNeighbor(Location, new GlobalVoxelOffset(0, -1, 0));

                            if (!below.IsValid || below.IsEmpty)
                            {
                                Player.World.ShowToolPopup("Must be " + PastParticple + " on solid ground!");
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

                foreach (var intersectingObject in Player.World.EnumerateIntersectingObjects(sensorBox, CollisionType.Static))
                {
                    if (Object.ReferenceEquals(intersectingObject, sensor)) continue;
                    var objectRoot = intersectingObject.GetRoot() as Body;
                    if (objectRoot is WorkPile) continue;
                    if (objectRoot != null && objectRoot.GetRotatedBoundingBox().Intersects(previewBox))
                    {
                        Player.World.ShowToolPopup("Can't " + Verb + " here: intersects " + objectRoot.Name);
                        return false;
                    }
                }

                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox
                    (PreviewBody.GetRotatedBoundingBox().Expand(-0.1f)).Any(
                    v =>
                    {
                        var tvh = new VoxelHandle(Player.World.ChunkManager.ChunkData, v);
                        return tvh.IsValid && !tvh.IsEmpty;
                    });

                if (intersectsWall && !CraftType.Prerequisites.Contains(CraftItem.CraftPrereq.NearWall))
                {
                    Player.World.ShowToolPopup("Can't " + Verb + " here: intersects wall.");
                    return false;
                }

            }
            Player.World.ShowToolPopup("");
            return true;
        }
    }
}
