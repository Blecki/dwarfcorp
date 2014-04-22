using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
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
        public Panel SelectorPanel { get; set; }
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
            }
        }

        private bool isActive = false;
        public ChunkManager Chunks { get; set; }

        private VoxelSelectionType orignalSelection = VoxelSelectionType.SelectEmpty;

        private VoxelSelectionType GetSelectionType(bool active)
        {
            return active ? VoxelSelectionType.SelectEmpty : orignalSelection;
        }

        public GodModeTool(DwarfGUI gui, GameMaster master)
        {
            GUI = gui;
            Player = master;
            
            SelectorPanel = new Panel(GUI, gui.RootComponent)
            {
                LocalBounds = new Rectangle(80, 140, 300, 100)
            };

            SelectorBox = new ComboBox(GUI, SelectorPanel)
            {
                LocalBounds = new Rectangle(10, 10, 250, 32)
            };

            IsActive = false;
            Chunks = PlayState.ChunkManager;


            foreach(string s in RoomLibrary.GetRoomTypes())
            {
                SelectorBox.AddValue("Build " + s);
            }

            foreach(string s in EntityFactory.ComponentList)
            {
                SelectorBox.AddValue(s);
            }

            foreach(VoxelType type in VoxelLibrary.PrimitiveMap.Keys.Where(type => type.Name != "empty" && type.Name != "water"))
            {
                SelectorBox.AddValue("Place " + type.Name);
            }


            SelectorBox.AddValue("Delete Block");
            SelectorBox.AddValue("Kill Block");
            SelectorBox.AddValue("Kill Things");
            SelectorBox.AddValue("Fill Water");
            SelectorBox.AddValue("Fill Lava");
            SelectorBox.AddValue("Fire");
            SelectorBox.OnSelectionModified += SelectorBox_OnSelectionModified;


            SelectorPanel.IsVisible = false;
        }

        private void SelectorBox_OnSelectionModified(string arg)
        {
            if(arg == "Delete Block" || arg.Contains("Build") || arg == "Kill Block")
            {
                Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            }
            else
            {
                Player.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            }
        }

        public override void OnVoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
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

            if(command.Contains("Build "))
            {
                string type = command.Substring(6);
                RoomBuildDesignation des = new RoomBuildDesignation(new Room(refs, RoomLibrary.GetType(type), Chunks), Player.Faction);
                Player.Faction.RoomDesignator.BuildDesignations.Add(des);
                Player.Faction.RoomDesignator.DesignatedRooms.Add(des.ToBuild);
                des.Build();
            }
            else
            {
                foreach(VoxelRef vox in refs.Where(vox => vox != null))
                {
                    if(command.Contains("Place "))
                    {
                        string type = command.Substring(6);

                        Vector3 gridPos = vox.GridPosition;


                        Chunks.ChunkData.ChunkMap[vox.ChunkID].VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z] = new Voxel(vox.WorldPosition, VoxelLibrary.GetVoxelType(type), VoxelLibrary.GetPrimitive(type), true)
                        {
                            Chunk = Chunks.ChunkData.ChunkMap[vox.ChunkID]
                        };

                        Chunks.ChunkData.ChunkMap[vox.ChunkID].Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].WaterLevel = 0;
                        chunksToRebuild.Add(vox.ChunkID);
                    }
                    else switch(command)
                    {
                        case "Delete Block":
                        {
                            Vector3 gridPos = vox.GridPosition;
                            VoxelChunk chunk = Chunks.ChunkData.ChunkMap[vox.ChunkID];
                            Voxel v = chunk.VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z];

                            PlayState.Master.Faction.OnVoxelDestroyed(v);
                            chunk.NotifyDestroyed(new Point3(gridPos));
                            chunk.VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z] = null;
                            chunk.Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].WaterLevel = 0;

                            if(!chunksToRebuild.Contains(vox.ChunkID))
                            {
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].NotifyTotalRebuild(v != null && !v.IsInterior);
                            }
                            chunksToRebuild.Add(vox.ChunkID);
                        }
                            break;
                        case "Kill Block":
                            foreach(VoxelRef selected in refs)
                            {
                                Vector3 gridPos = selected.GridPosition;

                                Voxel v = Chunks.ChunkData.ChunkMap[selected.ChunkID].VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z];

                                if(v != null)
                                {
                                    v.Kill();
                                }
                            }
                            break;
                        case "Fill Water":
                        {
                            Vector3 gridPos = vox.GridPosition;
                            if(Chunks.ChunkData.ChunkMap[vox.ChunkID].VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z] == null)
                            {
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].WaterLevel = 255;
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].Type = LiquidType.Water;
                                chunksToRebuild.Add(vox.ChunkID);
                            }
                        }
                            break;
                        case "Fill Lava":
                        {
                            Vector3 gridPos = vox.GridPosition;
                            if(Chunks.ChunkData.ChunkMap[vox.ChunkID].VoxelGrid[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z] == null)
                            {
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].WaterLevel = 255;
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z].Type = LiquidType.Lava;
                                chunksToRebuild.Add(vox.ChunkID);
                            }
                        }
                            break;
                        case "Fire":
                        {
                            List<LocatableComponent> components = new List<LocatableComponent>();
                            Player.Faction.Components.GetComponentsIntersecting(vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

                            foreach(FlammableComponent flam2 in components.Select(comp => comp.GetChildrenOfTypeRecursive<FlammableComponent>()).Where(flam => flam.Count > 0).SelectMany(flam => flam))
                            {
                                flam2.Heat = flam2.Flashpoint + 1;
                            }
                        }
                            break;
                        case "Kill Things":
                        {
                            List<LocatableComponent> components = new List<LocatableComponent>();
                            Player.Faction.Components.GetComponentsIntersecting(vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

                            foreach(LocatableComponent comp in components)
                            {
                                comp.Die();
                            }
                        }
                            break;
                        default:
                            if(vox.TypeName == "empty")
                            {
                                EntityFactory.GenerateComponent(SelectorBox.CurrentValue, vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f),
                                    PlayState.ChunkManager.Components, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, PlayState.ComponentManager.Factions, Player.CameraController);
                            }
                            break;
                    }
                }
            }

            foreach(Point3 chunk in chunksToRebuild)
            {
                Chunks.ChunkData.ChunkMap[chunk].NotifyTotalRebuild(false);
            }
        }


        public override void Update(DwarfGame game, GameTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                game.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            game.IsMouseVisible = true;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
          
        }
    }

}