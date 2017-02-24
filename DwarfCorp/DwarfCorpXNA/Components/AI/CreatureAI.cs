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
using System.Windows.Forms;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Component which manages the AI, scripting, and status of a particular creature (such as a Dwarf or Goblin)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CreatureAI : GameComponent
    {
        /// <summary> maximum number of messages the creature has in its mind </summary>
        public int MaxMessages = 10;
        /// <summary> As a way of debugging creature AI, creatures can say arbitrary strings which are stored in this buffer </summary>
        public List<string> MessageBuffer = new List<string>();

        public CreatureAI()
        {
            Movement = new CreatureMovement(Creature);
            History = new Dictionary<string, TaskHistory>();
        }

        public CreatureAI(Creature creature,
            string name,
            EnemySensor sensor,
            PlanService planService) :
            base(name, creature.Physics, creature.Manager)
        {
            History = new Dictionary<string, TaskHistory>();
            Movement = new CreatureMovement(creature);
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            Creature = creature;
            CurrentPath = null;
            DrawPath = false;
            PlannerTimer = new Timer(0.1f, false);
            LocalControlTimeout = new Timer(5, false, Timer.TimerMode.Real);
            WanderTimer = new Timer(1, false);
            Creature.Faction.Minions.Add(this);
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
        /// <summary> The creature this AI is controlling </summary>
        public Creature Creature { get; set; }
        /// <summary> The current path of voxels the AI is following </summary>
        public List<Voxel> CurrentPath { get; set; }
        /// <summary> If this is set to true, the creature will draw the path it is following </summary>
        public bool DrawPath { get; set; }
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
        /// <summary> When this timer triggers, the creature will wander in a new direction when it has nothing to do. </sumamry>
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
            set { Creature.Stats = value; }
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
        [JsonIgnore]
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

        /// <summary> Add exprience points to the creature. It will level up from time to time </summary>
        public void AddXP(int amount)
        {
            XPEvents.Add(amount);
        }

        /// <summary> Get the creature to say a debug message. </summary>
        public void Say(string message)
        {
            MessageBuffer.Add(message);
            if (MessageBuffer.Count > MaxMessages)
            {
                MessageBuffer.RemoveAt(0);
            }
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

        /// <summary> Update this creature </summary>
        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!IsActive) return;

            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            OrderEnemyAttack();
            DeleteBadTasks();
            PreEmptTasks();

            // Try to go to sleep if we are low on energy and it is night time.
            if (Status.Energy.IsUnhappy() && Manager.World.Time.IsNight())
            {
                Task toReturn = new SatisfyTirednessTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }

            // Try to find food if we are hungry.
            if (Status.Hunger.IsUnhappy() && Faction.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }


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
            else
            {
                // Throw a tantrum if we're unhappy.
                bool tantrum = false;
                if (Status.Happiness.IsUnhappy())
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
                            Manager.World.MakeAnnouncement(String.Format("{0} ({1}) refuses to workd!",
                                Stats.FullName, Stats.CurrentLevel.Name),
                                "Our employee is unhappy, and would rather not work!", ZoomToMe);
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

            base.Update(gameTime, chunks, camera);
        }

        /// <summary> updates the creature's experience points. </summary>
        public void UpdateXP()
        {
            foreach (int xp in XPEvents)
            {
                Stats.XP += xp;
                string sign = xp > 0 ? "+" : "";

                IndicatorManager.DrawIndicator(sign + xp + " XP",
                    Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, xp > 0 ? Color.Green : Color.Red);
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
            foreach (Voxel vox in Physics.Neighbors)
            {
                if (vox == null) continue;
                if (vox.IsEmpty) continue;
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
            if (!Creature.IsOnGround && !Movement.CanFly && !Creature.Physics.IsInLiquid)
            {
                return new ActWrapperTask(new Wrap(AvoidFalling));
            }

            if (Creature.Physics.IsInLiquid && MathFunctions.RandEvent(0.01f))
            {
                return new FindLandTask();
            }

            if (GatherManager.VoxelOrders.Count == 0 &&
                (GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile()))
            {
                // Find a room to train in
                if (Stats.CurrentClass.HasAction(GameMaster.ToolMode.Attack) && MathFunctions.RandEvent(0.01f))
                {
                    Body closestTraining = Faction.FindNearestItemWithTags("Train", Position, true);

                    if (closestTraining != null)
                    {
                        return new ActWrapperTask(new GoTrainAct(this));
                    }
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
            if (GatherManager.VoxelOrders.Count == 0)
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                GatherManager.StockOrders.RemoveAt(0);
                return new ActWrapperTask(new StockResourceAct(this, order.Resource))
                {
                    Priority = Task.PriorityType.Low
                };
            }
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
                AddThought(Thought.CreateStandardThought(type, Manager.World.Time.CurrentDate), true);
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
            else if (Status.Energy.IsUnhappy())
            {
                AddThought(Thought.ThoughtType.FeltSleepy);
            }

            if (Status.Hunger.IsUnhappy())
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
                            String.Format("{0} is fighting {1}!", Stats.FullName,
                                TextGenerator.IndefiniteArticle(enemy.Creature.Name)),
                            String.Format("{0} the {1} is fighting {2} {3}", Stats.FullName,
                                Stats.CurrentLevel.Name,
                                TextGenerator.IndefiniteArticle(enemy.Stats.CurrentLevel.Name),
                                enemy.Faction.Race.Name),
                            ZoomToMe);
                    }
                }
            }
        }

        /// <summary> Pay the creature this amount of money. The money goes into the creature's wallet. </summary>
        public void AddMoney(float pay)
        {
            Status.Money += pay;
            bool good = pay > 0;
            Color textColor = good ? Color.Green : Color.Red;
            string prefix = good ? "+" : "";
            IndicatorManager.DrawIndicator(prefix + "$" + pay,
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        /// <summary> gets a description of the creature to display to the player </summary>
        public override string GetDescription()
        {
            string desc = Stats.FullName + ", level " + Stats.CurrentLevel.Index +
                          " " +
                          Stats.CurrentClass.Name + "\n    " +
                          "Happiness: " + Status.Happiness.GetDescription() + ". Health: " + Status.Health.Percentage +
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

                    List<Creature.MoveAction> actions = agent.AI.Movement.GetMoveActions(creatureVoxel);

                    float minCost = float.MaxValue;
                    var minAction = new Creature.MoveAction();
                    bool hasMinAction = false;
                    foreach (Creature.MoveAction action in actions)
                    {
                        Voxel vox = action.Voxel;

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
                        var nullAction = new Creature.MoveAction
                        {
                            Diff = minAction.Diff,
                            MoveType = Creature.MoveType.Walk,
                            Voxel = creatureVoxel
                        };

                        agent.AI.Blackboard.SetData("GreedyPath", new List<Creature.MoveAction> { nullAction, minAction });
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
    }

    /// <summary> defines how a creature moves from voxel to voxel </summary>
    public class CreatureMovement
    {
        public CreatureMovement(Creature creature)
        {
            Creature = creature;
            Actions = new Dictionary<Creature.MoveType, ActionStats>
            {
                {
                    Creature.MoveType.Climb,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    Creature.MoveType.Walk,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Swim,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    Creature.MoveType.Jump,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Fly,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Fall,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 5.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.DestroyObject,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 30.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.ClimbWalls,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 30.0f,
                        Speed = 0.5f
                    }
                },
            };
        }

        /// <summary> The creature associated with this AI </summary>
        public Creature Creature { get; set; }

        /// <summary> Wrapper around the creature's fly movement </summary>
        [JsonIgnore]
        public bool CanFly
        {
            get { return Can(Creature.MoveType.Fly); }
            set { SetCan(Creature.MoveType.Fly, value); }
        }

        /// <summary> Wrapper aroound the creature's swim movement </summary>
        [JsonIgnore]
        public bool CanSwim
        {
            get { return Can(Creature.MoveType.Swim); }
            set { SetCan(Creature.MoveType.Swim, value); }
        }

        /// <summary> wrapper around creature's climb movement </summary>
        [JsonIgnore]
        public bool CanClimb
        {
            get { return Can(Creature.MoveType.Climb); }
            set { SetCan(Creature.MoveType.Climb, value); }
        }

        /// <summary> wrapper around creature's climb walls movement </summary>
        [JsonIgnore]
        public bool CanClimbWalls
        {
            get { return Can(Creature.MoveType.ClimbWalls); }
            set { SetCan(Creature.MoveType.ClimbWalls, value); }
        }

        /// <summary> wrapper around creature's walk movement </summary>
        [JsonIgnore]
        public bool CanWalk
        {
            get { return Can(Creature.MoveType.Walk); }
            set { SetCan(Creature.MoveType.Walk, value); }
        }

        /// <summary> List of move actions that the creature can take </summary>
        public Dictionary<Creature.MoveType, ActionStats> Actions { get; set; }

        /// <summary> determines whether the creature can move using the given move type. </summary>
        public bool Can(Creature.MoveType type)
        {
            return Actions[type].CanMove;
        }

        /// <summary> gets the cost of a creature's movement for a particular type </summary>
        public float Cost(Creature.MoveType type)
        {
            return Actions[type].Cost;
        }

        /// <summary> gets the speed multiplier of a creature's movement for a particular type </summary>
        public float Speed(Creature.MoveType type)
        {
            return Actions[type].Speed;
        }

        /// <summary> Sets whether the creature can move using the given type </summary>
        public void SetCan(Creature.MoveType type, bool value)
        {
            Actions[type].CanMove = value;
        }

        /// <summary> sets the cost of moving using a given movement type </summary>
        public void SetCost(Creature.MoveType type, float value)
        {
            Actions[type].Cost = value;
        }

        /// <summary> Sets the movement speed of a particular move type </summary>
        public void SetSpeed(Creature.MoveType type, float value)
        {
            Actions[type].Speed = value;
        }

        public bool IsSessile = false;

        /// <summary> 
        /// Returns a 3 x 3 x 3 voxel grid corresponding to the immediate neighborhood
        /// around the given voxel..
        /// </summary>
        private Voxel[, ,] GetNeighborhood(Voxel voxel)
        {
            var neighborHood = new Voxel[3, 3, 3];
            CollisionManager objectHash = Creature.Manager.World.ComponentManager.CollisionManager;

            VoxelChunk startChunk = voxel.Chunk;
            var x = (int)voxel.GridPosition.X;
            var y = (int)voxel.GridPosition.Y;
            var z = (int)voxel.GridPosition.Z;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    for (int dz = -1; dz < 2; dz++)
                    {
                        neighborHood[dx + 1, dy + 1, dz + 1] = new Voxel();
                        int nx = dx + x;
                        int ny = dy + y;
                        int nz = dz + z;
                        if (
                            !Creature.Manager.World.ChunkManager.ChunkData.GetVoxel(startChunk,
                                new Vector3(nx, ny, nz) + startChunk.Origin,
                                ref neighborHood[dx + 1, dy + 1, dz + 1]))
                        {
                            neighborHood[dx + 1, dy + 1, dz + 1] = null;
                        }
                    }
                }
            }
            return neighborHood;
        }

        /// <summary> Determines whether the voxel has any neighbors in X or Z directions </summary>
        private bool HasNeighbors(Voxel[, ,] neighborHood)
        {
            bool hasNeighbors = false;
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dz = 0; dz < 3; dz++)
                {
                    if (dx == 1 && dz == 1)
                    {
                        continue;
                    }

                    hasNeighbors = hasNeighbors ||
                                   (neighborHood[dx, 1, dz] != null && (!neighborHood[dx, 1, dz].IsEmpty));
                }
            }


            return hasNeighbors;
        }

        /// <summary> Determines whether the given voxel is null or empty </summary>
        private bool IsEmpty(Voxel v)
        {
            return v == null || v.IsEmpty;
        }

        /// <summary> gets a list of actions that the creature can take from the given position </summary>
        public List<Creature.MoveAction> GetMoveActions(Vector3 pos)
        {
            var vox = new Voxel();
            Creature.Manager.World.ChunkManager.ChunkData.GetVoxel(pos, ref vox);
            return GetMoveActions(vox);
        }

        /// <summary> gets the list of actions that the creature can take from a given voxel. </summary>
        public List<Creature.MoveAction> GetMoveActions(Voxel voxel)
        {
            var toReturn = new List<Creature.MoveAction>();

            CollisionManager objectHash = Creature.Manager.World.ComponentManager.CollisionManager;

            Voxel[, ,] neighborHood = GetNeighborhood(voxel);
            var x = (int)voxel.GridPosition.X;
            var y = (int)voxel.GridPosition.Y;
            var z = (int)voxel.GridPosition.Z;
            bool inWater = (neighborHood[1, 1, 1] != null && neighborHood[1, 1, 1].WaterLevel > WaterManager.inWaterThreshold);
            bool standingOnGround = (neighborHood[1, 0, 1] != null && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1] != null && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);
            bool isClimbing = false;

            var successors = new List<Creature.MoveAction>();

            //Climbing ladders.
            IEnumerable<IBoundedObject> objectsInside = objectHash.GetObjectsAt(voxel,
                CollisionManager.CollisionType.Static);
            if (objectsInside != null)
            {
                IEnumerable<GameComponent> bodies = objectsInside.OfType<GameComponent>();
                IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();
                if (CanClimb)
                {
                    bool hasLadder = enumerable.Any(component => component.Tags.Contains("Climbable"));
                    // if the creature can climb objects and a ladder is in this voxel,
                    // then add a climb action.
                    if (hasLadder)
                    {
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = Creature.MoveType.Climb
                        });

                        isClimbing = true;

                        if (!standingOnGround)
                        {
                            successors.Add(new Creature.MoveAction
                            {
                                Diff = new Vector3(1, 0, 1),
                                MoveType = Creature.MoveType.Climb
                            });
                        }

                        standingOnGround = true;
                    }
                }
            }

            // If the creature can climb walls and is not blocked by a voxl above.
            if (CanClimbWalls && !topCovered)
            {
                // Determine if the creature is adjacent to a wall.
                bool nearWall = (neighborHood[2, 1, 1] != null && !neighborHood[2, 1, 1].IsEmpty) ||
                                (neighborHood[0, 1, 1] != null && !neighborHood[0, 1, 1].IsEmpty) ||
                                (neighborHood[1, 1, 2] != null && !neighborHood[1, 1, 2].IsEmpty) ||
                                (neighborHood[1, 1, 0] != null && !neighborHood[1, 1, 0].IsEmpty);

                // If we're near a wall, we can climb upwards.
                if (nearWall)
                {
                    isClimbing = true;
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = Creature.MoveType.ClimbWalls
                    });
                }
                // If we're near a wall and not blocked from below, we can climb downward.
                if (nearWall && !standingOnGround)
                {
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 0, 1),
                        MoveType = Creature.MoveType.ClimbWalls
                    });
                }
            }

            // If the creature either can walk or is in water, add the 
            // eight-connected free neighbors around the voxel.
            if ((CanWalk && standingOnGround) || (CanSwim && inWater))
            {
                // If the creature is in water, it can swim. Otherwise, it will walk.
                Creature.MoveType moveType = inWater ? Creature.MoveType.Swim : Creature.MoveType.Walk;
                if (IsEmpty(neighborHood[0, 1, 1]))
                    // +- x
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[2, 1, 1]))
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 0]))
                    // +- z
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 2]))
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                // Only bother worrying about 8-connected movement if there are
                // no full neighbors around the voxel.
                if (!hasNeighbors)
                {
                    if (IsEmpty(neighborHood[2, 1, 2]))
                        // +x + z
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[2, 1, 0]))
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 2]))
                        // -x -z
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 0]))
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(0, 1, 0),
                            MoveType = moveType
                        });
                }
            }

            // If the creature's head is free, and it is standing on ground,
            // or if it is in water, or if it is climbing, it can also jump
            // to voxels that are 1 cell away and 1 cell up.
            if (!topCovered && (standingOnGround || (CanSwim && inWater) || isClimbing))
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 1 && dz == 1) continue;

                        if (!IsEmpty(neighborHood[dx, 1, dz]))
                        {
                            successors.Add(new Creature.MoveAction
                            {
                                Diff = new Vector3(dx, 2, dz),
                                MoveType = Creature.MoveType.Jump
                            });
                        }
                    }
                }
            }


            // If the creature is not in water and is not standing on ground,
            // it can fall one voxel downward in free space.
            if (!inWater && !standingOnGround)
            {
                successors.Add(new Creature.MoveAction
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = Creature.MoveType.Fall
                });
            }

            // If the creature can fly and is not underwater, it can fly
            // to any adjacent empty cell.
            if (CanFly && !inWater)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        for (int dy = 0; dy <= 2; dy++)
                        {
                            if (dx == 1 && dz == 1 && dy == 1) continue;

                            if (IsEmpty(neighborHood[dx, 1, dz]))
                            {
                                successors.Add(new Creature.MoveAction
                                {
                                    Diff = new Vector3(dx, dy, dz),
                                    MoveType = Creature.MoveType.Fly
                                });
                            }
                        }
                    }
                }
            }

            // Now, validate each move action that the creature might take.
            foreach (Creature.MoveAction v in successors)
            {
                Voxel n = neighborHood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n != null && (n.IsEmpty || n.WaterLevel > 0))
                {
                    // Do one final check to see if there is an object blocking the motion.
                    bool blockedByObject = false;
                    List<IBoundedObject> objectsAtNeighbor = Creature.Manager.CollisionManager.GetObjectsAt(
                        n, CollisionManager.CollisionType.Static);

                    // If there is an object blocking the motion, determine if it can be passed through.
                    if (objectsAtNeighbor != null)
                    {
                        IEnumerable<GameComponent> bodies = objectsAtNeighbor.OfType<GameComponent>();
                        IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();

                        foreach (GameComponent body in enumerable)
                        {
                            Door door = body.GetEntityRootComponent().GetChildrenOfType<Door>(true).FirstOrDefault();
                            // If there is an enemy door blocking movement, we can destroy it to get through.
                            if (door != null)
                            {
                                if (
                                    Creature.Manager.Diplomacy.GetPolitics(door.TeamFaction, Creature.Faction)
                                        .GetCurrentRelationship() !=
                                    Relationship.Loving)
                                {
                                    if (Can(Creature.MoveType.DestroyObject))
                                        toReturn.Add(new Creature.MoveAction
                                        {
                                            Diff = v.Diff,
                                            MoveType = Creature.MoveType.DestroyObject,
                                            InteractObject = door,
                                            Voxel = n
                                        });
                                    blockedByObject = true;
                                }
                            }
                        }
                    }
                    // If no object blocked us, we can move freely as normal.
                    if (!blockedByObject)
                    {
                        Creature.MoveAction newAction = v;
                        newAction.Voxel = n;
                        toReturn.Add(newAction);
                    }
                }
            }

            // Return the list of all validated actions that the creature can take.
            return toReturn;
        }
        /// <summary> Each action has a cost, a speed, and a validity check </summary>
        public class ActionStats
        {
            public bool CanMove = false;
            public float Cost = 1.0f;
            public float Speed = 1.0f;
        }
    }
}
