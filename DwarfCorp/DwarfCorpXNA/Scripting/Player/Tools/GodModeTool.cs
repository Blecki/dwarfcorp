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

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
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

        public override void OnVoxelsSelected(List<Voxel> refs, InputManager.MouseButton button)
        {
           
            HashSet<Point3> chunksToRebuild = new HashSet<Point3>();

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
                foreach (Voxel vox in refs.Where(vox => vox != null))
                {
                    if (vox.IsEmpty)
                    {
                        EntityFactory.CreateEntity<Body>(type, vox.Position + new Vector3(0.5f, 0.5f, 0.5f));
                    }
                }
            }
            else
            {
                foreach(Voxel vox in refs.Where(vox => vox != null))
                {
                    if(Command.Contains("Place/"))
                    {
                        string type = Command.Substring(6);
                        vox.Type = VoxelLibrary.GetVoxelType(type);
                        vox.Water = new WaterCell();
                        vox.Health = vox.Type.StartingHealth;

                        if (type == "Magic")
                        {
                            new VoxelListener(Player.World.ComponentManager, Player.World.ComponentManager.RootComponent,
                                Player.World.ChunkManager, vox)
                            {
                                DestroyOnTimer = true,
                                DestroyTimer = new Timer(5.0f + MathFunctions.Rand(-0.5f, 0.5f), true)
                            };
                        }


                        chunksToRebuild.Add(vox.ChunkID);
                    }
                    else switch(Command)
                    {
                        case "Delete Block":
                        {
                            Player.World.Master.Faction.OnVoxelDestroyed(vox);
                            vox.Chunk.NotifyDestroyed(new Point3(vox.GridPosition));
                            vox.Type = VoxelType.TypeList[0];
                            vox.Water = new WaterCell();

                            vox.Chunk.Manager.KilledVoxels.Add(vox);
                        }
                            break;
                        case "Kill Block":
                            foreach(Voxel selected in refs)
                            {

                                if (!selected.IsEmpty)
                                {
                                    selected.Kill();
                                }
                            }
                            break;
                        case "Fill Water":
                        {
                            if (vox.IsEmpty)
                            {
                                vox.WaterLevel = WaterManager.maxWaterLevel;
                                vox.Chunk.Data.Water[vox.Index].Type = LiquidType.Water;
                                chunksToRebuild.Add(vox.ChunkID);
                            }
                        }
                            break;
                        case "Fill Lava":
                        {
                            Vector3 gridPos = vox.GridPosition;
                            if (vox.IsEmpty)
                            {
                                vox.WaterLevel = WaterManager.maxWaterLevel;
                                vox.Chunk.Data.Water[vox.Index].Type = LiquidType.Lava;
                                chunksToRebuild.Add(vox.ChunkID);
                            }
                        }
                            break;
                        case "Fire":
                        {
                            List<Body> components = new List<Body>();
                            Player.Faction.Components.GetBodiesIntersecting(vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

                            foreach(Flammable flam2 in components.Select(comp => comp.GetChildrenOfTypeRecursive<Flammable>()).Where(flam => flam.Count > 0).SelectMany(flam => flam))
                            {
                                flam2.Heat = flam2.Flashpoint + 1;
                            }
                        }
                            break;
                        case "Kill Things":
                        {
                            List<Body> components = new List<Body>();
                            Player.Faction.Components.GetBodiesIntersecting(vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

                            foreach(Body comp in components)
                            {
                                comp.Die();
                            }
                        }
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach(Point3 chunk in chunksToRebuild)
            {
                Chunks.ChunkData.ChunkMap[chunk].NotifyTotalRebuild(false);
            }
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
