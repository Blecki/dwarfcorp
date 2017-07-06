// CreatureAI.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
//using System.Windows.Forms;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Component which manages the AI, scripting, and status of a particular creature (such as a Dwarf or Goblin)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CreatureAI : GameComponent, IUpdateableComponent
    {
        public int MaxMessages = 10;
        public List<string> MessageBuffer = new List<string>();

        public CreatureAI()
        {
            History = new Dictionary<string, TaskHistory>();
        }

        public CreatureAI(
            ComponentManager Manager,
            string name,
            EnemySensor sensor,
            PlanService planService) :
            base(name, Manager)
        {
            History = new Dictionary<string, TaskHistory>();
            Movement = new CreatureMovement(this);
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            CurrentPath = null;
            PlannerTimer = new Timer(0.1f, false);
            LocalControlTimeout = new Timer(5, false, Timer.TimerMode.Real);
            WanderTimer = new Timer(1, false);
            DrawAIPlan = false;
            WaitingOnResponse = false;
            PlanSubscriber = new PlanSubscriber(planService);
            ServiceTimeout = new Timer(2, false, Timer.TimerMode.Real);
            Sensor = sensor;
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            Sensor.Creature = this;
            CurrentTask = null;
            Tasks = new List<Task>();
            Thoughts = new List<Thought>();
            IdleTimer = new Timer(2.0f, true);
            SpeakTimer = new Timer(5.0f, true);
            XPEvents = new List<int>();
        }

        private bool jumpHeld = false;
        /// <summary> The current path of voxels the AI is following </summary>
        public List<Voxel> CurrentPath { get; set; }

        private Creature _cachedCreature = null;
        [JsonIgnore] public Creature Creature
        {
            get
            {
                if (_cachedCreature == null)
                    _cachedCreature = Parent.EnumerateAll().OfType<Creature>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_cachedCreature != null, "AI Could not find creature");
                return _cachedCreature;
            }
        }
        /// <summary> If this is set to true, the creature will draw the path it is following </summary>
        public bool DrawPath { get { return GameSettings.Default.DrawPaths; }}
        /// <summary> The gather manager handles gathering/building tasks </summary>
        public GatherManager GatherManager { get; set; }
        /// <summary> When this timer times out, the creature will awake from Idle mode and attempt to find something to do </summary>
        public Timer IdleTimer { get; set; }
        /// <summary> A creature has a list of thoughts in its mind. Toughts affect the emotional state of a creature. Try not to think disturbing thoughts, little dwarfs. </summary>
        public List<Thought> Thoughts { get; set; }
        /// <summary> When this timer triggers, the creature will attempt to speak to its neighbors, inducing thoughts. </summary>
        public Timer SpeakTimer { get; set; }

        /// <summary> Gets the Act that the creature is currently performing if it exists. </summary>
        [JsonIgnore]
        public Act CurrentAct
        {
            get
            {
                if (CurrentTask != null) return CurrentTask.Script;
                return null;
            }
        }

        /// <summary> Gets the current Task the creature is trying to perform </summary>
        [JsonIgnore]
        public Task CurrentTask { get; set; }

        /// <summary> When this timer triggers, the creature will poll the PlanService for replanning paths </summary>
        public Timer PlannerTimer { get; set; }
        /// <summary> When this timer triggers, the creature will stop trying to reach a local target (if it is blocked by a voxel for instance </summary>
        public Timer LocalControlTimeout { get; set; }
        /// <summary> When this timer triggers, the creature will wander in a new direction when it has nothing to do. </summary>
        public Timer WanderTimer { get; set; }
        /// <summary> This is the timeout for waiting on services (like the path planning service) </summary>
        public Timer ServiceTimeout { get; set; }
        /// <summary> TODO(mklingen): DEPRECATED. REMOVE </summary>
        public bool DrawAIPlan { get; set; }

        /// <summary> This is a Subscriber which waits for new paths from the A* planner </summary>
        public PlanSubscriber PlanSubscriber { get; set; }
        /// <summary> If true, the AI is waiting on a plan from the PlanSubscriber </summary>
        public bool WaitingOnResponse { get; set; }
        /// <summary>The AI uses this sensor to search for nearby enemies </summary>
        public EnemySensor Sensor { get; set; }
        /// <summary> This defines how the creature can move from voxel to voxel. </summary>
        public CreatureMovement Movement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is posessed. If a creature is posessed,
        /// it is being controlled by the player, so it shouldn't attempt to move unless it has to.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is posessed; otherwise, <c>false</c>.
        /// </value>
        public bool IsPosessed { get; set; }

        /// <summary> Wrapper around Creature.Hands </summary>
        [JsonIgnore]
        public Grabber Hands
        {
            get { return Creature.Hands; }
            set { Creature.Hands = value; }
        }

        /// <summary> Wrapper around Creature.Physics </summary>
        [JsonIgnore]
        public Physics Physics
        {
            get { return Creature.Physics; }
            set { Creature.Physics = value; }
        }

        /// <summary> Wrapper around Creature.Faction </summary>
        [JsonIgnore]
        public Faction Faction
        {
            get { return Creature.Faction; }
            set { Creature.Faction = value; }
        }

        /// <summary> Wrapper around Creature.Stats </summary>
        [JsonIgnore]
        public CreatureStats Stats
        {
            get { return Creature.Stats; }
        }

        /// <summary> Wrapper around Creature.Status </summary>
        [JsonIgnore]
        public CreatureStatus Status
        {
            get { return Creature.Status; }
            set { Creature.Status = value; }
        }

        /// <summary> Wrapper around Creature.Physics.Velocity </summary>
        [JsonIgnore]
        public Vector3 Velocity
        {
            get { return Creature.Physics.Velocity; }
            set { Creature.Physics.Velocity = value; }
        }

        /// <summary> Wrapper around Creature.Physics.GlobalTransform.Translation </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return Creature.Physics.GlobalTransform.Translation; }
            set
            {
                Matrix newTransform = Creature.Physics.LocalTransform;
                newTransform.Translation = value;
                Creature.Physics.LocalTransform = newTransform;
            }
        }

        /// <summary> Wrapper around Playstate.ChunkManager </summary>
        [JsonIgnore]
        public ChunkManager Chunks
        {
            get { return Manager.World.ChunkManager; }
        }

        /// <summary> Blackboard used for Acts. </summary>
        public Blackboard Blackboard { get; set; }

        /// <summary> 
        /// Tells us which tasks the creature has performed in the past. Maps task names to their histories.
        /// This is useful for determining how many times a task has failed or succeeded.
        /// </summary>
        public Dictionary<string, TaskHistory> History { get; set; }

        /// <summary>
        /// Queue of tasks that the creature is currently performing.
        /// </summary>
        public List<Task> Tasks { get; set; }

        /// <summary>
        /// If true, whent he creature dies its friends will mourn its death, generating unhappy Thoughts
        /// </summary>
        public bool TriggersMourning { get; set; }
        /// <summary> List of changes to the creatures XP over time.</summary>
        public List<int> XPEvents { get; set; }

        public string Biography = "";

        public BoundingBox? PositionConstraint = null;

        /// <summary> Add exprience points to the creature. It will level up from time to time </summary>
        public void AddXP(int amount)
        {
            XPEvents.Add(amount);
        }

        /// <summary> Called whenever a list of enemies has been sensed by the creature </summary>
        private void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            if (enemies.Count > 0)
            {
                foreach (CreatureAI threat in enemies.Where(threat => !Faction.Threats.Contains(threat.Creature)))
                {
                    Faction.Threats.Add(threat.Creature);
                }
            }
        }

        /// <summary>
        /// Raycasts to the specified target and returns true if the ray hit a voxel before hitting the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>True if the ray hit a voxel before the target.</returns>
        public bool Raycast(Vector3 target)
        {
            return Manager.World.ChunkManager.ChunkData.CheckRaySolid(Creature.AI.Position, target);
        }

        /// <summary> Find the task from the list of tasks which is easiest to perform. </summary>
        public Task GetEasiestTask(List<Task> tasks)
        {
            if (tasks == null)
            {
                return null;
            }

            float bestCost = float.MaxValue;
            Task bestTask = null;
            var bestPriority = Task.PriorityType.Eventually;


            foreach (Task task in tasks)
            {
                // A bit janky, but tasks are indexed by name. Look for any task that we have a history of
                // and if the task is locked (because it was found to be impossible), don't try it.
                if (History.ContainsKey(task.Name) && History[task.Name].IsLocked)
                {
                    continue;
                }

                float cost = task.ComputeCost(Creature);

                if (task.IsFeasible(Creature) && task.Priority >= bestPriority && cost < bestCost)
                {
                    bestCost = cost;
                    bestTask = task;
                    bestPriority = task.Priority;
                }
            }

            return bestTask;
        }

        /// <summary> Looks for any tasks with higher priority than the current task. Cancel the current task and do that one instead. </summary>
        public void PreEmptTasks()
        {
            if (CurrentTask == null) return;

            Task newTask = null;
            foreach (Task task in Tasks)
            {
                if (task.Priority > CurrentTask.Priority && task.IsFeasible(Creature))
                {
                    newTask = task;
                    break;
                }
            }

            if (newTask != null)
            {
                CurrentTask.Cancel();
                if (CurrentTask.ShouldRetry(Creature))
                {
                    Tasks.Add(CurrentTask);
                    CurrentTask.SetupScript(Creature);
                }
                CurrentTask = newTask;
                newTask.SetupScript(Creature);
                Tasks.Remove(newTask);
            }
        }

        /// <summary> remove any impossible or already completed tasks </summary>
        public void DeleteBadTasks()
        {
            Tasks.RemoveAll(task => task.ShouldDelete(Creature));
        }

        /// <summary> Animate the PlayState Camera to look at this creature </summary>
        public void ZoomToMe()
        {
            Manager.World.Camera.ZoomTo(Position + Vector3.Up * 8.0f);
            Manager.World.ChunkManager.ChunkData.SetMaxViewingLevel((int)Position.Y, ChunkManager.SliceMode.Y);
        }

        public void HandleReproduction()
        {
            if (!Creature.CanReproduce) return;
            if (Creature.IsPregnant) return;
            if (!MathFunctions.RandEvent(0.0001f)) return;
            if (CurrentTask != null) return;
            IEnumerable<CreatureAI> potentialMates =
                Faction.Minions.Where(minion => minion != this && Mating.CanMate(minion.Creature, this.Creature));
            CreatureAI closestMate = null;
            float closestDist = float.MaxValue;

            foreach (var ai in potentialMates)
            {
                var dist = (ai.Position - Position).LengthSquared();
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closestMate = ai;
            }

            if (closestMate != null && closestDist < 30)
            {
                Tasks.Add(new MateTask(closestMate));
            }
        }

        /// <summary> Update this creature </summary>
        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active) return;

            if (Faction == null && !string.IsNullOrEmpty(Creature.Allies))
            {
                Faction = Manager.World.Factions.Factions[Creature.Allies];
            }

            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            OrderEnemyAttack();
            DeleteBadTasks();
            PreEmptTasks();
            HandleReproduction();

            // Try to go to sleep if we are low on energy and it is night time.
            if (Status.Energy.IsDissatisfied() && Manager.World.Time.IsNight())
            {
                Task toReturn = new SatisfyTirednessTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }

            // Try to find food if we are hungry.
            if (Status.Hunger.IsDissatisfied() && Faction.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }

            /*
            if (CurrentAct != null && CurrentAct.IsCanceled)
            {
                CurrentTask.Script = null;
                CurrentTask = null;
            }
             */

            // Update the current task.
            if (CurrentTask != null && CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();

                // Update the task history on failure or success. 
                bool retried = false;
                if (status == Act.Status.Fail)
                {
                    if (History.ContainsKey(CurrentTask.Name))
                    {
                        History[CurrentTask.Name].NumFailures++;
                    }
                    else
                    {
                        History[CurrentTask.Name] = new TaskHistory();
                    }

                    if (CurrentTask.ShouldRetry(Creature) && !History[CurrentTask.Name].IsLocked)
                    {
                        if (!Tasks.Contains(CurrentTask))
                        {
                            // Lower the priority of failed tasks.
                            CurrentTask.Priority = Task.PriorityType.Eventually;
                            Tasks.Add(CurrentTask);
                            CurrentTask.SetupScript(Creature);
                            retried = true;
                        }
                    }
                }
                else if (status == Act.Status.Success)
                {
                    // Remove completed tasks.
                    if (History.ContainsKey(CurrentTask.Name))
                    {
                        History.Remove(CurrentTask.Name);
                    }
                }

                if (status != Act.Status.Running && !retried)
                {
                    CurrentTask = null;
                }
            }
            // Otherwise, we don't have any tasks at the moment.
            else if (CurrentTask == null)
            {
                // Throw a tantrum if we're unhappy.
                bool tantrum = false;
                if (Status.Happiness.IsDissatisfied())
                {
                    tantrum = MathFunctions.Rand(0, 1) < 0.25f;
                }

                // Otherwise, find a new task to perform.
                Task goal = GetEasiestTask(Tasks);
                if (goal != null)
                {
                    if (tantrum)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Sad);
                        if (Creature.Allies == "Dwarf")
                        {
                            Manager.World.MakeAnnouncement(String.Format("{0} ({1}) refuses to work!",
                                Stats.FullName, Stats.CurrentClass.Name), ZoomToMe);
                            Manager.World.Tutorial("happiness");
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                        }
                        CurrentTask = null;
                    }
                    else
                    {
                        IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                        goal.SetupScript(Creature);
                        CurrentTask = goal;
                        Tasks.Remove(goal);
                    }
                }
                else
                {
                    CurrentTask = ActOnIdle();
                    if (CurrentTask != null)
                        CurrentTask.SetupScript(Creature);
                }
            }


            PlannerTimer.Update(gameTime);
            UpdateThoughts();
            UpdateXP();

            // With a small probability, the creature will drown if its under water.
            if (MathFunctions.RandEvent(0.01f))
            {
                Voxel above = Physics.CurrentVoxel.GetVoxelAbove();
                bool shouldDrown = above != null && (!above.IsEmpty || above.WaterLevel > 0);
                if (Physics.IsInLiquid && (!Movement.CanSwim || shouldDrown))
                {
                    Creature.Damage(1.0f, Health.DamageType.Normal);
                }
            }

            foreach (var history in History)
            {
                history.Value.Update();
            }

            if (PositionConstraint.HasValue)
            {
                Physics.LocalPosition = MathFunctions.Clamp(Physics.Position, PositionConstraint.Value);
            }
        }

        private int lastXPAnnouncement = 0;
        /// <summary> updates the creature's experience points. </summary>
        public void UpdateXP()
        {
            foreach (int xp in XPEvents)
            {
                Stats.XP += xp;
                string sign = xp > 0 ? "+" : "";

                IndicatorManager.DrawIndicator(sign + xp + " XP",
                    Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, xp > 0 ? Color.Green : Color.Red);
                if (Stats.IsOverQualified && lastXPAnnouncement != Stats.LevelIndex && Faction == Manager.World.PlayerFaction)
                {
                    lastXPAnnouncement = Stats.LevelIndex;
                    Manager.World.MakeAnnouncement(String.Format("{0} ({1}) wants a promotion!",
                            Stats.FullName, Stats.CurrentClass.Name),
                        () => Manager.World.Game.StateManager.PushState(new NewEconomyState(Manager.World.Game, Manager.World.Game.StateManager, Manager.World)),
                    ContentPaths.Audio.Oscar.sfx_gui_positive_generic);
                    Manager.World.Tutorial("level up");
                }
            }
            XPEvents.Clear();
        }

        /// <summary> The Act that the creature performs when its told to "wander" (when it has nothing to do) </summary>
        public virtual Act ActOnWander()
        {
            return new WanderAct(this, 2, 0.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f);
        }

        /// <summary>
        /// Causes the creature to look for nearby blocks to jump on
        /// so as not to fall.
        /// </summary>
        /// <returns>Success if the jump has succeeded, Fail if it failed, and Running otherwise.</returns>
        public IEnumerable<Act.Status> AvoidFalling()
        {
            var above = Physics.CurrentVoxel.GetVoxelAbove();
            foreach (Voxel vox in Physics.Neighbors)
            {
                if (vox == null) continue;
                if (vox.IsEmpty) continue;

                // Avoid teleporting through the block above. Never jump up through the
                // block above you.
                if (above != null && !above.IsEmpty)
                {
                    if ((int)vox.Position.Y >= (int)above.Position.Y)
                    {
                        continue;
                    }
                }
                Voxel voxAbove = vox.GetVoxelAbove();
                if (!voxAbove.IsEmpty) continue;
                Vector3 target = voxAbove.Position + new Vector3(0.5f, 0.5f, 0.5f);
                Physics.Face(target);
                foreach (Act.Status status in Hop(target))
                {
                    yield return Act.Status.Running;
                }
                yield return Act.Status.Success;
                yield break;
            }
            yield return Act.Status.Fail;
            yield break;
        }

        /// <summary>
        /// Hops the specified location (coroutine)
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Running until the hop completes, and then returns success.</returns>
        public IEnumerable<Act.Status> Hop(Vector3 location)
        {
            float hopTime = 0.5f;

            TossMotion motion = new TossMotion(hopTime, location.Y - Position.Y, Physics.GlobalTransform, location);
            Physics.AnimationQueue.Add(motion);

            while (!motion.IsDone())
            {
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

        /// <summary> 
        /// Task the creature performs when it has no more tasks. In this case the creature will gather any necessary
        /// resources and place any blocks. If it doesn't have anything to do, it may wander somewhere or use an item
        /// to improve its wellbeing.
        /// </summary>
        public virtual Task ActOnIdle()
        {
            if (!IsPosessed && !Creature.IsOnGround && !Movement.CanFly && !Creature.Physics.IsInLiquid)
            {
                return new ActWrapperTask(new Wrap(AvoidFalling));
            }

            if (!IsPosessed && Creature.Physics.IsInLiquid && MathFunctions.RandEvent(0.01f))
            {
                return new FindLandTask();
            }

            if (!IsPosessed && GatherManager.VoxelOrders.Count == 0 &&
                (GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile()) &&
                (GatherManager.StockMoneyOrders.Count == 0 || !Faction.HasFreeTreasury()))
            {

                // Craft random items for fun.
                if (Stats.CurrentClass.HasAction(GameMaster.ToolMode.Craft) && MathFunctions.RandEvent(0.0005f))
                {
                    var item = CraftLibrary.GetRandomApplicableCraftItem(Faction);
                    if (item != null)
                    {
                        bool gotAny = true;
                        foreach (var resource in item.RequiredResources)
                        {
                            var amount = Faction.GetResourcesWithTags(new List<Quantitiy<Resource.ResourceTags>>() { resource });
                            if (amount == null || amount.Count == 0)
                            {
                                gotAny = false;
                                break;
                            }
                            item.SelectedResources.Add(Datastructures.SelectRandom(amount));
                        }
                        if (gotAny)
                        {
                            return new CraftResourceTask(item);
                        }
                    }
                }

                // Farm stuff if applicable
                if (Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm) && MathFunctions.RandEvent(0.1f) && Faction == World.PlayerFaction)
                {
                    var task = (World.Master.Tools[GameMaster.ToolMode.Farm] as FarmTool).AutoFarm();

                    if (task != null)
                    {
                        Faction.ChopDesignations.Add(task.EntityToKill);
                        return task;
                    }
                }

                // Find a room to train in, if applicable.
                if (Stats.CurrentClass.HasAction(GameMaster.ToolMode.Attack) && MathFunctions.RandEvent(0.0005f))
                {
                    Body closestTraining = Faction.FindNearestItemWithTags("Train", Position, true);

                    if (closestTraining != null)
                    {
                        return new ActWrapperTask(new GoTrainAct(this));
                    }
                }

                if (IdleTimer.HasTriggered && MathFunctions.RandEvent(0.005f))
                {
                    return new ActWrapperTask(new MournGraves(this))
                    {
                        Priority = Task.PriorityType.Eventually,
                        AutoRetry = false
                    };
                }

                // Otherwise, try to find a chair to sit in
                if (IdleTimer.HasTriggered && MathFunctions.RandEvent(0.25f))
                {
                    return new ActWrapperTask(new GoToChairAndSitAct(this))
                    {
                        Priority = Task.PriorityType.Eventually,
                        AutoRetry = false
                    };
                }
                if (IdleTimer.HasTriggered)
                {
                    IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                    return new ActWrapperTask(ActOnWander())
                    {
                        Priority = Task.PriorityType.Eventually
                    };
                }
                Physics.Velocity *= 0.0f;
                return null;
            }
            // If we have no more build orders, look for gather orders
            if (GatherManager.VoxelOrders.Count == 0 && GatherManager.StockOrders.Count > 0)
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                if (Faction.HasFreeStockpile(order.Resource))
                {
                    GatherManager.StockOrders.RemoveAt(0);
                    return new ActWrapperTask(new StockResourceAct(this, order.Resource))
                    {
                        Priority = Task.PriorityType.Low
                    };
                }
            }
            else if (GatherManager.VoxelOrders.Count == 0 && GatherManager.StockMoneyOrders.Count > 0)
            {
                var order = GatherManager.StockMoneyOrders[0];
                if (Faction.HasFreeTreasury(order.Money))
                {
                    GatherManager.StockMoneyOrders.RemoveAt(0);
                    return new ActWrapperTask(new StockMoneyAct(this, order.Money))
                    {
                        Priority = Task.PriorityType.Low
                    };
                }
            }

            if (GatherManager.VoxelOrders.Count > 0)
            {
                // Otherwise handle build orders.
                var voxels = new List<Voxel>();
                var types = new List<VoxelType>();
                foreach (GatherManager.BuildVoxelOrder order in GatherManager.VoxelOrders)
                {
                    voxels.Add(order.Voxel);
                    types.Add(order.Type);
                }

                GatherManager.VoxelOrders.Clear();
                return new ActWrapperTask(new BuildVoxelsAct(this, voxels, types))
                {
                    Priority = Task.PriorityType.Low,
                    AutoRetry = true
                };
            }

            return null;
        }


        /// <summary> Tell the creature to jump straight up </summary>
        public void Jump(DwarfTime dt)
        {
            if (!Creature.JumpTimer.HasTriggered)
            {
                return;
            }

            Creature.Physics.ApplyForce(Vector3.Up * Creature.Stats.JumpForce, (float)dt.ElapsedGameTime.TotalSeconds);
            Creature.JumpTimer.Reset(Creature.JumpTimer.TargetTimeSeconds);
            SoundManager.PlaySound(ContentPaths.Audio.jump, Creature.Physics.GlobalTransform.Translation);
        }

        /// <summary> returns whether or not the creature already has a thought of the given type. </summary>
        public bool HasThought(Thought.ThoughtType type)
        {
            return Thoughts.Any(existingThought => existingThought.Type == type);
        }

        /// <summary> Add a standard thought to the creature. </summary>
        public void AddThought(Thought.ThoughtType type)
        {
            if (!HasThought(type))
            {
                var thought = Thought.CreateStandardThought(type, Manager.World.Time.CurrentDate);
                AddThought(thought, true);

                if (thought.HappinessModifier > 0.01)
                {
                    Creature.NoiseMaker.MakeNoise("Pleased", Position, true);
                }
                else
                {
                    Creature.NoiseMaker.MakeNoise("Tantrum", Position, true);
                }
            }
        }

        /// <summary> Remove a standard thought from the creature. </summary>
        public void RemoveThought(Thought.ThoughtType thoughtType)
        {
            Thoughts.RemoveAll(thought => thought.Type == thoughtType);
        }

        /// <summary> Add a custom thought to the creature </summary>
        public void AddThought(Thought thought, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                Thoughts.Add(thought);
            }
            else
            {
                if (HasThought(thought.Type))
                {
                    return;
                }

                Thoughts.Add(thought);
            }
            bool good = thought.HappinessModifier > 0;
            Color textColor = good ? Color.Yellow : Color.Red;
            string prefix = good ? "+" : "";
            string postfix = good ? ":)" : ":(";
            IndicatorManager.DrawIndicator(prefix + thought.HappinessModifier + " " + postfix,
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        /// <summary> Tell the creature to kill the given body. </summary>
        public void Kill(Body entity)
        {
            var killTask = new KillEntityTask(entity, KillEntityTask.KillType.Auto);
            if (!Tasks.Contains(killTask))
                Tasks.Add(killTask);
        }

        /// <summary> Tell the creature to find a path to the edge of the world and leave it. </summary>
        public void LeaveWorld()
        {
            Task leaveTask = new LeaveWorldTask
            {
                Priority = Task.PriorityType.Urgent,
                AutoRetry = true,
                Name = "Leave the world."
            };
            Tasks.Add(leaveTask);
        }

        /// <summary> Updates the thoughts in the creature's head. </summary>
        public void UpdateThoughts()
        {
            Thoughts.RemoveAll(thought => thought.IsOver(Manager.World.Time.CurrentDate));
            Status.Happiness.CurrentValue = 50.0f;

            foreach (Thought thought in Thoughts)
            {
                Status.Happiness.CurrentValue += thought.HappinessModifier;
            }

            if (Status.IsAsleep)
            {
                AddThought(Thought.ThoughtType.Slept);
            }
            else if (Status.Energy.IsDissatisfied())
            {
                AddThought(Thought.ThoughtType.FeltSleepy);
            }

            if (Status.Hunger.IsDissatisfied())
            {
                AddThought(Thought.ThoughtType.FeltHungry);
            }
        }

        /// <summary> Tell the creature to speak with another </summary>
        public void Converse(CreatureAI other)
        {
            if (SpeakTimer.HasTriggered)
            {
                AddThought(Thought.ThoughtType.Talked);
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Dots);
                Creature.Physics.Face(other.Position);
                SpeakTimer.Reset(SpeakTimer.TargetTimeSeconds);
            }
        }

        /// <summary> Returns whether or not the creature has a task with the same name as another </summary>
        public bool HasTaskWithName(string other)
        {
            return Tasks.Any(task => task.Name == other);
        }

        /// <summary> Returns whether or not the creature has a task with the same name as another </summary>
        public bool HasTaskWithName(Task other)
        {
            return Tasks.Any(task => task.Name == other.Name);
        }

        /// <summary> For any enemy that this creature's enemy sensor knows about, order the creature to attack these enemies </summary>
        public void OrderEnemyAttack()
        {
            foreach (CreatureAI enemy in Sensor.Enemies)
            {
                Task task = new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto);
                if (!HasTaskWithName(task))
                {
                    Creature.AI.Tasks.Add(task);

                    if (Faction == Manager.World.PlayerFaction)
                    {
                        Manager.World.MakeAnnouncement(
                            String.Format("{0} the {1} is fighting {2} ({3})", Stats.FullName,
                                Stats.CurrentClass.Name,
                                TextGenerator.IndefiniteArticle(enemy.Stats.CurrentClass.Name),
                                enemy.Faction.Race.Name),
                            ZoomToMe);
                        Manager.World.Tutorial("combat");
                    }
                }
            }
        }

        /// <summary> Pay the creature this amount of money. The money goes into the creature's wallet. </summary>
        public void AddMoney(DwarfBux pay)
        {
            Status.Money += pay;
            bool good = pay > 0;
            Color textColor = good ? Color.Green : Color.Red;
            string prefix = good ? "+" : "";
            IndicatorManager.DrawIndicator(prefix + pay,
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        private string GetHappinessDescription(Status Happiness)
        {
            if (Happiness.CurrentValue >= Happiness.MaxValue)
                return "VERY HAPPY";
            else if (Happiness.CurrentValue <= Happiness.MinValue)
                return "LIVID";
            else if (Happiness.IsSatisfied())
                return "SATISFIED";
            else if (Happiness.IsDissatisfied())
                return "UNHAPPY";
            else
                return "OK";
        }

        /// <summary> gets a description of the creature to display to the player </summary>
        public override string GetDescription()
        {
            string desc = Stats.FullName + ", level " + Stats.CurrentLevel.Index +
                          " " +
                          Stats.CurrentClass.Name + "\n    " +
                          "Happiness: " + GetHappinessDescription(Status.Happiness) + ". Health: " + Status.Health.Percentage +
                          ". Hunger: " + (100 - Status.Hunger.Percentage) + ". Energy: " + Status.Energy.Percentage +
                          "\n";
            if (CurrentTask != null)
            {
                desc += "    Task: " + CurrentTask.Name;
            }

            if (CurrentAct != null)
            {
                desc += "\n   Action: ";
                Act act = CurrentAct;
                while (act != null && act.LastTickedChild != null)
                {
                    Act prevAct = act;
                    act = act.LastTickedChild;
                    if (act == null)
                    {
                        act = prevAct;
                        break;
                    }

                    if (act == act.LastTickedChild)
                    {
                        break;
                    }
                }
                desc += act.Name;
            }

            return desc;
        }

        /// <summary> Task telling the creature to exit the world. </summary>
        public class LeaveWorldTask : Task
        {
            public IEnumerable<Act.Status> GreedyFallbackBehavior(Creature agent)
            {
                var edgeGoal = new EdgeGoalRegion();

                while (true)
                {
                    Voxel creatureVoxel = agent.Physics.CurrentVoxel;

                    if (edgeGoal.IsInGoalRegion(creatureVoxel))
                    {
                        yield return Act.Status.Success;
                        yield break;
                    }

                    var actions = agent.AI.Movement.GetMoveActions(creatureVoxel);

                    float minCost = float.MaxValue;
                    var minAction = new MoveAction();
                    bool hasMinAction = false;
                    foreach (var action in actions)
                    {
                        Voxel vox = action.DestinationVoxel;

                        float cost = edgeGoal.Heuristic(vox) + MathFunctions.Rand(0.0f, 5.0f);

                        if (cost < minCost)
                        {
                            minAction = action;
                            minCost = cost;
                            hasMinAction = true;
                        }
                    }

                    if (hasMinAction)
                    {
                        var nullAction = new MoveAction
                        {
                            Diff = minAction.Diff,
                            MoveType = MoveType.Walk,
                            DestinationVoxel = creatureVoxel
                        };

                        agent.AI.Blackboard.SetData("GreedyPath", new List<MoveAction> { nullAction, minAction });
                        var pathAct = new FollowPathAct(agent.AI, "GreedyPath");
                        pathAct.Initialize();

                        foreach (Act.Status status in pathAct.Run())
                        {
                            yield return Act.Status.Running;
                        }
                    }

                    yield return Act.Status.Running;
                }
            }

            public override Act CreateScript(Creature agent)
            {
                return new Select(
                    new GoToVoxelAct("", PlanAct.PlanType.Edge, agent.AI),
                    new Wrap(() => GreedyFallbackBehavior(agent))
                    );
            }

            public override Task Clone()
            {
                return new LeaveWorldTask();
            }
        }

        /// <summary> History of a certain task that keeps track of how many times its failed </summary>
        public class TaskHistory
        {
            /// <summary> Tasks are locked for this amount of time before being tried again </summary>
            public static float LockoutTime;
            /// <summary> If a Task has failed this many times, it will be locked. </summary>
            public static int MaxFailures;
            /// <summary> While the timer is active, the creature will not try to perform the task again. </summary>
            public Timer LockoutTimer;
            /// <summary> Number of times the task has failed </summary>
            public int NumFailures;

            static TaskHistory()
            {
                LockoutTime = 30.0f;
                MaxFailures = 3;
            }

            public TaskHistory()
            {
                NumFailures = 0;
                LockoutTimer = new Timer(LockoutTime, true);
            }

            public bool IsLocked
            {
                get { return NumFailures >= MaxFailures && !LockoutTimer.HasTriggered; }
            }

            public void Update()
            {
                LockoutTimer.Update(DwarfTime.LastTime);
                if (LockoutTimer.HasTriggered)
                {
                    LockoutTimer.Reset(LockoutTime * 1.5f);
                    NumFailures = 0;
                }
            }

        }

        public void TryMoveVelocity(Vector3 desiredDirection, bool jumpCommand)
        {

            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = CharacterMode.Walking;

            float currSpeed = Creature.Physics.Velocity.Length();

            if (currSpeed < 1)
            {
                Creature.CurrentCharacterMode = DwarfCorp.CharacterMode.Idle;
            }

            float force = Creature.Stats.MaxAcceleration * 10;
            if (!Creature.IsOnGround)
            {
                force = Creature.Stats.MaxAcceleration;
                Creature.CurrentCharacterMode = Creature.Physics.Velocity.Y > 0
                    ? DwarfCorp.CharacterMode.Jumping
                    : DwarfCorp.CharacterMode.Falling;
            }


            if (Creature.Physics.IsInLiquid)
            {
                Creature.CurrentCharacterMode = CharacterMode.Swimming;
                Creature.Physics.ApplyForce(Vector3.Up * 10, DwarfTime.Dt);
                force = Creature.Stats.MaxAcceleration*5;
                Creature.NoiseMaker.MakeNoise("Swim", Position);
            }

            Vector3 projectedForce = new Vector3(desiredDirection.X, 0, desiredDirection.Z);

            if (jumpCommand && !jumpHeld && (Creature.IsOnGround || Creature.Physics.IsInLiquid) && Creature.IsHeadClear)
            {
                Creature.NoiseMaker.MakeNoise("Jump", Position);
                Creature.Physics.LocalTransform *= Matrix.CreateTranslation(Vector3.Up*0.01f);
                Creature.Physics.Velocity += Vector3.Up*5;
                Creature.IsOnGround = false;
                Creature.Physics.IsInLiquid = false;
            }
            

            jumpHeld = jumpCommand;

            if (projectedForce.LengthSquared() > 0.001f)
            {
                projectedForce.Normalize();
                Creature.Physics.ApplyForce(projectedForce * force, DwarfTime.Dt);
                Creature.Physics.Velocity = 
                    MathFunctions.ClampXZ(Creature.Physics.Velocity, Creature.Physics.IsInLiquid ? Stats.MaxSpeed * 0.5f: Stats.MaxSpeed);
            }
          
        }
    }
}
