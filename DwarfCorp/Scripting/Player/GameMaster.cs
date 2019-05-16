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
        [JsonIgnore]
        public VoxelSelector VoxSelector { get; set; }

        [JsonIgnore]
        public BodySelector BodySelector { get; set; }

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
            World.Master = this;
            World.Time.NewDay += Time_NewDay;
        }

        public void Initialize()
        {
            VoxSelector = new VoxelSelector(World);
            BodySelector = new BodySelector(World.Renderer.Camera, GameState.Game.GraphicsDevice, World.ComponentManager);
        }

        public void Destroy()
        {
            World.Time.NewDay -= Time_NewDay;
            VoxSelector = null;
            BodySelector = null;
        }

        void Time_NewDay(DateTime time)
        {
            World.PlayerFaction.PayEmployees();
        }

        private Timer orphanedTaskRateLimiter = new Timer(10.0f, false, Timer.TimerMode.Real);

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
            World.PlayerFaction.RoomBuilder.Update();
            UpdateOrphanedTasks();

            if (World.Paused)
                World.Renderer.Camera.LastWheel = Mouse.GetState().ScrollWheelValue;

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

        public void UpdateInput(DwarfGame game, DwarfTime time)
        {
            if (!World.UserInterface.IsMouseOverGui)
            {
                if (KeyManager.RotationEnabled(World.Renderer.Camera))
                    World.UserInterface.SetMouse(null);
                VoxSelector.Update();
                BodySelector.Update();
            }

        }

        #endregion
    }
}
