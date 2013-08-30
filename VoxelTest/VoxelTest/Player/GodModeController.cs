using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class GodModeController
    {
        public SillyGUI GUI { get; set; }
        public GameMaster Master { get; set; }
        public Panel SelectorPanel { get; set; }
        public ComboBox SelectorBox { get; set; }
        public bool IsActive { get { return m_active; }
            set
            {
                m_active = value; if (value) { orignalSelection = Master.VoxSelector.SelectionType; }
                
                Master.VoxSelector.SelectionType = GetSelectionType(value); SelectorPanel.IsVisible = value; }
        }
        private bool m_active = false;
        public ChunkManager Chunks { get; set;}

        VoxelSelectionType orignalSelection = VoxelSelectionType.SelectEmpty;

        private VoxelSelectionType GetSelectionType(bool active)
        {
            if (active)
            {
                return VoxelSelectionType.SelectEmpty;
            }
            else
            {
                return orignalSelection;
                    
            }
        }

        public GodModeController(SillyGUI gui, GameMaster master)
        {
            GUI = gui;
            Master = master;
            SelectorPanel = new Panel(GUI, gui.RootComponent);
            SelectorPanel.LocalBounds = new Rectangle(32, 180, 300, 100);
            SelectorBox = new ComboBox(GUI, SelectorPanel);
            SelectorBox.LocalBounds = new Rectangle(10, 10, 250, 32);
            IsActive = false;
            Chunks = Master.Chunks;


            foreach (string s in RoomLibrary.GetRoomTypes())
            {
                SelectorBox.AddValue("Build " + s);
            }

            foreach (string s in EntityFactory.ComponentList)
            {
                SelectorBox.AddValue(s);
            }

            foreach (VoxelType type in VoxelLibrary.PrimitiveMap.Keys)
            {
                if (type.name != "empty" && type.name != "water")
                {
                    SelectorBox.AddValue("Place " + type.name);
                }
            }



            SelectorBox.AddValue("Delete Block");
            SelectorBox.AddValue("Kill Block");
            SelectorBox.AddValue("Kill Things");
            SelectorBox.AddValue("Fill Water");
            SelectorBox.AddValue("Fill Lava");
            SelectorBox.AddValue("Fire");
            SelectorBox.OnSelectionModified += new ComboBoxSelector.Modified(SelectorBox_OnSelectionModified);

            Master.VoxSelector.Selected += OnSelected;

            SelectorPanel.IsVisible = false;
        }

        void SelectorBox_OnSelectionModified(string arg)
        {
            if (arg == "Delete Block" || arg.Contains("Build") || arg == "Kill Block")
            {
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            }
            else
            {
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            }
        }

        public void OnSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if (IsActive)
            {
                string command = SelectorBox.CurrentValue;
                if(command == "")
                {
                    return;
                }

                HashSet<Point3> ChunksToRebuild = new HashSet<Point3>();

                if (command.Contains("Build "))
                {
                    string type = command.Substring(6);
                    RoomBuildDesignation des = new RoomBuildDesignation(new Room(refs, RoomLibrary.GetType(type), Chunks), Master);
                    Master.RoomDesignator.BuildDesignations.Add(des);
                    Master.RoomDesignator.DesignatedRooms.Add(des.ToBuild);
                    des.Build();

                }
                else foreach (VoxelRef vox in refs)
                    {

                        if (command.Contains("Place "))
                        {
                            string type = command.Substring(6);

                            Vector3 gridPos = vox.GridPosition;


                            Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z] = new Voxel(vox.WorldPosition, VoxelLibrary.GetVoxelType(type),
                                                                                                                                        VoxelLibrary.GetPrimitive(type), true);
                            Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].Chunk = Chunks.ChunkMap[vox.ChunkID];
                            Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].WaterLevel = 0;
                            ChunksToRebuild.Add(vox.ChunkID);


                        }
                        else if (command == "Delete Block")
                        {
                            Vector3 gridPos = vox.GridPosition;

                            Voxel v = Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z];

                            PlayState.master.OnVoxelDestroyed(v);

                            Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z] = null;
                            Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].WaterLevel = 0;

                            if (!ChunksToRebuild.Contains(vox.ChunkID))
                            {
                                Chunks.ChunkMap[vox.ChunkID].NotifyTotalRebuild(v != null && !v.IsInterior);
                            }
                            ChunksToRebuild.Add(vox.ChunkID);


                        }
                        else if (command == "Kill Block")
                        {
                            foreach (VoxelRef selected in refs)
                            {
                                Vector3 gridPos = selected.GridPosition;

                                Voxel v = Chunks.ChunkMap[selected.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z];

                                if (v != null)
                                {
                                    v.Kill();
                                }
                            }
                        }
                        else if (command == "Fill Water")
                        {

                            Vector3 gridPos = vox.GridPosition;
                            if (Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z] == null)
                            {
                                Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].WaterLevel = 255;
                                Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].Type = LiquidType.Water;
                                ChunksToRebuild.Add(vox.ChunkID);
                            }

                        }
                        else if (command == "Fill Lava")
                        {

                            Vector3 gridPos = vox.GridPosition;
                            if (Chunks.ChunkMap[vox.ChunkID].VoxelGrid[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z] == null)
                            {
                                Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].WaterLevel = 255;
                                Chunks.ChunkMap[vox.ChunkID].Water[(int)gridPos.X][(int)gridPos.Y][(int)gridPos.Z].Type = LiquidType.Lava;
                                ChunksToRebuild.Add(vox.ChunkID);
                            }

                        }
                        else if (command == "Fire")
                        {
                            List<LocatableComponent> components = new List<LocatableComponent>();
                            Master.Components.GetComponentsIntersecting(vox.GetBoundingBox(), components);

                            foreach (LocatableComponent comp in components)
                            {
                                List<FlammableComponent> flam = comp.GetChildrendOfTypeRecursive<FlammableComponent>();

                                if (flam.Count > 0)
                                {
                                    foreach (FlammableComponent flam2 in flam)
                                    {
                                        flam2.Heat = flam2.Flashpoint + 1;
                                    }
                                }
                            }
                        }
                        else if (command == "Kill Things")
                        {
                             List<LocatableComponent> components = new List<LocatableComponent>();
                            Master.Components.GetComponentsIntersecting(vox.GetBoundingBox(), components);

                            foreach (LocatableComponent comp in components)
                            {
                                comp.Die();
                            }
                        }
                        else
                        {
                            if (vox.TypeName == "empty")
                            {
                                EntityFactory.GenerateComponent(SelectorBox.CurrentValue, vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f),
                                    Master.Components, Master.Content, Master.Graphics, Master.Chunks, Master, Master.CameraController);
                            }
                        }
                    }

                foreach (Point3 chunk in ChunksToRebuild)
                {
                    Chunks.ChunkMap[chunk].NotifyTotalRebuild(false);
                }

            }
        }


       
    }

}
