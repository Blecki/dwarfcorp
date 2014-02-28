using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CreatureAIComponent : GameComponent
    {
        public Creature Creature { get; set; }
        public Designation TargetVoxDesignation { get; set; }
        public Stockpile TargetStockpile { get; set; }
        public Room TargetRoom { get; set; }
        public VoxelBuildDesignation TargetBuildDesignation { get; set; }
        public List<string> DesiredTags { get; set; }
        public LocatableComponent TargetComponent { get; set; }
        public VoxelRef TargetVoxel { get; set; }
        public VoxelRef PreviousTargetVoxel { get; set; }
        public List<VoxelRef> CurrentPath { get; set; }
        public bool DrawPath { get; set; }
        public GatherManager GatherManager { get; set; }


        [JsonIgnore]
        public Act CurrentAct { get; set; }

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
        public PhysicsComponent Physics
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

        public CreatureAIComponent()
        {
            
        }

        public CreatureAIComponent(Creature creature,
            string name,
            EnemySensor sensor,
            PlanService planService) :
                base(creature.Manager, name, creature.Physics)
        {
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            Creature = creature;
            TargetVoxel = null;
            CurrentPath = null;
            DrawPath = false;
            TargetComponent = null;
            TargetStockpile = null;
            TargetRoom = null;
            PreviousTargetVoxel = null;
            TargetBuildDesignation = null;
            DesiredTags = new List<string>();
            TargetVoxDesignation = null;
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
            CurrentAct = null;
            Tasks = new List<Task>();
        }

        public void Say(string message)
        {
            MessageBuffer.Add(message);
            if(MessageBuffer.Count > MaxMessages)
            {
                MessageBuffer.RemoveAt(0);
            }
        }

        private void Sensor_OnEnemySensed(List<CreatureAIComponent> enemies)
        {
            if(enemies.Count > 0)
            {
                Say("Sensed " + enemies.Count + " enemies");

                foreach(CreatureAIComponent threat in enemies.Where(threat => !Faction.Threats.Contains(threat.Creature)))
                {
                    Faction.Threats.Add(threat.Creature);
                }
            }
        }


        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();

                if(status != Act.Status.Running)
                {
                    CurrentAct = null;
                }
            }
            else
            {
                Task goal = Tasks.FirstOrDefault();
                if(goal != null)
                {
                    Tasks.RemoveAt(0);

                    if(goal.IsFeasible(Creature))
                    {
                        CurrentAct = goal.CreateScript(Creature);
                    }
                }
                else
                {
                    CurrentAct = ActOnIdle();
                }
            }


            PlannerTimer.Update(gameTime);

            base.Update(gameTime, chunks, camera);
        }

        public Act ActOnIdle()
        {
            if(GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile())
            {
                return new WanderAct(this, 2, 0.5f, 1.0f);
            }
            else
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                GatherManager.StockOrders.RemoveAt(0);
                return new StockResourceAct(this, order.Resource);
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




    }

}