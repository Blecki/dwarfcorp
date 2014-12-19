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
            }
        }

        private bool isActive = false;
        public ChunkManager Chunks { get; set; }

        private VoxelSelectionType orignalSelection = VoxelSelectionType.SelectEmpty;

        private VoxelSelectionType GetSelectionType(bool active)
        {
            return active ? VoxelSelectionType.SelectEmpty : orignalSelection;
        }


        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public GodModeTool(DwarfGUI gui, GameMaster master)
        {
            GUI = gui;
            Player = master;
            
            SelectorPanel = new Window(GUI, gui.RootComponent)
            {
                LocalBounds = new Rectangle(80, 140, 300, 100)
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

            if(command.Contains("Build "))
            {
                string type = command.Substring(6);
                BuildRoomOrder des = new BuildRoomOrder(RoomLibrary.CreateRoom(type, refs, false), Player.Faction);
                Player.Faction.RoomBuilder.BuildDesignations.Add(des);
                Player.Faction.RoomBuilder.DesignatedRooms.Add(des.ToBuild);
                des.Build();

                if (type == Stockpile.StockpileName)
                {
                    Player.Faction.Stockpiles.Add((Stockpile)des.ToBuild);
                }
            }
            else
            {
                foreach(Voxel vox in refs.Where(vox => vox != null))
                {
                    if(command.Contains("Place "))
                    {
                        string type = command.Substring(6);
                        vox.Type = VoxelLibrary.GetVoxelType(type);
                        vox.IsVisible = true;
                        vox.Water = new WaterCell();
                        vox.Health = vox.Type.StartingHealth;

                        if (type == "Magic")
                        {
                            new VoxelListener(PlayState.ComponentManager, PlayState.ComponentManager.RootComponent,
                                PlayState.ChunkManager, vox)
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
                            PlayState.Master.Faction.OnVoxelDestroyed(vox);
                            vox.Chunk.NotifyDestroyed(new Point3(vox.GridPosition));
                            vox.Type = VoxelType.TypeList[0];
                            vox.Water = new WaterCell();

                            if(!chunksToRebuild.Contains(vox.ChunkID))
                            {
                                Chunks.ChunkData.ChunkMap[vox.ChunkID].NotifyTotalRebuild(vox.IsEmpty && !vox.IsInterior);
                            }
                            chunksToRebuild.Add(vox.ChunkID);
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
                                vox.WaterLevel = 255;
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
                                vox.WaterLevel = 255;
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
                            if(vox.IsEmpty)
                            {
                                EntityFactory.GenerateComponent(SelectorBox.CurrentValue, vox.Position + new Vector3(0.5f, 0.5f, 0.5f),
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
                PlayState.GUI.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            PlayState.GUI.IsMouseVisible = true;

            PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;

        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
          
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }

}