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

namespace DwarfCorp
{
    /// <summary>
    /// This is the debug tool that allows the player to mess with the engine.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GodModeTool : PlayerTool
    {
        [JsonIgnore]
        public DwarfGUI GUI { get; set; }

        [JsonIgnore]
        public Window SelectorPanel { get; set; }
        [JsonIgnore]
        public ComboBox SelectorBox { get; set; }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                if(value)
                {
                    orignalSelection = Player.VoxSelector.SelectionType;
                }

                Player.VoxSelector.SelectionType = GetSelectionType(value);
                SelectorPanel.IsVisible = value;

                if (!value)
                {
                    SelectorBox.Kill();
                }
            }
        }

        private bool isActive = false;
        public ChunkManager Chunks { get; set; }

        private VoxelSelectionType orignalSelection = VoxelSelectionType.SelectEmpty;

        private VoxelSelectionType GetSelectionType(bool active)
        {
            return active ? GetSelectionTypeBySelectionBoxValue(SelectorBox.CurrentValue) : orignalSelection;
        }


        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            IsActive = false;
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }


        public GodModeTool(DwarfGUI gui, GameMaster master)
        {
            GUI = gui;
            Player = master;
            
            SelectorPanel = new Window(GUI, gui.RootComponent)
            {
                LocalBounds = new Rectangle(5, 5, 300, 200)
            };

            Label label = new Label(GUI, SelectorPanel, "Cheat Mode!", GUI.DefaultFont)
            {
                LocalBounds = new Rectangle(10, 10, 250, 32)
            };

            SelectorBox = new ComboBox(GUI, SelectorPanel)
            {
                LocalBounds = new Rectangle(10, 64, 250, 32),
                WidthSizeMode = GUIComponent.SizeMode.Fit
            };

            IsActive = false;
            Chunks = Player.World.ChunkManager;


            foreach(string s in RoomLibrary.GetRoomTypes())
            {
                SelectorBox.AddValue("Build/" + s);
            }

            List<string> strings = EntityFactory.EntityFuncs.Keys.ToList();
            strings.Sort();
            foreach (string s in strings)
            {
                SelectorBox.AddValue("Spawn/" + s);
            }

            foreach(VoxelType type in VoxelLibrary.PrimitiveMap.Keys.Where(type => type.Name != "empty" && type.Name != "water"))
            {
                SelectorBox.AddValue("Place/" + type.Name);
            }


            SelectorBox.AddValue("Delete Block");
            SelectorBox.AddValue("Kill Block");
            SelectorBox.AddValue("Kill Things");
            SelectorBox.AddValue("Fill Water");
            SelectorBox.AddValue("Fill Lava");
            SelectorBox.AddValue("Fire");
            SelectorBox.AddValue("Inspect Above");
            SelectorBox.AddValue("Toggle Chunk Render");
            SelectorBox.AddValue("Force Chunk Rebuild");
            SelectorBox.AddValue("Dump Vertices & Lightmap");
            SelectorBox.AddValue("Show Voxel Coordinates");
            SelectorBox.OnSelectionModified += SelectorBox_OnSelectionModified;


            Button tradeButton = new Button(GUI, SelectorPanel, "Send Trade Envoy", GUI.DefaultFont,
                Button.ButtonMode.PushButton, null)
            {
                LocalBounds = new Rectangle(10, 128, 200, 50)
            };
            tradeButton.OnClicked += () =>
            {
                Faction toSend = null;
                foreach (var faction in Player.World.ComponentManager.Factions.Factions)
                {
                    if (faction.Value.Race.IsIntelligent && faction.Value.Race.IsNative)
                    {
                        toSend = faction.Value;
                        break;
                    }
                }
                if (toSend == null) return;
                Player.World.ComponentManager.Diplomacy.SendTradeEnvoy(toSend, Player.World);
            };

            SelectorPanel.IsVisible = false;
        }

        public override void Destroy()
        {
            SelectorBox.OnSelectionModified -= SelectorBox_OnSelectionModified;
            SelectorBox.CleanUp();
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
            if(!IsActive)
            {
                return;
            }

            string command = SelectorBox.CurrentValue;
            if(command == "")
            {
                return;
            }

            HashSet<Point3> chunksToRebuild = new HashSet<Point3>();

            if(command.Contains("Build/"))
            {
                string type = command.Substring(6);
                BuildRoomOrder des = new BuildRoomOrder(RoomLibrary.CreateRoom(Player.Faction, type, refs, false, Player.World), Player.Faction, Player.World);
                Player.Faction.RoomBuilder.BuildDesignations.Add(des);
                Player.Faction.RoomBuilder.DesignatedRooms.Add(des.ToBuild);
                des.Build();
            }
            else if (command.Contains("Spawn/"))
            {
                string type = command.Substring(6);
                foreach (Voxel vox in refs.Where(vox => vox != null))
                {
                    if (vox.IsEmpty)
                    {
                        EntityFactory.CreateEntity<Body>(type, vox.Position + new Vector3(0.5f, 0.5f, 0.5f));
                    }
                }
            } else if (command.Contains("Inspect"))
            {
                int waterTotal = 0;
                foreach (Voxel vox in refs.Where(vox => vox != null))
                {
                    waterTotal += vox.WaterLevel;
                }
                GamePerformance.Instance.TrackValueType("waterLevel", waterTotal);
            }
            else if (command.Contains("Toggle"))
            {
                foreach(Voxel vox in refs)
                {
                    if (vox == null) continue;
                    vox.Chunk.noRender = !vox.Chunk.noRender;
                    break;
                }
            }
            else if (command.Contains("Force"))
            {
                foreach(Voxel vox in refs)
                {
                    if (vox == null) continue;
                    vox.Chunk.NotifyTotalRebuild(false);
                    break;
                }
            }
            else if (command.Contains("Dump"))
            {
                foreach (Voxel vox in refs)
                {
                    if (vox == null) continue;
                    FileUtils.SaveJSon(vox.Chunk.Primitive, "VertexDump.txt", false);
                    using (System.IO.FileStream stream = new System.IO.FileStream("Lightmap.png", System.IO.FileMode.Create))
                    {
                        RenderTarget2D lm = vox.Chunk.Primitive.Lightmap;
                        lm.SaveAsPng(stream, lm.Width, lm.Height);
                    }
                    break;
                }
            }
            else if (command.Contains("Show Voxel"))
            {
                foreach(Voxel vox in refs)
                {
                    if (vox == null) continue;
                    GamePerformance.Instance.TrackReferenceType("Voxel Coord", vox);
                    break;
                }
            }
            else
            {
                foreach(Voxel vox in refs.Where(vox => vox != null))
                {
                    if(command.Contains("Place/"))
                    {
                        string type = command.Substring(6);
                        vox.Place(type);

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
                    else switch(command)
                    {
                        case "Delete Block":
                        {
                            vox.Destroy();
                        }
                        break;
                        case "Kill Block":
                            if (!vox.IsEmpty) vox.Kill();
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
