using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Handles the player's controls, tools, and factions.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GameMaster
    {


        public enum ToolMode
        {
            SelectUnits,
            Dig,
            Build,
            Cook,
            Magic,
            Gather,
            Chop,
            Guard,
            Attack,
            Farm,
            Craft,
            God
        }


        public Camera CameraController { get; set; }

        [JsonIgnore]
        public DwarfGUI GUI { get; set; }

        [JsonIgnore]
        public VoxelSelector VoxSelector { get; set; }

        [JsonIgnore]
        public BodySelector BodySelector { get; set; }

        [JsonIgnore]
        public AIDebugger Debugger { get; set; }

        public Faction Faction { get; set; }

        #region  Player tool management

        [JsonIgnore]
        public Dictionary<ToolMode, PlayerTool> Tools { get; set; }

        [JsonIgnore]
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }

        public ToolMode CurrentToolMode { get; set; }

        public void ChangeTool(ToolMode NewTool)
        {
            Tools[NewTool].OnBegin();
            if (CurrentToolMode != NewTool)
                CurrentTool.OnEnd();
            CurrentToolMode = NewTool;
        }

        #endregion


        [JsonIgnore]
        public List<CreatureAI> SelectedMinions { get { return Faction.SelectedMinions; }set { Faction.SelectedMinions = value; } }

        [JsonIgnore]
        public SpellTree Spells { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        protected void OnDeserialized(StreamingContext context)
        {
            World = (WorldManager) (context.Context);
            Initialize(GameState.Game, World.ComponentManager, World.ChunkManager, World.Camera, World.ChunkManager.Graphics, World.GUI);
            World.Master = this;
        }

        public GameMaster()
        {
        }

        public GameMaster(Faction faction, DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, DwarfGUI gui)
        {
            World = components.World;
            Faction = faction;
            Initialize(game, components, chunks, camera, graphics, gui);
            VoxSelector.Selected += OnSelected;
            VoxSelector.Dragged += OnDrag;
            BodySelector.Selected += OnBodiesSelected;
            World.Master = this;
            World.Time.NewDay += Time_NewDay;
        }

        public void Initialize(DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, DwarfGUI gui)
        {
            RoomLibrary.InitializeStatics();

            CameraController = camera;
            VoxSelector = new VoxelSelector(World, CameraController, chunks.Graphics, chunks);
            BodySelector = new BodySelector(CameraController, chunks.Graphics, components);
            GUI = gui;
            SelectedMinions = new List<CreatureAI>();
            Spells = SpellLibrary.CreateSpellTree(components.World);
            CreateTools();

            InputManager.KeyReleasedCallback += OnKeyReleased;

            Debugger = new AIDebugger(GUI, this);

        }

        public void Destroy()
        {
            VoxSelector.Selected -= OnSelected;
            VoxSelector.Dragged -= OnDrag;
            BodySelector.Selected -= OnBodiesSelected;
            World.Time.NewDay -= Time_NewDay;
            InputManager.KeyReleasedCallback -= OnKeyReleased;
            Tools[ToolMode.God].Destroy();
            Tools[ToolMode.SelectUnits].Destroy();
            Tools.Clear();
            Debugger.Destroy();
            Debugger = null;
            Faction = null;
            VoxSelector = null;
            BodySelector = null;
        }

        private void CreateTools()
        {
            Tools = new Dictionary<ToolMode, PlayerTool>();
            Tools[ToolMode.God] = new GodModeTool(GUI, this);

            Tools[ToolMode.SelectUnits] = new DwarfSelectorTool(this);

            Tools[ToolMode.Farm] = new FarmTool()
            {
                Player = this
            };

            
            Tools[ToolMode.Dig] = new DigTool
            {
                Player = this,
                DigDesignationGlowRate = 2.0f,
                UnreachableColor = new Color(205, 10, 10),
                DigDesignationColor = new Color(205, 200, 10)
            };
             

            Tools[ToolMode.Gather] = new GatherTool
            {
                Player = this,
                GatherDesignationColor = Color.Goldenrod,
                GatherDesignationGlowRate = 2.0f
            };

            Tools[ToolMode.Guard] = new GuardTool
            {
                Player = this,
                GuardDesignationColor = new Color(10, 10, 205),
                GuardDesignationGlowRate = 2.0f,
                UnreachableColor = new Color(205, 10, 10)
            };

            Tools[ToolMode.Chop] = new ChopTool
            {
                Player = this,
                ChopDesignationColor = Color.LightGreen,
                ChopDesignationGlowRate = 2.0f
            };

            Tools[ToolMode.Attack] = new AttackTool
            {
                Player = this,
                DesignationColor = Color.Red,
                GlowRate = 2.0f
            };

            Tools[ToolMode.Build] = new BuildTool
            {
                Player = this,
                BuildType = NewGui.BuildMenu.BuildTypes.AllButCook,
            };

            Tools[ToolMode.Magic] = new MagicTool(this);

            Tools[ToolMode.Cook] = new BuildTool
            {
                Player = this,
                BuildType = NewGui.BuildMenu.BuildTypes.Cook,
            };
        }

        void Time_NewDay(DateTime time)
        {
            PayEmployees();
        }

        public void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            CurrentTool.OnBodiesSelected(bodies, button);
        }

        public void OnDrag(List<Voxel> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsDragged(voxels, button);
        }

        public void OnSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsSelected(voxels, button);
        }

        public bool AreAllEmployeesAsleep()
        {
            return (Faction.Minions.Count > 0) && Faction.Minions.All(minion => (!minion.Stats.CanSleep || minion.Creature.IsAsleep) && !minion.IsDead);
        }

        public void PayEmployees()
        {
            DwarfBux total = 0;
            bool noMoney = false;
            foreach (CreatureAI creature in Faction.Minions)
            {
                if (creature.Stats.IsOverQualified)
                {
                    creature.AddThought(Thought.ThoughtType.IsOverQualified);    
                }

                if (!noMoney)
                {
                    DwarfBux pay = creature.Stats.CurrentLevel.Pay;
                    total += pay;
                    Faction.Economy.CurrentMoney = Math.Max(Faction.Economy.CurrentMoney - pay, 0m);
                    creature.AddMoney(pay);
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.NotPaid);
                }

                if (!(Faction.Economy.CurrentMoney > 0m))
                {
                    if (!noMoney)
                    {
                        World.MakeAnnouncement("We're bankrupt!",
                            "If we don't make a profit by tomorrow, our stock will crash!");
                    }
                    noMoney = true;
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.GotPaid);
                }
            }

            World.MakeAnnouncement("Pay day!", String.Format("We paid our employees {0} today.",
                total), null, ContentPaths.Audio.change);
        }


        public void Render(DwarfGame game, DwarfTime time, GraphicsDevice g)
        {
            CurrentTool.Render(game, g, time);
            VoxSelector.Render();

            foreach (CreatureAI creature in Faction.SelectedMinions)
            {
                //Drawer2D.DrawZAlignedRect(creature.Position + Vector3.Down * 0.5f, 0.25f, 0.25f, 2, new Color(255, 255, 255, 50));
                creature.Creature.SelectionCircle.IsVisible = true;
                creature.Creature.Sprite.DrawSilhouette = true;
                foreach(Task task in creature.Tasks)
                {
                    if (task.IsFeasible(creature.Creature))
                        task.Render(time);
                }

                if(creature.CurrentTask != null)
                {
                    creature.CurrentTask.Render(time);
                }
            
            }


            DwarfGame.SpriteBatch.Begin();
            BodySelector.Render(DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.End();
        }

        public void UpdateRooms()
        {
            bool hasAnyMinions = SelectedMinions.Count > 0;


            foreach (Room room in Faction.GetRooms())
            {
                if (room.wasDeserialized)
                {
                    room.CreateGUIObjects();
                    room.wasDeserialized = false;
                }
                if (room.GUIObject != null && hasAnyMinions)
                {
                    room.GUIObject.IsVisible = true;
                    room.GUIObject.Enabled = true;
                }
                else if (!hasAnyMinions && room.GUIObject != null)
                {
                    room.GUIObject.IsVisible = false;
                    room.GUIObject.GUIObject.IsVisible = false;
                    room.GUIObject.Enabled = false;
                }
            }
        }

        public void Update(DwarfGame game, DwarfTime time)
        {
            //if (CurrentToolMode != ToolMode.God)
            //{
            //    CurrentToolMode = ToolBar.CurrentMode;
            //}

            CurrentTool.Update(game, time);
            if(GameSettings.Default.EnableAIDebugger)
            {
                if(Debugger != null)
                {
                    Debugger.Update(time);
                }
            }

            if (!World.Paused)
            {

            }
            else
            {
                CameraController.LastWheel = Mouse.GetState().ScrollWheelValue;
            }
            UpdateInput(game, time);

            if (Faction.Minions.Any(m => m.IsDead && m.TriggersMourning))
            {
                CreatureAI deadMinion = null;
                foreach (CreatureAI minion in Faction.Minions)
                {
                    minion.AddThought(Thought.ThoughtType.FriendDied);

                    if (minion.IsDead)
                    {
                        deadMinion = minion;
                    }
                }

                if (deadMinion != null)
                {
                    World.MakeAnnouncement(
                        String.Format("{0} ({1}) died!", deadMinion.Stats.FullName, deadMinion.Stats.CurrentLevel.Name),
                        "One of our employees has died!");
                    Faction.Economy.Company.StockPrice -= MathFunctions.Rand(0, 0.5f);
                }
            }

            Faction.Minions.RemoveAll(m => m.IsDead);

            UpdateRooms();

            Faction.CraftBuilder.Update(time, this);
        }


        #region input


        public bool IsCameraRotationModeActive()
        {
            KeyboardState keyState = Keyboard.GetState();
            return KeyManager.RotationEnabled();

        }


        public void UpdateMouse(MouseState mouseState, KeyboardState keyState, DwarfGame game, DwarfTime time)
        {
            if(KeyManager.RotationEnabled())
            {
                World.SetMouse(null);
            }
          
        }

        public void UpdateInput(DwarfGame game, DwarfTime time)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();


            if(!IsMouseOverGui())
            {
                UpdateMouse(Mouse.GetState(), Keyboard.GetState(), game, time);
                VoxSelector.Update();
                BodySelector.Update();
            }

        }

        public void OnKeyPressed()
        {
        }

        public void OnKeyReleased(Keys key)
        {
            if(key == ControlSettings.Mappings.SliceUp)
            {
                World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
            }

            else if(key == ControlSettings.Mappings.SliceDown)
            {
                World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
            }
            else if (key == ControlSettings.Mappings.SliceSelected)
            {
                if (VoxSelector.VoxelUnderMouse != null)
                {
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(VoxSelector.VoxelUnderMouse.Position.Y,
                        ChunkManager.SliceMode.Y);
                    Drawer3D.DrawBox(VoxSelector.VoxelUnderMouse.GetBoundingBox(), Color.White, 0.15f, true);
                }
            }
            else if (key == ControlSettings.Mappings.Unslice)
            {
                World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkHeight, ChunkManager.SliceMode.Y);
            }
            //else if(key == ControlSettings.Mappings.GodMode)
            //{
            //    if(CurrentToolMode == ToolMode.God)
            //    {
            //        CurrentToolMode = ToolMode.SelectUnits;
            //        GodModeTool godMode = (GodModeTool) Tools[ToolMode.God];
            //        godMode.IsActive = false;
            //    }
            //    else
            //    {
            //        CurrentToolMode = ToolMode.God;
            //        GodModeTool godMode = (GodModeTool)Tools[ToolMode.God];
            //        godMode.IsActive = true;
            //    }
            //}
        }

        // Todo: Delete this.
        public bool IsMouseOverGui()
        {
            return World.IsMouseOverGui;
            //return GUI.IsMouseOver() || (GUI.FocusComponent != null);
        }

        #endregion
        

    }

}
