using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp

{

    /// <summary>
    /// Component which manages the AI, scripting, and status of a particular creature (such as a Dwarf or Goblin)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CreatureAI : GameComponent
    {
        public Creature Creature { get; set; }
        public Voxel TargetVoxel { get; set; }
        public Voxel PreviousTargetVoxel { get; set; }
        public List<Voxel> CurrentPath { get; set; }
        public bool DrawPath { get; set; }
        public GatherManager GatherManager { get; set; }
        public Timer IdleTimer { get; set; }
        public List<Thought> Thoughts { get; set; }
        public Timer SpeakTimer { get; set; }
            
        [JsonIgnore]
        public Act CurrentAct { get { return CurrentTask.Script;  }}

        [JsonIgnore]
        public Task CurrentTask { get; set; }

        public Timer PlannerTimer { get; set; }
        public Timer LocalControlTimeout { get; set; }
        public Timer WanderTimer { get; set; }
        public Timer ServiceTimeout { get; set; }
        public bool DrawAIPlan { get; set; }

        public PlanSubscriber PlanSubscriber { get; set; }
        public bool WaitingOnResponse { get; set; }
        public List<string> MessageBuffer = new List<string>();
        public int MaxMessages = 10;
        public EnemySensor Sensor { get; set; }

        [JsonIgnore]
        public Grabber Hands
        {
            get { return Creature.Hands; }
            set { Creature.Hands = value; }
        }

        [JsonIgnore]
        public Physics Physics
        {
            get { return Creature.Physics; }
            set { Creature.Physics = value; }
        }

        [JsonIgnore]
        public Faction Faction
        {
            get { return Creature.Faction; }
            set { Creature.Faction = value; }
        }

        [JsonIgnore]
        public CreatureStats Stats
        {
            get { return Creature.Stats; }
            set { Creature.Stats = value; }
        }

        [JsonIgnore]
        public CreatureStatus Status
        {
            get { return Creature.Status; }
            set { Creature.Status = value; }
        }

        [JsonIgnore]
        public Vector3 Velocity
        {
            get { return Creature.Physics.Velocity; }
            set { Creature.Physics.Velocity = value; }
        }

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

        [JsonIgnore]
        public ChunkManager Chunks
        {
            get { return PlayState.ChunkManager; }
        }

        public Blackboard Blackboard { get; set; }

        public List<Task> Tasks { get; set; }

        public CreatureAI()
        {
            
        }

        public CreatureAI(Creature creature,
            string name,
            EnemySensor sensor,
            PlanService planService) :
                base(name, creature.Physics)
        {
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            Creature = creature;
            TargetVoxel = null;
            CurrentPath = null;
            DrawPath = false;
            PreviousTargetVoxel = null;
            PlannerTimer = new Timer(0.1f, false);
            LocalControlTimeout = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            Creature.Faction.Minions.Add(this);
            DrawAIPlan = false;
            WaitingOnResponse = false;
            PlanSubscriber = new PlanSubscriber(planService);
            ServiceTimeout = new Timer(2, false);
            Sensor = sensor;
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            Sensor.Creature = this;
            CurrentTask = null;
            Tasks = new List<Task>();
            Thoughts = new List<Thought>();
            IdleTimer = new Timer(15.0f, true);
            SpeakTimer = new Timer(5.0f, true);
        }

        public void Say(string message)
        {
            MessageBuffer.Add(message);
            if(MessageBuffer.Count > MaxMessages)
            {
                MessageBuffer.RemoveAt(0);
            }
        }

        private void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            if(enemies.Count > 0)
            {
                foreach(CreatureAI threat in enemies.Where(threat => !Faction.Threats.Contains(threat.Creature)))
                {
                    Faction.Threats.Add(threat.Creature);
                }
            }
        }

        public Task GetEasiestTask(List<Task> tasks)
        {
            if(tasks == null)
            {
                return null;
            }

            float bestCost = float.MaxValue;
            Task bestTask = null;

            foreach(Task task in tasks)
            {
                float cost = task.ComputeCost(Creature);

                if(cost < bestCost)
                {
                    bestCost = cost;
                    bestTask = task;
                }
            }

            return bestTask;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            if(CurrentTask != null && CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();


                bool retried = false;
                if(status == Act.Status.Fail)
                {
                    if(CurrentTask.ShouldRetry(Creature))
                    {
                        Tasks.Add(CurrentTask);
                        CurrentTask.SetupScript(Creature);
                        CurrentTask = ActOnIdle();
                        retried = true;
                    }
                }

                if(status != Act.Status.Running && !retried)
                {
                    CurrentTask = null;
                }
            }
            else
            {
                bool tantrum = false;
                if (Status.Happiness.IsUnhappy())
                {
                    tantrum = MathFunctions.Rand(0, 1) < 0.25f;
                }

                    Task goal = GetEasiestTask(Tasks);
                    if (goal != null)
                    {
                        if (tantrum)
                        {
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Sad);
                            if (Creature.Allies == "Dwarf")
                            {
                                PlayState.AnnouncementManager.Announce(Stats.FirstName + " " + Stats.LastName + " refuses to work!", "Our employee is unhappy, and would rather not work!");
                            }
                            CurrentTask = ActOnIdle();
                        }
                        else
                        {
                            if (goal.IsFeasible(Creature))
                            {
                                IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                                goal.SetupScript(Creature);
                                CurrentTask = goal;
                                Tasks.Remove(goal);
                            }
                            else
                            {
                                Tasks.Remove(goal);
                            }   
                        }
                    }
                    else
                    {
                        CurrentTask = ActOnIdle();
                    }   
                
            }


            PlannerTimer.Update(gameTime);
            UpdateThoughts();
            base.Update(gameTime, chunks, camera);
        }

        public virtual Task ActOnIdle()
        {
            if(GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile())
            {
                if (Status.Energy.IsUnhappy() && PlayState.Time.IsNight())
                {
                    Task toReturn = new SatisfyTirednessTask();
                    toReturn.SetupScript(Creature);
                    return toReturn;
                }

                if (Status.Hunger.IsUnhappy())
                {
                    Task toReturn =  new SatisfyHungerTask();
                    toReturn.SetupScript(Creature);
                    return toReturn;
                }

                List<Room> rooms = Faction.GetRooms();
              
                if (IdleTimer.HasTriggered && rooms.Count > 0 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    return
                        new ActWrapperTask(new GoToZoneAct(this, rooms[PlayState.Random.Next(rooms.Count)]) |
                                           new WanderAct(this, 2, 0.5f, 1.0f));
                }
                else
                {
                    if (IdleTimer.HasTriggered && MathFunctions.Rand(0, 1) < 0.25f)
                    {
                        return new ActWrapperTask(new GoToChairAndSitAct(this));
                    }
                    return new ActWrapperTask(new WanderAct(this, 2, 0.5f, 1.0f));
                }
            }
            else
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                GatherManager.StockOrders.RemoveAt(0);
                return new ActWrapperTask(new StockResourceAct(this, order.Resource));
            }
        }


        public void Jump(GameTime dt)
        {
            if(!Creature.JumpTimer.HasTriggered)
            {
                return;
            }

            Creature.Physics.ApplyForce(Vector3.Up * Creature.Stats.JumpForce, (float) dt.ElapsedGameTime.TotalSeconds);
            Creature.JumpTimer.Reset(Creature.JumpTimer.TargetTimeSeconds);
            SoundManager.PlaySound(ContentPaths.Audio.jump, Creature.Physics.GlobalTransform.Translation);
        }

        public bool HasThought(Thought.ThoughtType type)
        {
            return Thoughts.Any(existingThought => existingThought.Type == type);
        }

        public void AddThought(Thought.ThoughtType type)
        {
            if (!HasThought(type))
            {
                AddThought(Thought.CreateStandardThought(type, PlayState.Time.CurrentDate), true);
            }
        }

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
        }

        public void UpdateThoughts()
        {
            Thoughts.RemoveAll(thought => thought.IsOver(PlayState.Time.CurrentDate));
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
    }

}