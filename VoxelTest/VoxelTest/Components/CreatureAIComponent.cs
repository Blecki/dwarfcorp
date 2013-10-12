using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace DwarfCorp

{

    public class CreatureStats
    {
        public float MaxSpeed { get; set; }
        public float MaxAcceleration { get; set; }
        public float StoppingForce { get; set; }
        public float BaseDigSpeed { get; set; }
        public float JumpForce { get; set;}
        public float BaseChopSpeed { get; set; }
        public float MaxHealth { get; set;}
        public float EnergyRecharge { get; set; }
        public float EnergyRechargeBed { get; set; }
        public float EnergyLoss { get; set; }
        public float SleepyThreshold { get; set; }
        public float HungerIncrease { get; set; }
        public float HungerThreshold { get; set; }
        public float WanderRadius { get; set; }
        public float PlanRateLimit { get; set; }
        public int MaxExpansions { get; set; }
    }

    public class CreatureStatus
    {
        public float Energy { get; set; }
        public float Hunger { get; set; }
        public float Thirst { get; set; }

        public CreatureStatus()
        {
            Energy = 1.0f;
            Hunger = 0.0f;
            Thirst = 0.0f;
        }
    }


    public class CreatureAIComponent : GameComponent
    {
        public Creature Creature { get; set; }
        public GameMaster.Designation TargetVoxDesignation { get; set; }
        public Stockpile TargetStockpile { get; set; }
        public Room TargetRoom { get; set; }
        public VoxelBuildDesignation TargetBuildDesignation { get; set; }
        public List<string> DesiredTags { get; set; }
        public LocatableComponent TargetComponent { get; set; }
        public VoxelRef TargetVoxel { get; set; }
        public VoxelRef PreviousTargetVoxel { get; set; }
        public List<VoxelRef> CurrentPath { get; set; }
        public bool DrawPath { get; set; }
        public InteractiveComponent InteractingWith { get; set; }
        public GOAP Goap { get; set; }

        public Act CurrentAct { get; set; }

        public Goal CurrentGoal { get; set; }
        public int CurrentActionIndex { get; set; }
        public List<Action> CurrentActionPlan { get; set; }
        public Timer PlannerTimer { get; set; }
        public Timer LocalControlTimeout { get; set; }
        public Timer WanderTimer { get;set;}
        public Timer ServiceTimeout { get; set; }
        public bool DrawAIPlan { get; set; }
        public PlanSubscriber PlanSubscriber { get; set; }
        public bool WaitingOnResponse { get; set; }
        public List<string> MessageBuffer = new List<string>();
        public int MaxMessages = 10;
        public EnemySensor Sensor { get; set; }
        public Grabber Hands { get { return Creature.Hands; } set { Creature.Hands = value; } }
        public PhysicsComponent Physics { get { return Creature.Physics; } set { Creature.Physics = value; } }
        public GameMaster Master { get { return Creature.Master; } set { Creature.Master = value; } }
        public CreatureStats Stats { get { return Creature.Stats; } set { Creature.Stats = value; } }
        public CreatureStatus Status { get { return Creature.Status; } set { Creature.Status = value; } }

        public Vector3 Velocity { get { return Creature.Physics.Velocity; } set { Creature.Physics.Velocity = value; } }
        public Vector3 Position { get { return Creature.Physics.GlobalTransform.Translation; } }
        public ChunkManager Chunks { get { return Creature.Master.Chunks; } }

        public Blackboard Blackboard { get; set; }

        public CreatureAIComponent(Creature creature,
                                   string name,
                                   EnemySensor sensor,
                                   PlanService planService) :
            base(creature.Manager, name, creature.Physics)
        {
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
            InteractingWith = null;
            Goap = new GOAP(this);
            CurrentGoal = null;
            CurrentActionIndex = -1;
            CurrentActionPlan = null;
            PlannerTimer = new Timer(0.1f, false);
            LocalControlTimeout = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            Creature.Master.Minions.Add(this);
            DrawAIPlan = false;
            WaitingOnResponse = false;
            PlanSubscriber = new PlanSubscriber(planService);
            ServiceTimeout = new Timer(2, false);
            Sensor = sensor;
            Sensor.OnEnemySensed += new EnemySensor.EnemySensed(Sensor_OnEnemySensed);
            Sensor.Creature = this;
            CurrentAct = null;
          
        }

        public void Say(string message)
        {
            MessageBuffer.Add(message);
            if (MessageBuffer.Count > MaxMessages)
            {
                MessageBuffer.RemoveAt(0);
            }
        }

        void Sensor_OnEnemySensed(List<CreatureAIComponent> enemies)
        {
            if (enemies.Count > 0)
            {
                Goap.Belief[GOAPStrings.SenseEnemy] = true;

                Say("Sensed " + enemies.Count + " enemies");

                foreach(CreatureAIComponent creature in enemies)
                {
                    if (creature.IsDead)
                    {
                        continue;
                    }

                    Goal goal = new KillEntity(Goap, creature.Creature.Physics);
                    if (!Goap.Goals.ContainsKey(goal.Name))
                    {
                        Goap.AddGoal(goal);
                    }
                }
            }
            else
            {
                Goap.Belief[GOAPStrings.SenseEnemy] = false;
            }
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {

            if (CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();

                if (status != Act.Status.Running)
                {
                    CurrentAct = null;
                }
            }
            else
            {
                Goal goal = Goap.GetHighestPriorityGoal();
                if (goal != null)
                {
                    CurrentAct = goal.GetBehaviorTree(this);
                    Goap.Goals.Remove(goal.Name);
                }
            }
            
            /*
            if (CurrentGoal == null || (!WaitingOnResponse && CurrentGoal != null && CurrentActionPlan == null))
            {
                ReplanGOAP();
            }

            PerformCurrentGOAPAction(chunks, gameTime);
            */
             
            PlannerTimer.Update(gameTime);
            CheckPlanSubscriber(gameTime);

            base.Update(gameTime, chunks, camera);
        }


   

        public void CheckPlanSubscriber(GameTime dt)
        {
            if (WaitingOnResponse)
            {
                ServiceTimeout.Update(dt);

                if (ServiceTimeout.HasTriggered)
                {
                    Say("Stopped waiting on response.");
                    WaitingOnResponse = false;

                    if (CurrentGoal != null)
                    {
                        Goap.Goals.Remove(CurrentGoal.Name);
                        CurrentGoal = null;
                    }
                }
            }

            while (PlanSubscriber.AStarPlans.Count > 0)
            {
                PlanService.AStarPlanResponse res = null;
                while(!PlanSubscriber.AStarPlans.TryDequeue(out res)) { ; }
                CurrentPath = res.path;
                if (CurrentPath != null && CurrentPath.Count > 0)
                {
                    Say("Got an A* path of length " + CurrentPath.Count);
                    TargetVoxel = CurrentPath[0];
                }
                else
                {
                    Say("A* Path was null!");
                    ResetBrain();
                    
                }

                WaitingOnResponse = false;
                ServiceTimeout.Reset(ServiceTimeout.TargetTimeSeconds);
               
            }

            while (PlanSubscriber.GoapPlans.Count > 0)
            {
                PlanService.GoapPlanResponse res = null;
                while (!PlanSubscriber.GoapPlans.TryDequeue(out res)) { ; }
                CurrentActionPlan = res.path;

                if (res.path != null)
                {
                    Say("Got a GOAP path of length " + res.path.Count);
                    CurrentActionIndex = 0;
                }
                else
                {
                    Say("GOAP path was null!");
                    ResetBrain();
                }

                WaitingOnResponse = false;
                ServiceTimeout.Reset(ServiceTimeout.TargetTimeSeconds);
            }
           
    

        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {

            if (DrawAIPlan)
            {
                if (CurrentActionIndex != -1 && CurrentActionPlan != null)
                {
                    int i = 0;
                    string actionString = "{\n";
                    foreach (Action a in CurrentActionPlan)
                    {
                        if (i == CurrentActionIndex)
                        {
                            actionString += "> ";
                        }

                        actionString += "    " + a.Name;
                        actionString += "\n";
                        i++;
                    }
                    actionString += "}";

                    Drawer2D.DrawText("Goal: " + CurrentGoal.Name + "\n" + actionString + "\n", Creature.Physics.GlobalTransform.Translation + new Vector3(0, 0.5f, 0), Color.White, Color.Black);
                }
                else if (CurrentGoal != null && CurrentActionPlan == null)
                {
                    if (!WaitingOnResponse)
                    {
                        Drawer2D.DrawText(CurrentGoal.Name, Creature.Physics.GlobalTransform.Translation + new Vector3(0, 0.5f, 0), Color.Yellow, Color.Black);
                    }
                    else
                    {
                        Drawer2D.DrawText(CurrentGoal.Name + ServiceTimeout.CurrentTimeSeconds, Creature.Physics.GlobalTransform.Translation + new Vector3(0, 0.5f, 0), Color.Red, Color.Black);
                    }
                }
                else if (CurrentActionPlan != null)
                {
                    Drawer2D.DrawText("???", Creature.Physics.GlobalTransform.Translation + new Vector3(0, 0.5f, 0), Color.White, Color.Red);
                }
            }
             
           

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }


        #region GOAPING
        
        public void PerformCurrentGOAPAction(ChunkManager chunks, GameTime gameTime)
        {
            if (CurrentActionPlan != null)
            {
                if (CurrentActionIndex >= 0 && CurrentActionIndex < CurrentActionPlan.Count)
                {
                    Action currentAction = CurrentActionPlan[CurrentActionIndex];

                    Action.PerformStatus status = currentAction.PerformContextAction(this, gameTime);

                    if (status == Action.PerformStatus.Failure || status == Action.PerformStatus.Invalid)
                    {
                        Say("Action " + currentAction.Name + " failed! Replanning.");

                        if (CurrentGoal != null) { Goap.Goals.Remove(CurrentGoal.Name); }
                        CurrentGoal = null;
                        CurrentActionPlan = null;
                        CurrentActionIndex = -1;

                        ReplanGOAP();
                        
                    }
                    else if (status == Action.PerformStatus.Success)
                    {
                        currentAction.Apply(Goap.Belief);
                        CurrentActionIndex++;

                        Say("Action " + currentAction.Name + " success.");

                        if (CurrentActionIndex >= 0 && CurrentActionIndex < CurrentActionPlan.Count)
                        {
                            Action nextAction = CurrentActionPlan[CurrentActionIndex];

                            Action.ValidationStatus validate = nextAction.ContextValidate(this);

                            if (validate == Action.ValidationStatus.Replan)
                            {
                                Say("Could not validate action. Replanning.");             
                                ReplanGOAP();
                            }
                            else if (validate == Action.ValidationStatus.Invalid)
                            {
                                Say("Could not validate action. Replanning.");    
                                ReplanGOAP();
                            }
                        }
                    }

                }
            }

            if (CurrentActionPlan != null && CurrentActionIndex >= CurrentActionPlan.Count)
            {
                Say("Resetting brain."); 
                ResetBrain();
                ReplanGOAP();
            }
        }

        public void ResetBrain()
        {
            CurrentActionPlan = null;
            CurrentActionIndex = -1;
            if (!(CurrentGoal is CompoundGoal) && CurrentGoal != null)
            {
                Goap.Goals.Remove(CurrentGoal.Name);
            }
            else
            {
                CurrentGoal = null;
            }
            CurrentGoal = null;
            Goap.Belief[GOAPStrings.TargetDead] = false;
            Goap.Belief[GOAPStrings.TargetVoxel] = null;
            Goap.Belief[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Goap.Belief[GOAPStrings.TargetEntity] = null;
            Goap.Belief[GOAPStrings.TargetZone] = null;
            Goap.Belief[GOAPStrings.TargetTags] = null;
            Goap.Belief[GOAPStrings.ZoneTags] = null;

            TargetVoxDesignation = null;
            TargetVoxel = null;
            TargetComponent = null;
            CurrentPath = null;
        }

        public void ReplanGOAP()
        {
            if (CurrentGoal == null && Goap.Goals.Count > 0)
            {
                ResetBrain();
                UpdateGOAPBelief();
                
                Goap.Zones.Clear();
                Goap.Items.Clear();
                Goap.Voxels.Clear();


                foreach (Goal g in Goap.Goals.Values)
                {
                    if (g is KillEntity)
                    {
                        Goap.Items.Add(((KillEntity)(g)).Item);
                    }
                }


                foreach (Room room in Creature.Master.RoomDesignator.DesignatedRooms)
                {
                    Goap.Zones.Add(room);

                    if (room.RoomType.Name == "BedRoom")
                    {
                        List<LocatableComponent> beds = room.GetComponentsInRoomContainingTag("Bed");

                        foreach (LocatableComponent bed in beds)
                        {
                            List<BedComponent> interactivebeds = bed.GetChildrenOfTypeRecursive<BedComponent>();
                            Goap.Items.Add(new BedItem("Bed " + bed.GlobalID, room, bed, interactivebeds[0], 1.0f));
                        }
                    }
                }


                foreach (Stockpile stockpile in Creature.Master.Stockpiles)
                {
                    Goap.Zones.Add(stockpile);

                    foreach (Item component in stockpile.ListItems())
                    {
                        if (!Goap.Items.Contains(component))
                        {
                            Goap.Items.Add(component);
                        }
                    }
                }

                foreach (GameMaster.Designation des in Creature.Master.DigDesignations)
                {
                    Goap.Voxels.Add(des.vox.GetReference());
                }

                foreach (GameMaster.Designation des in Creature.Master.GuardDesignations)
                {
                    Goap.Voxels.Add(des.vox.GetReference());
                }

                foreach (LocatableComponent grab in Creature.Hands.GrabbedComponents.Keys)
                {
                    Goap.Items.Add(Item.CreateItem(grab.Name + " " + grab.GlobalID, null, grab));
                }

                foreach (LocatableComponent des in Creature.Master.GatherDesignations)
                {
                    Goap.Items.Add(Item.CreateItem(des.Name + " " + des.GlobalID, null, des));
                }

                foreach (LocatableComponent des in Creature.Master.ChopDesignations)
                {
                    Goap.Items.Add(Item.CreateItem(des.Name + " " + des.GlobalID, null, des));
                }


                foreach (RoomBuildDesignation buildDesignation in Creature.Master.RoomDesignator.BuildDesignations)
                {

                    if (!buildDesignation.IsBuilt)
                    {
                        HashSet<string> strings = new HashSet<string>();
                        foreach (string item in buildDesignation.ToBuild.RoomType.RequiredResources.Keys)
                        {
                            strings.Add(item);
                        }

                        //Goap.AddGoal(new PutItemWithTag(Goap, new TagList(strings), buildDesignation.ToBuild));
                    }
                }

                foreach (Item i in Item.ItemDictionary.Values)
                {
                    if (!Goap.Items.Contains(i) && !i.userData.IsDead && (i.reservedFor == null || i.reservedFor == this))
                    {
                        Goap.Items.Add(i);
                    }
                }


                Goap.Actions.Clear();

                bool reservedPutDesignation = false;
                foreach (PutDesignation put in Creature.Master.PutDesignator.Designations)
                {
                    Goap.AddAction(new ConstructVoxel(put.vox));
                    Goap.Voxels.Add(put.vox);
                    //Goap.AddGoal(new BuildVoxel(Goap, new TagList(put.type.resourceToRelease), put.vox, put.type));

                    /*
                    if (put.reservedCreature == null && !reservedPutDesignation)
                    {
                        put.reservedCreature = this;
                        reservedPutDesignation = true;
                    }
                     */
                }

                Goap.CreateGeneralActions();
                Goap.CreateItemActions();
                Goap.CreateZoneActions();


                foreach(Goal g in Goap.Goals.Values)
                {
                    g.ContextReweight(this);
                }



                CurrentGoal = Goap.GetHighestPriorityGoal();

                

                if (CurrentGoal is CompoundGoal)
                {
                    CompoundGoal comp = (CompoundGoal)CurrentGoal;

                    comp.CurrentGoalIndex++;

                    if (comp.CurrentGoalIndex >= comp.Goals.Count)
                    {
                        comp.CurrentGoalIndex = 0;
                        CurrentGoal = Goap.GetHighestPriorityGoal();
                    }
                }


                if (CurrentGoal != null && ! WaitingOnResponse)
                {

                    if (!CurrentGoal.ContextValidate(this))
                    {
                        Goap.Goals.Remove(CurrentGoal.Name);
                        CurrentGoal = null;
                    }
                    else
                    {
                        CurrentGoal.Reset(Goap);
                        //CurrentActionPlan = Goap.PlanToGoal(CurrentGoal);
                        CurrentActionPlan = null;
                        
                        PlanService.GoapPlanRequest gpr = new PlanService.GoapPlanRequest();
                        gpr.goal = CurrentGoal;
                        gpr.sender = this;
                        gpr.start = Goap.Belief;
                        gpr.subscriber = PlanSubscriber;

                        PlanSubscriber.SendRequest(gpr);
                        WaitingOnResponse = true;
                    }

                }

                if (CurrentActionPlan != null && CurrentActionPlan.Count > 0)
                {
                    CurrentActionIndex = 0;
                }
                
            }
            else if (Goap.Goals.Count == 0)
            {
                Goap.Goals.Add("LookInteresting", new LookInteresting(Goap));
                //Goap.Goals.Add("SatisfyHunger", new SatisfyHunger(Goap));
                //Goap.Goals.Add("SatisfySleepiness", new SatisfySleepiness(Goap));
            }

            if (Goap.Belief.Specification.ContainsKey(GOAPStrings.IsSleepy) && (bool)Goap.Belief[GOAPStrings.IsSleepy])
            {
                if (!Goap.Goals.ContainsKey("SatisfySleepiness"))
                {
                    //Goap.Goals.Add("SatisfySleepiness", new SatisfySleepiness(Goap));
                }
            }

            if (Goap.Belief.Specification.ContainsKey(GOAPStrings.IsHungry) && (bool)Goap.Belief[GOAPStrings.IsHungry])
            {
                if (!Goap.Goals.ContainsKey("SatisfyHunger"))
                {
                    //Goap.Goals.Add("SatisfyHunger", new SatisfyHunger(Goap));
                }
            }

            if (Goap.Belief.Specification.ContainsKey(GOAPStrings.MotionStatus) && (GOAP.MotionStatus)Goap.Belief[GOAPStrings.MotionStatus] == GOAP.MotionStatus.Stationary)
            {
                if (!Goap.Goals.ContainsKey("LookInteresting"))
                {
                    Goap.Goals.Add("LookInteresting", new LookInteresting(Goap));
                }
            }


            if (!WaitingOnResponse && CurrentActionPlan == null && CurrentGoal != null)
            {
                Goap.Goals.Remove(CurrentGoal.Name);
                CurrentGoal = null;
            }
        }

        public void UpdateGOAPBelief()
        {

            if (Creature.Status.Energy < Creature.Stats.SleepyThreshold)
            {
                Goap.Belief[GOAPStrings.IsSleepy] = true;
            }
            else
            {
                Goap.Belief[GOAPStrings.IsSleepy] = false;
            }

            if (Creature.Status.Hunger > Creature.Stats.HungerThreshold)
            {
                Goap.Belief[GOAPStrings.IsHungry] = true;
            }
            else
            {
                Goap.Belief[GOAPStrings.IsHungry] = false;
            }

            if (Creature.Hands.IsFull())
            {
                Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Full;
                Goap.Belief[GOAPStrings.HeldObject] = Item.CreateItem(Creature.Hands.GetFirstGrab().Name + " " + Creature.Hands.GetFirstGrab().GlobalID, null, Creature.Hands.GetFirstGrab());
                Goap.Belief[GOAPStrings.HeldItemTags] = new TagList(Creature.Hands.GetFirstGrab().Tags);
            }
            else
            {
                Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Empty;
                Goap.Belief[GOAPStrings.HeldObject] = null;
                Goap.Belief[GOAPStrings.HeldItemTags] = null;
            }

            if (TargetRoom != null)
            {
                Goap.Belief[GOAPStrings.TargetZone] = TargetRoom;
                Goap.Belief[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
                Goap.Belief[GOAPStrings.TargetDead] = false;
                Goap.Belief[GOAPStrings.ZoneTags] = new TagList(TargetRoom.RoomType.Name);

                if (TargetRoom.IsInZone(Creature.Physics.GlobalTransform.Translation))
                {
                    Goap.Belief[GOAPStrings.AtTarget] = true;
                    Goap.Belief[GOAPStrings.CurrentZone] = TargetRoom;

                    Goap.Belief[GOAPStrings.TargetZoneType] = "Room";

                }
                else
                {
                    Goap.Belief[GOAPStrings.AtTarget] = false;
                    Goap.Belief[GOAPStrings.CurrentZone] = null;
                    Goap.Belief[GOAPStrings.TargetZoneType] = null;
                }
            }
            else if (TargetVoxel != null && TargetComponent == null)
            {
                Voxel vox = TargetVoxel.GetVoxel(Creature.Master.Chunks, true);
                Goap.Belief[GOAPStrings.TargetVoxel] = TargetVoxel;
                Goap.Belief[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
                Goap.Belief[GOAPStrings.TargetDead] = vox.Health > 0;
                Goap.Belief[GOAPStrings.ZoneTags] = null;
                Vector3 diff = TargetVoxel.WorldPosition - Creature.Physics.GlobalTransform.Translation;

                if (diff.Length() < 1)
                {
                    Goap.Belief[GOAPStrings.AtTarget] = true;
                }
                else
                {
                    Goap.Belief[GOAPStrings.AtTarget] = false;
                }

            }
            else if (TargetComponent != null)
            {
                Goap.Belief[GOAPStrings.TargetEntity] = TargetComponent;
                Goap.Belief[GOAPStrings.TargetType] = GOAP.TargetType.Entity;
                Goap.Belief[GOAPStrings.TargetDead] = TargetComponent.IsDead;
                Goap.Belief[GOAPStrings.TargetTags] = new TagList(TargetComponent.Tags);

                Vector3 diff = TargetComponent.GlobalTransform.Translation - Creature.Physics.GlobalTransform.Translation;

                if (diff.Length() < 1)
                {
                    Goap.Belief[GOAPStrings.AtTarget] = true;
                }
                else
                {
                    Goap.Belief[GOAPStrings.AtTarget] = false;
                }

            }
            else
            {
                Goap.Belief[GOAPStrings.TargetType] = GOAP.TargetType.None;
                Goap.Belief[GOAPStrings.TargetVoxel] = null;
                Goap.Belief[GOAPStrings.TargetEntity] = null;
                Goap.Belief[GOAPStrings.TargetZone] = null;
                Goap.Belief[GOAPStrings.AtTarget] = false;
                Goap.Belief[GOAPStrings.TargetDead] = false;
                Goap.Belief[GOAPStrings.TargetEntityInZone] = false;
                Goap.Belief[GOAPStrings.CurrentZone] = null;
                Goap.Belief[GOAPStrings.TargetZoneType] = null;
                Goap.Belief[GOAPStrings.TargetZoneFull] = false;
            }

            if (Creature.Physics.Velocity.LengthSquared() < 0.5f)
            {
                Goap.Belief[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            }
            else
            {
                Goap.Belief[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Moving;
            }
        }
        
        #endregion 

        #region ACTING
        public enum PlannerSuccess
        {
            Success,
            Failure,
            Wait
        }



        public PlannerSuccess Wander(GameTime gameTime, float radius)
        {

            if (WanderTimer.Update(gameTime) || WanderTimer.HasTriggered)
            {
                Creature.LocalTarget = new Vector3((float)PlayState.random.NextDouble() * radius - radius / 2.0f, 0.0f, (float)PlayState.random.NextDouble() * radius - radius / 2.0f) + Creature.Physics.GlobalTransform.Translation;
            }

            Vector3 output = Creature.Controller.GetOutput((float)gameTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
            output.Y = 0.0f;

            Creature.Physics.ApplyForce(output, (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (output.LengthSquared() > 0.5f)
            {
                return PlannerSuccess.Wait;
            }
            else
            {
                return PlannerSuccess.Success;
            }
        }


        public PlannerSuccess PlanPath(GameTime gameTime)
        {
            if (CurrentPath != null)
            {
                return PlannerSuccess.Success;
            }

            ChunkManager chunks = Creature.Master.Chunks;
            if (PlannerTimer.HasTriggered)
            {

                Voxel vox = chunks.GetFirstVisibleBlockUnder(Creature.Physics.GlobalTransform.Translation, true);
                List<VoxelRef> voxAbove = new List<VoxelRef>();
                chunks.GetVoxelReferencesAtWorldLocation(null, vox.Position + new Vector3(0, 1, 0), voxAbove);

                if (TargetVoxel == null)
                {
                    return PlannerSuccess.Failure;
                }

                if (voxAbove.Count > 0)
                {
                    CurrentPath = null; // AStarPlanner.FindPath(voxAbove[0], TargetVoxel.GetReference(), chunks, 500);

                    PlanService.AstarPlanRequest aspr = new PlanService.AstarPlanRequest();
                    aspr.subscriber = PlanSubscriber;
                    aspr.start = voxAbove[0];
                    aspr.goal = TargetVoxel;
                    aspr.maxExpansions = 20000;
                    aspr.sender = this;

                    PlanSubscriber.SendRequest(aspr);
                    PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);

                    return PlannerSuccess.Wait;
                }
                else
                {
                    CurrentPath = null;
                    return PlannerSuccess.Failure;
                }


 
            }
            else
            {
                return PlannerSuccess.Wait;
            }
        }

        public void Jump(GameTime dt)
        {
            if (Creature.JumpTimer.HasTriggered)
            {
                Creature.Physics.ApplyForce(Vector3.Up * Creature.Stats.JumpForce, (float)dt.ElapsedGameTime.TotalSeconds);
                Creature.JumpTimer.Reset(Creature.JumpTimer.TargetTimeSeconds);
                SoundManager.PlaySound("jump", Creature.Physics.GlobalTransform.Translation);
            }

        }
       
        public PlannerSuccess Pathfind(GameTime gameTime)
        {
            ChunkManager chunks = Creature.Master.Chunks;
            if (CurrentPath == null)
            {
                CurrentPath = null;
                return PlannerSuccess.Failure;
            }

            if (TargetVoxel != null)
            {
                LocalControlTimeout.Update(gameTime);

                if (LocalControlTimeout.HasTriggered)
                {
                    return PlannerSuccess.Failure;
                }


                if (PreviousTargetVoxel == null)
                {
                    Creature.LocalTarget = TargetVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    Creature.LocalTarget = LinearMathHelpers.ClosestPointToLineSegment(Creature.Physics.GlobalTransform.Translation, PreviousTargetVoxel.WorldPosition, TargetVoxel.WorldPosition, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f); 
                }

                Vector3 output = Creature.Controller.GetOutput((float)gameTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
                Creature.Physics.ApplyForce(output, (float)gameTime.ElapsedGameTime.TotalSeconds);
                output.Y = 0.0f;

                if ((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                {
                    Jump(gameTime);
                }


                if (DrawPath)
                {
                    List<Vector3> points = new List<Vector3>();
                    foreach (VoxelRef v in CurrentPath)
                    {
                        points.Add(v.WorldPosition + new Vector3(0.5f, 0.5f, 0.2f));
                    }

                    SimpleDrawing.DrawLineList(points, Color.Red, 0.1f);
                }

                if ((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Length() < 0.8f || CurrentPath.Count < 2)
                {
                    if (CurrentPath != null && CurrentPath.Count > 1)
                    {
                        PreviousTargetVoxel = TargetVoxel;
                        CurrentPath.RemoveAt(0);
                        LocalControlTimeout.Reset(LocalControlTimeout.TargetTimeSeconds);
                        TargetVoxel = CurrentPath[0];
                    }
                    else
                    {
                        PreviousTargetVoxel = null;
                        CurrentPath = null;
                        return PlannerSuccess.Success;
                    }
                }
            }
            else
            {
                PreviousTargetVoxel = null;
                return PlannerSuccess.Failure;
            }

            return PlannerSuccess.Wait;
        }

        public PlannerSuccess Stop(GameTime gameTime)
        {
            if (Creature.Physics.Velocity.LengthSquared() < 0.5f)
            {
                return PlannerSuccess.Success;
            }
            else
            {
                Creature.Physics.Velocity *= 0.8f;
                return PlannerSuccess.Wait;
            }
        }

        public PlannerSuccess Dig(GameTime gameTime)
        {
            Voxel vox = TargetVoxel.GetVoxel(Creature.Master.Chunks, false);
            if (vox == null || vox.Health <= 0.0f || !Creature.Master.IsDigDesignation(vox))
            {
                if (vox != null && vox.Health <= 0.0f)
                {
                    vox.Kill();
                }
                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                return PlannerSuccess.Success;
            }
            else 
            {
                Creature.LocalTarget = vox.Position + new Vector3(0.5f, 0.5f, 0.5f);
                Vector3 output = Creature.Controller.GetOutput((float)gameTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
                Creature.Physics.ApplyForce(output, (float)gameTime.ElapsedGameTime.TotalSeconds);
                output.Y = 0.0f;

                if ((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                {
                    Jump(gameTime);
                }

                Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.5f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.5f);
                vox.Health -= Creature.Stats.BaseDigSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

                Creature.CurrentCharacterMode = DwarfCorp.Creature.CharacterMode.Attacking;
                Creature.Weapon.PlayNoise();

                return PlannerSuccess.Wait;
            }


        }

        public PlannerSuccess MeleeAttack(GameTime gameTime)
        {

            Creature.LocalTarget = new Vector3(TargetComponent.GlobalTransform.Translation.X,
                                                Creature.Physics.GlobalTransform.Translation.Y,
                                                TargetComponent.GlobalTransform.Translation.Z);


            Vector3 diff = Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation;

            Creature.Physics.Face(Creature.LocalTarget);
           

            if (diff.Length() > 1.0f)
            {

                Vector3 output = Creature.Controller.GetOutput((float)gameTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                Creature.Physics.ApplyForce(output, (float)gameTime.ElapsedGameTime.TotalSeconds);
                output.Y = 0.0f;

                if ((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                {
                    Jump(gameTime);
                }
                Creature.Physics.OrientWithVelocity = true;
            }
            else
            {
                Creature.Physics.OrientWithVelocity = false;
                Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
            }

            List<HealthComponent> healths = TargetComponent.GetChildrenOfTypeRecursive<HealthComponent>();

            foreach (HealthComponent health in healths)
            {
                health.Damage(Creature.Stats.BaseChopSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (TargetComponent.IsDead)
            {
                Creature.Master.ChopDesignations.Remove(TargetComponent);
                TargetComponent = null;

                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                Creature.Physics.OrientWithVelocity = true;
                Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                return PlannerSuccess.Success;
            }

            Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;

            Creature.Weapon.PlayNoise();

            if (TargetComponent is PhysicsComponent)
            {

                if (PlayState.random.Next(100) < 10)
                {
                    PhysicsComponent phys = (PhysicsComponent)TargetComponent;
                    //if (ouchTimer.HasTriggered)
                    {
                        SoundManager.PlaySound("ouch", phys.GlobalTransform.Translation);
                        PlayState.ParticleManager.Trigger("blood_particle", phys.GlobalTransform.Translation, Color.White, 5);
                    }


                    Vector3 f = phys.GlobalTransform.Translation - Creature.Physics.GlobalTransform.Translation;
                    if (f.Length() > 2.0f)
                    {
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                        Creature.Physics.OrientWithVelocity = true;
                        Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                        return PlannerSuccess.Failure;
                    }
                    f.Y = 0.0f;

                    f.Normalize();
                    f *= 80;


                    phys.ApplyForce(f, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }

            return PlannerSuccess.Wait;
        }

    }
    #endregion
}
