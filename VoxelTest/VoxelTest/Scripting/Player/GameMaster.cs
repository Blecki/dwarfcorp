using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            Dig,
            Build,
            SelectUnits,
            Chop,
            Guard,
            CreateStockpiles,
            Gather,
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
        public AIDebugger Debugger { get; set; }

        public Faction Faction { get; set; }

        [JsonIgnore]
        public Dictionary<ToolMode, PlayerTool> Tools { get; set; }

        [JsonIgnore]
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }
            
        [OnDeserialized]
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

            Faction.Components = components;
            CameraController = camera;
            VoxSelector = new VoxelSelector(CameraController, chunks.Graphics, chunks);
            GUI = gui;
            
            CreateTools();

            InputManager.KeyReleasedCallback += OnKeyReleased;
            ToolBar = new MasterControls(GUI, GUI.RootComponent, this, TextureManager.GetTexture("IconSheet"), graphics, game.Content.Load<SpriteFont>(Program.CreatePath("Fonts", "Default")))
            {
                Master = this
            };

            Debugger = new AIDebugger(GUI, this);

            
        }

        private void CreateTools()
        {
            Tools = new Dictionary<ToolMode, PlayerTool>();
            Tools[ToolMode.God] = new GodModeTool(GUI, this);

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


            Tools[ToolMode.CreateStockpiles] = new StockpileTool
            {
                Player = this,
                DrawColor = Color.LightGoldenrodYellow,
                GlowRate = 2.0f,
            };

            Tools[ToolMode.Build] = new BuildTool
            {
                Player = this
            };
        }

        public GameMaster(Faction faction, DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, DwarfGUI gui)
        {
            Faction = faction;
            Initialize(game, components, chunks, camera, graphics, gui);
            VoxSelector.Selected += OnSelected;
        }

        public void OnSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsSelected(voxels, button);
        }


        public void Render(DwarfGame game, GameTime time, GraphicsDevice g)
        {
            CurrentTool.Render(game, g, time);
            VoxSelector.Render();
        }

        public void Update(DwarfGame game, GameTime time)
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
            CameraController.Update(time, PlayState.ChunkManager);
            UpdateInput(game, time);
        }

        #region input


        public bool IsCameraRotationModeActive()
        {
            KeyboardState keyState = Keyboard.GetState();
            return keyState.IsKeyDown(ControlSettings.Default.CameraMode);

        }


        public void UpdateMouse(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            if(keyState.IsKeyDown(ControlSettings.Default.CameraMode))
            {
                game.IsMouseVisible = false;
            }
          
        }

        public void UpdateInput(DwarfGame game, GameTime time)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();


            if(!IsMouseOverGui())
            {
                UpdateMouse(Mouse.GetState(), Keyboard.GetState(), game, time);
                VoxSelector.Update();
            }

        }

        public void OnKeyPressed()
        {
        }

        public void OnKeyReleased(Keys key)
        {
            if(key == ControlSettings.Default.SliceUp)
            {
                PlayState.ChunkManager.ChunkData.SetMaxViewingLevel(PlayState.ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
            }

            if(key == ControlSettings.Default.SliceDown)
            {
                PlayState.ChunkManager.ChunkData.SetMaxViewingLevel(PlayState.ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
            }


            if(key == ControlSettings.Default.GodMode)
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