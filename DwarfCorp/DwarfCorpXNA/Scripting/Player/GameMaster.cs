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
            MoveObjects,
            God
        }


        public OrbitCamera CameraController { get; set; }

        [JsonIgnore]
        public VoxelSelector VoxSelector { get; set; }

        [JsonIgnore]
        public BodySelector BodySelector { get; set; }

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
        public List<CreatureAI> SelectedMinions { get { return Faction.SelectedMinions; } set { Faction.SelectedMinions = value; } }

        [JsonIgnore]
        public SpellTree Spells { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }


        protected void OnDeserialized(StreamingContext context)
        {
            World = (WorldManager)(context.Context);
            Initialize(GameState.Game, World.ComponentManager, World.ChunkManager, World.Camera, World.ChunkManager.Graphics);
            World.Master = this;
        }

        public GameMaster()
        {
        }

        public GameMaster(Faction faction, DwarfGame game, ComponentManager components, ChunkManager chunks, OrbitCamera camera, GraphicsDevice graphics)
        {
            World = components.World;
            Faction = faction;
            Initialize(game, components, chunks, camera, graphics);
            VoxSelector.Selected += OnSelected;
            VoxSelector.Dragged += OnDrag;
            BodySelector.Selected += OnBodiesSelected;
            BodySelector.MouseOver += OnMouseOver;
            World.Master = this;
            World.Time.NewDay += Time_NewDay;
        }

        public void Initialize(DwarfGame game, ComponentManager components, ChunkManager chunks, OrbitCamera camera, GraphicsDevice graphics)
        {
            RoomLibrary.InitializeStatics();

            CameraController = camera;
            VoxSelector = new VoxelSelector(World, CameraController, chunks.Graphics, chunks);
            BodySelector = new BodySelector(CameraController, chunks.Graphics, components);
            SelectedMinions = new List<CreatureAI>();
            Spells = SpellLibrary.CreateSpellTree(components.World);
            CreateTools();

            InputManager.KeyReleasedCallback += OnKeyReleased;
        }

        public void Destroy()
        {
            VoxSelector.Selected -= OnSelected;
            VoxSelector.Dragged -= OnDrag;
            BodySelector.Selected -= OnBodiesSelected;
            BodySelector.MouseOver -= OnMouseOver;
            World.Time.NewDay -= Time_NewDay;
            InputManager.KeyReleasedCallback -= OnKeyReleased;
            Tools[ToolMode.God].Destroy();
            Tools[ToolMode.SelectUnits].Destroy();
            Tools.Clear();
            Faction = null;
            VoxSelector = null;
            BodySelector = null;
        }

        private void CreateTools()
        {
            Tools = new Dictionary<ToolMode, PlayerTool>();
            Tools[ToolMode.God] = new GodModeTool(this);

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
                BuildType = BuildTypes.AllButCook,
            };

            Tools[ToolMode.Magic] = new MagicTool(this);

            Tools[ToolMode.Cook] = new BuildTool
            {
                Player = this,
                BuildType = BuildTypes.Cook,
            };

            Tools[ToolMode.MoveObjects] = new MoveObjectTool()
            {
                Player = this
            };
        }

        void Time_NewDay(DateTime time)
        {
            PayEmployees();
        }

        public void OnMouseOver(IEnumerable<Body> bodies)
        {
            CurrentTool.OnMouseOver(bodies);
        }

        public void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            CurrentTool.OnBodiesSelected(bodies, button);
        }

        public void OnDrag(List<TemporaryVoxelHandle> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsDragged(voxels, button);
        }

        public void OnSelected(List<TemporaryVoxelHandle> voxels, InputManager.MouseButton button)
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
                    creature.Tasks.Add(new ActWrapperTask(new GetMoneyAct(creature, pay)) { AutoRetry = true, Name = "Get paid." });
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.NotPaid);
                }

                if (total >= Faction.Economy.CurrentMoney)
                {
                    if (!noMoney)
                    {
                        World.MakeAnnouncement("If we don't make a profit by tomorrow, our stock will crash!");
                        World.Tutorial("money");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                    }
                    noMoney = true;
                }
                else
                {
                    creature.AddThought(Thought.ThoughtType.GotPaid);
                }
            }

            World.MakeAnnouncement(String.Format("We paid our employees {0} today.",
                total), null, ContentPaths.Audio.change);
            World.Tutorial("pay");
        }


        public void Render(DwarfGame game, DwarfTime time, GraphicsDevice g)
        {
            CurrentTool.Render(game, g, time);
            VoxSelector.Render();

            foreach (var m in Faction.Minions)
            {
                m.Creature.SelectionCircle.IsVisible = false;
                m.Creature.Sprite.DrawSilhouette = false;
            };

            foreach (CreatureAI creature in Faction.SelectedMinions)
            {
                creature.Creature.SelectionCircle.IsVisible = true;
                creature.Creature.Sprite.DrawSilhouette = true;

                foreach (Task task in creature.Tasks)
                {
                    if (task.IsFeasible(creature.Creature))
                        task.Render(time);
                }

                if (creature.CurrentTask != null)
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

        }

        public void Update(DwarfGame game, DwarfTime time)
        {
            CurrentTool.Update(game, time);

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
                foreach (CreatureAI minion in Faction.Minions)
                {
                    minion.AddThought(Thought.ThoughtType.FriendDied);

                    if (!minion.IsDead) continue;
                    World.MakeAnnouncement(
                        String.Format("{0} ({1}) died!", minion.Stats.FullName, minion.Stats.CurrentClass.Name));
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
                    World.Tutorial("death");
                }

            }

            Faction.Minions.RemoveAll(m => m.IsDead);

            UpdateRooms();

            Faction.CraftBuilder.Update(time, this);

            HandlePosessedDwarf();
        }


        public void HandlePosessedDwarf()
        {
            foreach (var creature in Faction.Minions)
            {
                creature.IsPosessed = false;
            }
            KeyboardState keyState = Keyboard.GetState();
            if (SelectedMinions.Count != 1)
            {
                CameraController.FollowAutoTarget = false;
                CameraController.EnableControl = true;
                return;
            }

            var dwarf = SelectedMinions[0];
            dwarf.IsPosessed = true;
            CameraController.EnableControl = false;
            CameraController.AutoTarget = dwarf.Position;
            CameraController.FollowAutoTarget = true;

            if (dwarf.Velocity.Length() > 0.1)
            {
                var above = VoxelHelpers.FindFirstVoxelAbove(new TemporaryVoxelHandle(
                    World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(dwarf.Position)));

                if (above.IsValid)
                { 
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(above.Coordinate.Y - 1, ChunkManager.SliceMode.Y);
                }
                else
                {
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(VoxelConstants.ChunkSizeY, ChunkManager.SliceMode.Y);
                }
            }

            Vector3 forward = CameraController.GetForwardVector();
            Vector3 right = CameraController.GetRightVector();
            Vector3 desiredVelocity = Vector3.Zero;
            bool hadCommand = false;
            bool jumpCommand = false;
            if (keyState.IsKeyDown(ControlSettings.Mappings.Forward) || keyState.IsKeyDown(Keys.Up))
            {
                hadCommand = true;
                desiredVelocity += forward * 10;
            }

            if (keyState.IsKeyDown(ControlSettings.Mappings.Back) || keyState.IsKeyDown(Keys.Down))
            {
                hadCommand = true;
                desiredVelocity -= forward * 10;
            }

            if (keyState.IsKeyDown(ControlSettings.Mappings.Right) || keyState.IsKeyDown(Keys.Right))
            {
                hadCommand = true;
                desiredVelocity += right * 10;
            }

            if (keyState.IsKeyDown(ControlSettings.Mappings.Left) || keyState.IsKeyDown(Keys.Left))
            {
                hadCommand = true;
                desiredVelocity -= right * 10;
            }

            if (keyState.IsKeyDown(ControlSettings.Mappings.Jump))
            {
                jumpCommand = true;
                hadCommand = true;
            }

            if (hadCommand)
            {
                if (dwarf.CurrentTask != null)
                    dwarf.CurrentTask.Cancel();
                dwarf.CurrentTask = null;
                dwarf.TryMoveVelocity(desiredVelocity, jumpCommand);
            }
            else if (dwarf.CurrentTask == null)
            {
                if (dwarf.Creature.IsOnGround)
                {
                    if (dwarf.Physics.Velocity.LengthSquared() < 1)
                    {
                        dwarf.Creature.CurrentCharacterMode = DwarfCorp.CharacterMode.Idle;
                    }
                    dwarf.Physics.Velocity = new Vector3(dwarf.Physics.Velocity.X * 0.9f, dwarf.Physics.Velocity.Y,
                        dwarf.Physics.Velocity.Z * 0.9f);
                    dwarf.TryMoveVelocity(Vector3.Zero, false);
                }
            }

        }

        #region input


        public bool IsCameraRotationModeActive()
        {
            return KeyManager.RotationEnabled();

        }


        public void UpdateMouse(MouseState mouseState, KeyboardState keyState, DwarfGame game, DwarfTime time)
        {
            if (KeyManager.RotationEnabled())
            {
                World.SetMouse(null);
            }

        }

        public void UpdateInput(DwarfGame game, DwarfTime time)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();


            if (!World.IsMouseOverGui)
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
            if (key == ControlSettings.Mappings.SliceUp)
            {
                World.Tutorial("unslice");
                World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
            }

            else if (key == ControlSettings.Mappings.SliceDown)
            {
                World.Tutorial("unslice");
                World.ChunkManager.ChunkData.SetMaxViewingLevel(World.ChunkManager.ChunkData.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
            }
            else if (key == ControlSettings.Mappings.SliceSelected)
            {
                if (VoxSelector.VoxelUnderMouse != null)
                {
                    World.Tutorial("unslice");
                    World.ChunkManager.ChunkData.SetMaxViewingLevel(VoxSelector.VoxelUnderMouse.Coordinate.Y,
                        ChunkManager.SliceMode.Y);
                    Drawer3D.DrawBox(VoxSelector.VoxelUnderMouse.GetBoundingBox(), Color.White, 0.15f, true);
                }
            }
            else if (key == ControlSettings.Mappings.Unslice)
            {
                World.ChunkManager.ChunkData.SetMaxViewingLevel(VoxelConstants.ChunkSizeY, ChunkManager.SliceMode.Y);
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

        #endregion
    }
}
