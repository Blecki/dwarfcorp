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
            BuildZone,
            BuildWall,
            BuildObject,
            Magic,
            Gather,
            Chop,
            Guard,
            Attack,
            Plant,
            Wrangle,
            Craft,
            MoveObjects,
            DeconstructObjects,
            BuildRail,
            PaintRail,
            God,
            CancelTasks
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
            if (NewTool != ToolMode.SelectUnits)
            {
                SelectedObjects = new List<Body>();
            }

            // Todo: Should probably clean up existing tool even if they are the same tool.
            Tools[NewTool].OnBegin();
            if (CurrentToolMode != NewTool)
                CurrentTool.OnEnd();
            CurrentToolMode = NewTool;
        }

        #endregion


        [JsonIgnore]
        public List<CreatureAI> SelectedMinions { get { return Faction.SelectedMinions; } set { Faction.SelectedMinions = value; } }

        [JsonIgnore]
        public List<Body> SelectedObjects = new List<Body>();

        [JsonIgnore]
        public SpellTree Spells { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        public TaskManager TaskManager { get; set; }

        private bool sliceDownheld = false;
        private bool sliceUpheld = false;
        private Timer sliceDownTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        private Timer sliceUpTimer = new Timer(0.5f, true, Timer.TimerMode.Real);
        public int MaxViewingLevel = VoxelConstants.ChunkSizeY;

        public Scripting.Gambling GamblingState = new Scripting.Gambling();

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            World = (WorldManager)(context.Context);
            Initialize(GameState.Game, World.ComponentManager, World.ChunkManager, World.Camera, GameState.Game.GraphicsDevice);
            World.Master = this;
            TaskManager.Faction = Faction;
        }

        public GameMaster()
        {
        }

        public GameMaster(Faction faction, DwarfGame game, ComponentManager components, ChunkManager chunks, OrbitCamera camera, GraphicsDevice graphics)
        {
            TaskManager = new TaskManager();
            TaskManager.Faction = faction;

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
            VoxSelector = new VoxelSelector(World, CameraController, GameState.Game.GraphicsDevice, chunks);
            BodySelector = new BodySelector(CameraController, GameState.Game.GraphicsDevice, components);
            SelectedMinions = new List<CreatureAI>();

            if (Spells == null)
                Spells = SpellLibrary.CreateSpellTree(components.World);
            CreateTools();
            
            //InputManager.KeyReleasedCallback += OnKeyReleased;
            //InputManager.KeyPressedCallback += OnKeyPressed;
        }

        public void Destroy()
        {
            VoxSelector.Selected -= OnSelected;
            VoxSelector.Dragged -= OnDrag;
            BodySelector.Selected -= OnBodiesSelected;
            BodySelector.MouseOver -= OnMouseOver;
            World.Time.NewDay -= Time_NewDay;
            //InputManager.KeyReleasedCallback -= OnKeyReleased;
            //InputManager.KeyPressedCallback -= OnKeyPressed;
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

            Tools[ToolMode.Plant] = new PlantTool
            {
                Player = this
            };

            Tools[ToolMode.Wrangle] = new WrangleTool
            {
                Player = this
            };

            Tools[ToolMode.Dig] = new DigTool
            {
                Player = this,
            };

            Tools[ToolMode.Gather] = new GatherTool
            {
                Player = this,
            };

            Tools[ToolMode.Guard] = new GuardTool
            {
                Player = this,
            };

            Tools[ToolMode.Chop] = new ChopTool
            {
                Player = this,
            };

            Tools[ToolMode.Attack] = new AttackTool
            {
                Player = this,
            };

            Tools[ToolMode.BuildZone] = new BuildZoneTool
            {
                Player = this,
            };

            Tools[ToolMode.BuildWall] = new BuildWallTool
            {
                Player = this
            };

            Tools[ToolMode.BuildObject] = new BuildObjectTool
            {
                Player = this,
                World = World
            };

            Tools[ToolMode.Magic] = new MagicTool(this);

            Tools[ToolMode.MoveObjects] = new MoveObjectTool()
            {
                Player = this
            };

            Tools[ToolMode.DeconstructObjects] = new DeconstructObjectTool()
            {
                Player = this
            };

            Tools[ToolMode.BuildRail] = new Rail.BuildRailTool(this);
            Tools[ToolMode.PaintRail] = new Rail.PaintRailTool(this);
            Tools[ToolMode.CancelTasks] = new CancelTasksTool()
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
            if (CurrentToolMode == ToolMode.SelectUnits)
                SelectedObjects = bodies;
        }

        public void OnDrag(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsDragged(voxels, button);
        }

        public void OnSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            CurrentTool.OnVoxelsSelected(voxels, button);
        }

        public bool AreAllEmployeesAsleep()
        {
            return (Faction.Minions.Count > 0) && Faction.Minions.All(minion => (!minion.Stats.CanSleep || minion.Creature.IsAsleep) && !minion.IsDead);
        }

        // Todo: %KILL% - does not belong here.
        public void SetMaxViewingLevel(int level)
        {
            if (level == MaxViewingLevel)
                return;
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_click_voxel, 0.15f, (float)(level / (float)VoxelConstants.ChunkSizeY) - 0.5f);

            var oldLevel = MaxViewingLevel;

            MaxViewingLevel = Math.Max(Math.Min(level, VoxelConstants.ChunkSizeY), 1);

            foreach (var c in World.ChunkManager.ChunkData.ChunkMap)
            {
                c.InvalidateSlice(oldLevel - 1);
                c.InvalidateSlice(MaxViewingLevel - 1);
            }
        }

        // Todo: Why exactly is Faction.Minions a list of CreatureAI and not a list of Creature?
        public void PayEmployees()
        {
            DwarfBux total = 0;
            bool noMoney = false;
            foreach (CreatureAI creature in Faction.Minions)
            {
                if (creature.Stats.IsOverQualified)
                {
                    creature.Creature.AddThought(Thought.ThoughtType.IsOverQualified);
                }

                if (!noMoney)
                {
                    DwarfBux pay = creature.Stats.CurrentLevel.Pay;
                    total += pay;
                    creature.AssignTask(new ActWrapperTask(new GetMoneyAct(creature, pay)) { AutoRetry = true, Name = "Get paid." });
                }
                else
                {
                    creature.Creature.AddThought(Thought.ThoughtType.NotPaid);
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
                    creature.Creature.AddThought(Thought.ThoughtType.GotPaid);
                }
            }

            World.MakeAnnouncement(String.Format("We paid our employees {0} today.",
                total));
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.15f);
            World.Tutorial("pay");
        }


        public void Render(DwarfGame game, DwarfTime time, GraphicsDevice g)
        {
            CurrentTool.Render(game, g, time);
            VoxSelector.Render();

            foreach (var m in Faction.Minions)
            {
                if (m.Creature.SelectionCircle != null)
                    m.Creature.SelectionCircle.IsVisible = false;
                m.Creature.Sprite.DrawSilhouette = false;
            };

            foreach (CreatureAI creature in Faction.SelectedMinions)
            {
                if (creature.Creature.SelectionCircle != null)
                    creature.Creature.SelectionCircle.IsVisible = true;
                creature.Creature.Sprite.DrawSilhouette = true;

                foreach (Task task in creature.Tasks)
                {
                    if (task.IsFeasible(creature.Creature) == Task.Feasibility.Feasible)
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

            foreach (var obj in SelectedObjects)
            {
                if (obj.IsVisible && !obj.IsDead)
                {
                    Drawer3D.DrawBox(obj.GetBoundingBox(), Color.White, 0.01f, true);
                }
            }
        }

        public void UpdateRooms()
        {

        }

        private Timer orphanedTaskRateLimiter = new Timer(10.0f, false, Timer.TimerMode.Real);
        private Timer checkFoodTimer = new Timer(60.0f, false, Timer.TimerMode.Real);

        // This hack exists to find orphaned tasks not assigned to any dwarf, and to then
        // put them on the task list.
        public void UpdateOrphanedTasks()
        {
            orphanedTaskRateLimiter.Update(DwarfTime.LastTime);
            if (orphanedTaskRateLimiter.HasTriggered)
            {
                List<Task> orphanedTasks = new List<Task>();
                
                foreach (var ent in Faction.Designations.EnumerateEntityDesignations())
                {
                    if (ent.Type == DesignationType.Attack)
                    {
                        var task = new KillEntityTask(ent.Body, KillEntityTask.KillType.Attack);
                        if (!TaskManager.HasTask(task) &&
                            !Faction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }
                    
                    
                    else if (ent.Type == DesignationType.Craft)
                    {
                        var task = new CraftItemTask(ent.Tag as CraftDesignation);
                        if (!TaskManager.HasTask(task) &&
                            !Faction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }
                    
                    // TODO ... other entity task types
                }

                if (orphanedTasks.Count > 0)
                    //TaskManager.AssignTasksGreedy(orphanedTasks, Faction.Minions);
                    TaskManager.AddTasks(orphanedTasks);
            }
        }

        public void Update(DwarfGame game, DwarfTime time)
        {
            GamblingState.Update(time);
            TaskManager.Update(Faction.Minions);
            CurrentTool.Update(game, time);
            Faction.RoomBuilder.Update();
            UpdateOrphanedTasks();
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
                    minion.Creature.AddThought(Thought.ThoughtType.FriendDied);

                    if (!minion.IsDead) continue;

                    World.MakeAnnouncement(
                        String.Format("{0} ({1}) died!", minion.Stats.FullName, minion.Stats.CurrentClass.Name));
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
                    World.Tutorial("death");
                }

            }

            Faction.Minions.RemoveAll(m => m.IsDead);

            UpdateRooms();

            HandlePosessedDwarf();

            if (sliceDownheld)
            {
                sliceDownTimer.Update(time);
                if (sliceDownTimer.HasTriggered)
                {
                    SetMaxViewingLevel(MaxViewingLevel - 1);
                    sliceDownTimer.Reset(sliceDownTimer.TargetTimeSeconds * 0.6f);
                }
            }
            else if (sliceUpheld)
            {
                sliceUpTimer.Update(time);
                if (sliceUpTimer.HasTriggered)
                {
                    SetMaxViewingLevel(MaxViewingLevel + 1);
                    sliceUpTimer.Reset(sliceUpTimer.TargetTimeSeconds * 0.6f);
                }
            }
            // Make sure that the faction's money is identical to the money in treasuries.
            Faction.Economy.CurrentMoney = Faction.Treasurys.Sum(treasury => treasury.Money);

            checkFoodTimer.Update(time);
            if (checkFoodTimer.HasTriggered)
            {
                var food = Faction.CountResourcesWithTag(Resource.ResourceTags.Edible);
                if (food == 0)
                {
                    Faction.World.MakeAnnouncement("We're out of food!", null, () => { return Faction.CountResourcesWithTag(Resource.ResourceTags.Edible) == 0; });
                }
            }
        }


        public void HandlePosessedDwarf()
        {
            // Don't attempt any control if the user is trying to type intoa focus item.
            if (World.Gui.FocusItem != null && !World.Gui.FocusItem.IsAnyParentTransparent() && !World.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }
            KeyboardState keyState = Keyboard.GetState();
            if (SelectedMinions.Count != 1)
            {
                CameraController.FollowAutoTarget = false;
                CameraController.EnableControl = true;
                foreach (var creature in Faction.Minions)
                {
                    creature.IsPosessed = false;
                }
                return;
            }

            var dwarf = SelectedMinions[0];
            if (!dwarf.IsPosessed)
            {
                CameraController.FollowAutoTarget = false;
                CameraController.EnableControl = true;
                return;
            }
            CameraController.EnableControl = false;
            CameraController.AutoTarget = dwarf.Position;
            CameraController.FollowAutoTarget = true;

            if (dwarf.Velocity.Length() > 0.1)
            {
                var above = VoxelHelpers.FindFirstVoxelAbove(new VoxelHandle(
                    World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(dwarf.Position)));

                if (above.IsValid)
                {
                    SetMaxViewingLevel(above.Coordinate.Y);
                }
                else
                {
                    SetMaxViewingLevel(VoxelConstants.ChunkSizeY);
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
                dwarf.CancelCurrentTask();
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

        public bool OnKeyPressed(Keys key)
        {
            if (key == ControlSettings.Mappings.SliceUp)
            {
                if (!sliceUpheld)
                {
                    sliceUpheld = true;
                    World.Tutorial("unslice");
                    sliceUpTimer.Reset(0.5f);
                    SetMaxViewingLevel(MaxViewingLevel + 1);
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.SliceDown)
            {
                if (!sliceDownheld)
                {
                    World.Tutorial("unslice");
                    sliceDownheld = true;
                    sliceDownTimer.Reset(0.5f);
                    SetMaxViewingLevel(MaxViewingLevel - 1);
                    return true;
                }
            }
            return false;
        }
        private int rememberedViewValue = VoxelConstants.ChunkSizeY;

        public bool OnKeyReleased(Keys key)
        {
            KeyboardState keys = Keyboard.GetState();
            if (key == ControlSettings.Mappings.SliceUp)
            {
                sliceUpheld = false;
                return true;
            }

            else if (key == ControlSettings.Mappings.SliceDown)
            {
                sliceDownheld = false;
                return true;
            }
            else if (key == ControlSettings.Mappings.SliceSelected)
            {
                if (keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl))
                {
                    SetMaxViewingLevel(rememberedViewValue);
                    return true;
                }
                else if (VoxSelector.VoxelUnderMouse.IsValid)
                {
                    World.Tutorial("unslice");
                    SetMaxViewingLevel(VoxSelector.VoxelUnderMouse.Coordinate.Y + 1);
                    Drawer3D.DrawBox(VoxSelector.VoxelUnderMouse.GetBoundingBox(), Color.White, 0.15f, true);
                    return true;
                }
            }
            else if (key == ControlSettings.Mappings.Unslice)
            {
                rememberedViewValue = MaxViewingLevel;
                SetMaxViewingLevel(VoxelConstants.ChunkSizeY);
                return true;
            }
            return false;
        }

        #endregion
    }
}
