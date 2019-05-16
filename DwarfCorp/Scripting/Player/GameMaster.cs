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
    public class GameMaster
    {
        public OrbitCamera Camera;

        [JsonIgnore]
        public VoxelSelector VoxSelector { get; set; }

        [JsonIgnore]
        public BodySelector BodySelector { get; set; }

        #region  Player tool management

        [JsonIgnore]
        public Dictionary<String, PlayerTool> Tools { get; set; }

        [JsonIgnore]
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }

        public String CurrentToolMode = "SelectUnits";

        public void ChangeTool(String NewTool)
        {
            if (NewTool != "SelectUnits")
            {
                SelectedObjects = new List<GameComponent>();
            }

            // Todo: Should probably clean up existing tool even if they are the same tool.
            Tools[NewTool].OnBegin();
            if (CurrentToolMode != NewTool)
                CurrentTool.OnEnd();
            CurrentToolMode = NewTool;
        }

        #endregion

        [JsonIgnore]
        public List<GameComponent> SelectedObjects = new List<GameComponent>();

        [JsonIgnore]
        public WorldManager World { get; set; }

        public TaskManager TaskManager { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            World = (WorldManager)(context.Context);
            Initialize();
            World.Master = this;
            TaskManager.Faction = World.PlayerFaction;
        }

        public GameMaster()
        {
        }

        // Todo: Clean up construction
        public GameMaster(WorldManager World)
        {
            TaskManager = new TaskManager();
            TaskManager.Faction = World.PlayerFaction;

            this.World = World;
            
            Initialize();
            VoxSelector.Selected += OnSelected;
            VoxSelector.Dragged += OnDrag;
            BodySelector.Selected += OnBodiesSelected;
            BodySelector.MouseOver += OnMouseOver;
            World.Master = this;
            World.Time.NewDay += Time_NewDay;
        }

        public void Initialize()
        {
            Camera = World.Renderer.Camera;
            VoxSelector = new VoxelSelector(World);
            BodySelector = new BodySelector(Camera, GameState.Game.GraphicsDevice, World.ComponentManager);
            CreateTools();
        }

        public void Destroy()
        {
            VoxSelector.Selected -= OnSelected;
            VoxSelector.Dragged -= OnDrag;
            BodySelector.Selected -= OnBodiesSelected;
            BodySelector.MouseOver -= OnMouseOver;
            World.Time.NewDay -= Time_NewDay;
            Tools["God"].Destroy();
            Tools["SelectUnits"].Destroy();
            Tools.Clear();
            VoxSelector = null;
            BodySelector = null;
        }

        // Todo: Give these the mod hook treatment.
        private void CreateTools()
        {
            Tools = new Dictionary<String, PlayerTool>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(ToolFactoryAttribute), typeof(PlayerTool), new Type[]
            {
                typeof(GameMaster)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is ToolFactoryAttribute) as ToolFactoryAttribute;
                if (attribute == null) continue;
                Tools[attribute.Name] = method.Invoke(null, new Object[] { this }) as PlayerTool;
            }
        }

        void Time_NewDay(DateTime time)
        {
            World.PlayerFaction.PayEmployees();
        }

        public void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            CurrentTool.OnMouseOver(bodies);
        }

        public void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            CurrentTool.OnBodiesSelected(bodies, button);
            if (CurrentToolMode == "SelectUnits")
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

        public void Render2D(DwarfGame game, DwarfTime time)
        {
            CurrentTool.Render2D(game, time);
            
            foreach (CreatureAI creature in World.PlayerFaction.SelectedMinions)
            {
                foreach (Task task in creature.Tasks)
                    if (task.IsFeasible(creature.Creature) == Task.Feasibility.Feasible)
                        task.Render(time);

                if (creature.CurrentTask != null)
                    creature.CurrentTask.Render(time);
            }

            DwarfGame.SpriteBatch.Begin();
            BodySelector.Render(DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.End();
        }

        public void Render3D(DwarfGame game, DwarfTime time)
        {
            CurrentTool.Render3D(game, time);
            VoxSelector.Render();

            foreach (var obj in SelectedObjects)
                if (obj.IsVisible && !obj.IsDead)
                    Drawer3D.DrawBox(obj.GetBoundingBox(), Color.White, 0.01f, true);
        }

        private Timer orphanedTaskRateLimiter = new Timer(10.0f, false, Timer.TimerMode.Real);
        private Timer checkFoodTimer = new Timer(60.0f, false, Timer.TimerMode.Real);

        // This hack exists to find orphaned tasks not assigned to any dwarf, and to then
        // put them on the task list.
        // Todo: With the new task pool, how often is this used?
        // Todo: Belongs in... WorldManager?
        public void UpdateOrphanedTasks()
        {
            orphanedTaskRateLimiter.Update(DwarfTime.LastTime);
            if (orphanedTaskRateLimiter.HasTriggered)
            {
                List<Task> orphanedTasks = new List<Task>();
                
                foreach (var ent in World.PlayerFaction.Designations.EnumerateEntityDesignations())
                {
                    if (ent.Type == DesignationType.Attack)
                    {
                        var task = new KillEntityTask(ent.Body, KillEntityTask.KillType.Attack);
                        if (!TaskManager.HasTask(task) &&
                            !World.PlayerFaction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }
                    
                    
                    else if (ent.Type == DesignationType.Craft)
                    {
                        var task = new CraftItemTask(ent.Tag as CraftDesignation);
                        if (!TaskManager.HasTask(task) &&
                            !World.PlayerFaction.Minions.Any(minion => minion.Tasks.Contains(task)))
                        {
                            orphanedTasks.Add(task);
                        }
                    }
                    
                    // TODO ... other entity task types
                }

                if (orphanedTasks.Count > 0)
                    TaskManager.AddTasks(orphanedTasks);
            }
        }

        public void Update(DwarfGame game, DwarfTime time)
        {
            // Todo: All input handling should be in one spot. PlayState!
            TaskManager.Update(World.PlayerFaction.Minions);
            CurrentTool.Update(game, time);
            World.PlayerFaction.RoomBuilder.Update();
            UpdateOrphanedTasks();

            if (World.Paused)
                Camera.LastWheel = Mouse.GetState().ScrollWheelValue;

            UpdateInput(game, time);

            if (World.PlayerFaction.Minions.Any(m => m.IsDead && m.TriggersMourning))
            {
                foreach (CreatureAI minion in World.PlayerFaction.Minions)
                {
                    minion.Creature.AddThought(Thought.ThoughtType.FriendDied);

                    if (!minion.IsDead) continue;

                    World.MakeAnnouncement(String.Format("{0} ({1}) died!", minion.Stats.FullName, minion.Stats.CurrentClass.Name));
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
                    World.Tutorial("death");
                }

            }

            World.PlayerFaction.Minions.RemoveAll(m => m.IsDead);

            checkFoodTimer.Update(time);
            if (checkFoodTimer.HasTriggered)
            {
                var food = World.PlayerFaction.CountResourcesWithTag(Resource.ResourceTags.Edible);
                if (food == 0)
                {
                    World.PlayerFaction.World.MakeAnnouncement("We're out of food!", null, () => { return World.PlayerFaction.CountResourcesWithTag(Resource.ResourceTags.Edible) == 0; });
                }
            }

            foreach(var minion in World.PlayerFaction.Minions)
            {
                if (minion == null) throw new InvalidProgramException("Null minion?");
                if (minion.Stats == null) throw new InvalidProgramException("Minion has null status?");

                if (minion.Stats.IsAsleep)
                    continue;

                if (minion.CurrentTask == null)
                    continue;

                if (minion.Stats.IsTaskAllowed(Task.TaskCategory.Dig))
                    minion.Movement.SetCan(MoveType.Dig, GameSettings.Default.AllowAutoDigging);

                minion.ResetPositionConstraint();
            }
        }

        #region input


        public bool IsCameraRotationModeActive()
        {
            return KeyManager.RotationEnabled(World.Renderer.Camera);

        }


        public void UpdateMouse(MouseState mouseState, KeyboardState keyState, DwarfGame game, DwarfTime time)
        {
            if (KeyManager.RotationEnabled(World.Renderer.Camera))
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

        #endregion
    }
}
