﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using DwarfCorpCore;
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

        public ToolMode CurrentToolMode { get; set; }


        [JsonIgnore]
        public DwarfGUI GUI { get; set; }

        [JsonIgnore]
        public MasterControls ToolBar { get; set; }

        [JsonIgnore]
        public VoxelSelector VoxSelector { get; set; }

        [JsonIgnore]
        public BodySelector BodySelector { get; set; }

        [JsonIgnore]
        public AIDebugger Debugger { get; set; }

        public Faction Faction { get; set; }

        [JsonIgnore]
        public Dictionary<ToolMode, PlayerTool> Tools { get; set; }

        [JsonIgnore]
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }
            
        [JsonIgnore]
        public List<CreatureAI> SelectedMinions { get { return Faction.SelectedMinions; }set { Faction.SelectedMinions = value; } }

        [JsonIgnore]
        public SpellTree Spells { get; set; }

        protected void OnDeserialized(StreamingContext context)
        {
            Initialize(GameState.Game, PlayState.ComponentManager, PlayState.ChunkManager, PlayState.Camera, PlayState.ChunkManager.Graphics,  PlayState.GUI);
            PlayState.Master = this;
        }

        public GameMaster()
        {
        }


        public void Initialize(DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, DwarfGUI gui)
        {
            RoomLibrary.InitializeStatics();

            CameraController = camera;
            VoxSelector = new VoxelSelector(CameraController, chunks.Graphics, chunks);
            BodySelector = new BodySelector(CameraController, chunks.Graphics, components);
            GUI = gui;
            SelectedMinions = new List<CreatureAI>();
            Spells = SpellLibrary.CreateSpellTree();
            CreateTools();

            InputManager.KeyReleasedCallback += OnKeyReleased;
            ToolBar = new MasterControls(GUI, GUI.RootComponent, this, TextureManager.GetTexture(ContentPaths.GUI.icons), graphics, game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default))
            {
                Master = this
            };

            Debugger = new AIDebugger(GUI, this);

        }

        private void CreateTools()
        {
            Tools = new Dictionary<ToolMode, PlayerTool>();
            Tools[ToolMode.God] = new GodModeTool(GUI, this);

            Tools[ToolMode.SelectUnits] = new DwarfSelectorTool(this);

            
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
                Player = this
            };

            Tools[ToolMode.Magic] = new MagicTool(this);
        }

        public GameMaster(Faction faction, DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, DwarfGUI gui)
        {
            Faction = faction;
            Initialize(game, components, chunks, camera, graphics, gui);
            VoxSelector.Selected += OnSelected;
            BodySelector.Selected += OnBodiesSelected;
            PlayState.Time.NewDay += Time_NewDay;
        }

        void Time_NewDay(DateTime time)
        {
            PayEmployees();
        }

        public void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            CurrentTool.OnBodiesSelected(bodies, button);
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
            float total = 0;
            bool noMoney = false;
            foreach (CreatureAI creature in Faction.Minions)
            {
                if (creature.Stats.IsOverQualified)
                {
                    creature.AddThought(Thought.ThoughtType.IsOverQualified);    
                }

                if (!noMoney)
                {
                    float pay = creature.Stats.CurrentLevel.Pay;
                    total += pay;
                    Faction.Economy.CurrentMoney = Math.Max(Faction.Economy.CurrentMoney - pay, 0);
                    creature.AddMoney(pay);
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.NotPaid);
                }

                if (!(Faction.Economy.CurrentMoney > 0))
                {
                    if (!noMoney)
                    {
                        PlayState.AnnouncementManager.Announce("We're bankrupt!",
                            "If we don't make a profit by tomorrow, our stock will crash!");
                    }
                    noMoney = true;
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.GotPaid);
                }
            }

            SoundManager.PlaySound(ContentPaths.Audio.change);
            PlayState.AnnouncementManager.Announce("Pay day!", "We paid our employees " + total.ToString("C") + " today.");
        }


        public void Render(DwarfGame game, DwarfTime time, GraphicsDevice g)
        {
            CurrentTool.Render(game, g, time);
            VoxSelector.Render();

            foreach (CreatureAI creature in Faction.SelectedMinions)
            {
                //Drawer2D.DrawZAlignedRect(creature.Position + Vector3.Down * 0.5f, 0.25f, 0.25f, 2, new Color(255, 255, 255, 50));
                creature.Creature.SelectionCircle.IsVisible = true;
                foreach(Task task in creature.Tasks)
                {
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
            if(CurrentToolMode != ToolMode.God)
            {
                CurrentToolMode = ToolBar.CurrentMode;
            }

            CurrentTool.Update(game, time);
            if(GameSettings.Default.EnableAIDebugger)
            {
                if(Debugger != null)
                {
                    Debugger.Update(time);
                }
            }

            if (!PlayState.Paused)
            {

            }
            else
            {
                CameraController.LastWheel = Mouse.GetState().ScrollWheelValue;
            }
            UpdateInput(game, time);

            if (Faction.Minions.Any(m => m.IsDead && m.TriggersMourning))
            {
                foreach (CreatureAI minion in Faction.Minions)
                {
                    minion.AddThought(Thought.ThoughtType.FriendDied);
                }

                PlayState.AnnouncementManager.Announce("An employee died!", "One of our employees has died!");
                Faction.Economy.Company.StockPrice -= MathFunctions.Rand(0, 0.5f);
            }

            UpdateRooms();
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
                PlayState.GUI.IsMouseVisible = false;
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
                PlayState.ChunkManager.ChunkData.SetMaxViewingLevel(PlayState.ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
            }

            if(key == ControlSettings.Mappings.SliceDown)
            {
                PlayState.ChunkManager.ChunkData.SetMaxViewingLevel(PlayState.ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
            }


            if(key == ControlSettings.Mappings.GodMode)
            {
                if(CurrentToolMode == ToolMode.God)
                {
                    CurrentToolMode = ToolBar.CurrentMode;
                    GodModeTool godMode = (GodModeTool) Tools[ToolMode.God];
                    godMode.IsActive = false;
                }
                else
                {
                    CurrentToolMode = ToolMode.God;
                    GodModeTool godMode = (GodModeTool)Tools[ToolMode.God];
                    godMode.IsActive = true;
                }
            }
        }

        public bool IsMouseOverGui()
        {
            return GUI.IsMouseOver() || (GUI.FocusComponent != null);
        }

        #endregion
        

    }

}