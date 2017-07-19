// GodModeTool.cs
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    /// <summary>
    /// This is the debug tool that allows the player to mess with the engine.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GodModeTool : PlayerTool
    {
        public String Command;

        public ChunkManager Chunks { get; set; }

        public override void OnBegin()
        {
            Player.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(Command);
        }

        public override void OnEnd()
        {
        }

        public override void OnVoxelsDragged(List<TemporaryVoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public GodModeTool(GameMaster master)
        {
            Player = master;
            Chunks = Player.World.ChunkManager;
        }

        private VoxelSelectionType GetSelectionTypeBySelectionBoxValue(string arg)
        {
            if (arg == "Delete Block" || arg.Contains("Build") || arg == "Kill Block")
            {
                return VoxelSelectionType.SelectFilled;
            }
            else
            {
                return VoxelSelectionType.SelectEmpty;
            }
        }

        private void SelectorBox_OnSelectionModified(string arg)
        {
            Player.VoxSelector.SelectionType = GetSelectionTypeBySelectionBoxValue(arg);
        }

        public override void OnVoxelsSelected(List<TemporaryVoxelHandle> refs, InputManager.MouseButton button)
        {
           
            var chunksToRebuild = new HashSet<GlobalVoxelCoordinate>();

            if(Command.Contains("Build/"))
            {
                string type = Command.Substring(6);
                BuildRoomOrder des = new BuildRoomOrder(RoomLibrary.CreateRoom(Player.Faction, type, refs, false, Player.World), Player.Faction, Player.World);
                des.ToBuild.Designations = refs;
                Player.Faction.RoomBuilder.BuildDesignations.Add(des);
                Player.Faction.RoomBuilder.DesignatedRooms.Add(des.ToBuild);
                des.Build();
            }
            else if (Command.Contains("Spawn/"))
            {
                string type = Command.Substring(6);
                foreach (var vox in refs.Where(vox => vox.IsValid))
                {
                    if (vox.IsEmpty)
                    {
                        EntityFactory.CreateEntity<Body>(type, vox.Coordinate.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
                    }
                }
            }
            else
            {
                foreach(var vox in refs.Where(vox => vox.IsValid))
                {
                    if(Command.Contains("Place/"))
                    {
                        string type = Command.Substring(6);
                        var v = vox;
                        v.Type = VoxelLibrary.GetVoxelType(type);
                        v.WaterCell = new WaterCell();
                        v.Health = vox.Type.StartingHealth;

                        if (type == "Magic")
                        {
                            Player.World.ComponentManager.RootComponent.AddChild(
                                new VoxelListener(Player.World.ComponentManager, Player.World.ChunkManager, vox)
                                {
                                    DestroyOnTimer = true,
                                    DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                                });
                        }


                        chunksToRebuild.Add(vox.Coordinate);
                    }
                    else switch(Command)
                    {
                        case "Delete Block":
                        {
                                    var v = vox;
                            Player.World.Master.Faction.OnVoxelDestroyed(vox);
                            vox.Chunk.NotifyDestroyed(vox.Coordinate.GetLocalVoxelCoordinate());
                            v.Type = VoxelType.TypeList[0];
                            v.WaterCell = new WaterCell();

                            vox.Chunk.Manager.KilledVoxels.Add(vox);
                        }
                            break;
                        case "Kill Block":
                                foreach (var selected in refs)
                                {
                                    if (!selected.IsEmpty)
                                        Player.World.ChunkManager.KillVoxel(selected);

                                }
                            break;
                        case "Fill Water":
                        {
                            if (vox.IsEmpty)
                            {
                                        var v = vox;
                                        v.WaterCell = new WaterCell
                                        {
                                            Type = LiquidType.Water,
                                            WaterLevel = WaterManager.maxWaterLevel
                                        };

                                chunksToRebuild.Add(vox.Coordinate);
                            }
                        }
                            break;
                        case "Fill Lava":
                        {
                            if (vox.IsEmpty)
                            {
                                        var v = vox;
                                        v.WaterCell = new WaterCell
                                        {
                                            Type = LiquidType.Lava,
                                            WaterLevel = WaterManager.maxWaterLevel
                                        };
                                        chunksToRebuild.Add(vox.Coordinate);
                            }
                        }
                            break;
                        case "Fire":
                        {
                            foreach(var flam2 in 
                                Player.World.CollisionManager.EnumerateIntersectingObjects(vox.GetBoundingBox(), CollisionManager.CollisionType.Both).OfType<GameComponent>().SelectMany(c => c.EnumerateAll()).OfType<Flammable>())
                            {
                                flam2.Heat = flam2.Flashpoint + 1;
                            }
                        }
                            break;
                        case "Kill Things":
                        {
                            foreach(var comp in Player.World.CollisionManager.EnumerateIntersectingObjects(
                                vox.GetBoundingBox(), CollisionManager.CollisionType.Both).OfType<Body>())
                            {
                                comp.Die();
                            }
                        }
                            break;
                        case "Disease":
                        {
                            foreach(var comp in Player.World.CollisionManager.EnumerateIntersectingObjects(
                                vox.GetBoundingBox(), CollisionManager.CollisionType.Both).OfType<Body>())
                            {
                                var creature = comp.GetComponent<Creature>();
                                if (creature != null)
                                {
                                    var disease = Datastructures.SelectRandom(DiseaseLibrary.Diseases);
                                    creature.AcquireDisease(disease.Name);
                                }
                            }
                            break;
                        }
                        default:
                            break;
                    }
                }
            }

            foreach (var chunk in chunksToRebuild)
                Chunks.ChunkData.NotifyRebuild(chunk);
        }


        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            Player.World.SetMouse(Player.World.MousePointer);
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
          
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }

}
