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
using System.Security.Cryptography.X509Certificates;
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
        public List<Voxel> CurrentPath { get; set; }
        public bool DrawPath { get; set; }
        public GatherManager GatherManager { get; set; }
        public Timer IdleTimer { get; set; }
        public List<Thought> Thoughts { get; set; }
        public Timer SpeakTimer { get; set; }
            
        [JsonIgnore]
        public Act CurrentAct { get
        {
            if (CurrentTask != null) return CurrentTask.Script;
            else return null;
        }}

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

        public CreatureMovement Movement { get; set; }

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
        public bool TriggersMourning { get; set; }
        public List<int> XPEvents { get; set; } 
        public CreatureAI()
        {
            Movement = new CreatureMovement();
        }

        public CreatureAI(Creature creature,
            string name,
            EnemySensor sensor,
            PlanService planService) :
                base(name, creature.Physics)
        {
            Movement = new CreatureMovement();
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            Creature = creature;
            CurrentPath = null;
            DrawPath = false;
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
            XPEvents = new List<int>();
        }

        public void AddXP(int amount)
        {
            XPEvents.Add(amount);
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
            Task.PriorityType bestPriority = Task.PriorityType.Eventually;
            

            foreach(Task task in tasks)
            {
                float cost = task.ComputeCost(Creature);

                if(task.IsFeasible(Creature) && task.Priority >= bestPriority && cost < bestCost)
                {
                    bestCost = cost;
                    bestTask = task;
                    bestPriority = task.Priority;
                }
            }

            return bestTask;
        }

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

        public void DeleteBadTasks()
        {
            Tasks.RemoveAll(task => task.ShouldDelete(Creature));
        }

        public void ZoomToMe()
        {
            PlayState.Camera.ZoomTo(Position + Vector3.Up * 8.0f);
            PlayState.ChunkManager.ChunkData.SetMaxViewingLevel((int)Position.Y, ChunkManager.SliceMode.Y);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            OrderEnemyAttack();
            DeleteBadTasks();
            PreEmptTasks();

            if (Status.Energy.IsUnhappy() && PlayState.Time.IsNight())
            {
                Task toReturn = new SatisfyTirednessTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }

            if (Status.Hunger.IsUnhappy() &&  Faction.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }


            if(CurrentTask != null && CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();


                bool retried = false;
                if(status == Act.Status.Fail)
                {
                    if(CurrentTask.ShouldRetry(Creature))
                    {
                        if (!Tasks.Contains(CurrentTask))
                        {
                            CurrentTask.Priority = Task.PriorityType.Eventually;
                            Tasks.Add(CurrentTask);
                            CurrentTask.SetupScript(Creature);
                            retried = true;
                        }
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
                            PlayState.AnnouncementManager.Announce(Stats.FullName +  " (" + Stats.CurrentLevel.Name + ")" + " refuses to work!", 
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
                }   
                
            }


            PlannerTimer.Update(gameTime);
            UpdateThoughts();
            UpdateXP();

            base.Update(gameTime, chunks, camera);
        }

        public void UpdateXP()
        {
            foreach (int xp in XPEvents)
            {
                Stats.XP += xp;
                string sign = xp > 0 ? "+" : "";

                IndicatorManager.DrawIndicator(sign + xp.ToString() + " XP", Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, xp > 0 ? Color.Green : Color.Red);
            }
            XPEvents.Clear();
        }

        public virtual Task ActOnIdle()
        {
            if(GatherManager.VoxelOrders.Count == 0 && (GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile()))
            {
                // This is what to do when the unit has not been given any explicit orders.
                List<Room> rooms = Faction.GetRooms();

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
                    return new ActWrapperTask(new GoToChairAndSitAct(this)) { Priority = Task.PriorityType.Eventually, AutoRetry = false };
                }
                return new ActWrapperTask(new WanderAct(this, 2, 1.0f + MathFunctions.Rand(-0.5f, 0.5f), 1.0f)) { Priority = Task.PriorityType.Eventually };
            }
            // If we have no more build orders, look for gather orders
            else if (GatherManager.VoxelOrders.Count == 0)
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                GatherManager.StockOrders.RemoveAt(0);
                return new ActWrapperTask(new StockResourceAct(this, order.Resource))
                {
                    Priority = Task.PriorityType.Low
                };
            }
            // Otherwise handle build orders.
            else
            {
                List<Voxel> voxels = new List<Voxel>();
                List<VoxelType> types = new List<VoxelType>();
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
        }


        public void Jump(DwarfTime dt)
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

        public void RemoveThought(Thought.ThoughtType thoughtType)
        {
            Thoughts.RemoveAll(thought => thought.Type == thoughtType);
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
            bool good = thought.HappinessModifier > 0;
            Color textColor = good ? Color.Yellow : Color.Red;
            string prefix = good ? "+" : "";
            string postfix = good ? ":)" : ":(";
            IndicatorManager.DrawIndicator(prefix + thought.HappinessModifier + " " + postfix, Position + Vector3.Up + MathFunctions.RandVector3Cube() *0.5f, 1.0f, textColor);
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

        public bool HasTaskWithName(Task other)
        {
            return Tasks.Any(task => task.Name == other.Name);
        }

        public void OrderEnemyAttack()
        {
            foreach (CreatureAI enemy in Sensor.Enemies)
            {
                Task task = new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto);
                if (!HasTaskWithName(task))
                {
                    Creature.AI.Tasks.Add(task);

                    if (Faction == PlayState.PlayerFaction)
                    {
                        PlayState.AnnouncementManager.Announce(Stats.FullName + " is fighting a " + enemy.Creature.Name, 
                            Stats.FullName + " the " + Stats.CurrentLevel.Name + " is fighting a " + enemy.Stats.CurrentLevel.Name + " " + enemy.Faction.Race.Name, 
                            ZoomToMe);
                    }
                }
            }
        }

        public void AddMoney(float pay)
        {
            Status.Money += pay;
            bool good = pay > 0;
            Color textColor = good ? Color.Green : Color.Red;
            string prefix = good ? "+" : "";
            IndicatorManager.DrawIndicator(prefix + "$" + pay, Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);

        }

        public override string GetDescription()
        {
            string desc = Stats.FullName + ", level " + Stats.CurrentLevel.Index +
                          " " +
                          Stats.CurrentClass.Name + "\n    " +
                          "Happiness: " + Status.Happiness.GetDescription() + ". Health: " + Status.Health.Percentage +
                          ". Hunger: " + (100 - Status.Hunger.Percentage) + ". Energy: " + Status.Energy.Percentage + "\n";
            if (CurrentTask != null)
            {
                desc += "    Task: " + CurrentTask.Name;
            }

            return desc;
        }


    }

    public class CreatureMovement
    {

        private Voxel[,,] GetNeighborhood(Voxel voxel)
        {
            Voxel[, ,] neighborHood = new Voxel[3, 3, 3];
            CollisionManager objectHash = PlayState.ComponentManager.CollisionManager;

            VoxelChunk startChunk = voxel.Chunk;
            int x = (int)voxel.GridPosition.X;
            int y = (int)voxel.GridPosition.Y;
            int z = (int)voxel.GridPosition.Z;
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
                        if (!PlayState.ChunkManager.ChunkData.GetVoxel(startChunk, new Vector3(nx, ny, nz) + startChunk.Origin,
                            ref neighborHood[dx + 1, dy + 1, dz + 1]))
                        {
                            neighborHood[dx + 1, dy + 1, dz + 1] = null;
                        }

                    }
                }
            }
            return neighborHood;
        }

        bool HasNeighbors(Voxel[,,] neighborHood)
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

                    hasNeighbors = hasNeighbors || (neighborHood[dx, 1, dz] != null && (!neighborHood[dx, 1, dz].IsEmpty));
                }
            }


            return hasNeighbors;
        }

        private bool IsEmpty(Voxel v)
        {
            return v == null || v.IsEmpty;
        }

        public List<Creature.MoveAction> GetMoveActions(Vector3 pos)
        {
            Voxel vox = new Voxel();
            PlayState.ChunkManager.ChunkData.GetVoxel(pos, ref vox);
            return GetMoveActions(vox);
        }

        public List<Creature.MoveAction> GetMoveActions(Voxel voxel)
        {
            List<Creature.MoveAction> toReturn = new List<Creature.MoveAction>();
           
            CollisionManager objectHash = PlayState.ComponentManager.CollisionManager;

            Voxel[,,] neighborHood = GetNeighborhood(voxel);
            int x = (int)voxel.GridPosition.X;
            int y = (int)voxel.GridPosition.Y;
            int z = (int)voxel.GridPosition.Z;
            bool inWater = (neighborHood[1, 1, 1] != null && neighborHood[1, 1, 1].WaterLevel > 5);
            bool standingOnGround = (neighborHood[1, 0, 1] != null && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1] != null && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);


            List<Creature.MoveAction> successors = new List<Creature.MoveAction>();

            //Climbing ladders
            List<IBoundedObject> bodiesInside =
                objectHash.Hashes[CollisionManager.CollisionType.Static].GetItems(
                    new Point3(MathFunctions.FloorInt(voxel.Position.X),
                        MathFunctions.FloorInt(voxel.Position.Y),
                        MathFunctions.FloorInt(voxel.Position.Z)));
            if (bodiesInside != null)
            {
                bool hasLadder =
                    bodiesInside.OfType<GameComponent>()
                        .Any(component => component.Tags.Contains("Climbable"));
                if (hasLadder) ;
                {
                    successors.Add(new Creature.MoveAction()
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = Creature.MoveType.Climb
                    });

                    if (!standingOnGround)
                    {
                        successors.Add(new Creature.MoveAction()
                        {
                            Diff = new Vector3(1, 0, 1),
                            MoveType = Creature.MoveType.Climb
                        });
                    }

                    standingOnGround = true;
                }
            }


            if (standingOnGround || inWater)
            {
                Creature.MoveType moveType = inWater ? Creature.MoveType.Swim : Creature.MoveType.Walk;
                if (IsEmpty(neighborHood[0, 1, 1]))
                    // +- x
                    successors.Add(new Creature.MoveAction()
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[2, 1, 1]))
                    successors.Add(new Creature.MoveAction()
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 0]))
                    // +- z
                    successors.Add(new Creature.MoveAction()
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 2]))
                    successors.Add(new Creature.MoveAction()
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                if (!hasNeighbors)
                {
                    if (IsEmpty(neighborHood[2, 1, 2]))
                        // +x + z
                        successors.Add(new Creature.MoveAction()
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[2, 1, 0]))
                        successors.Add(new Creature.MoveAction()
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 2]))
                        // -x -z
                        successors.Add(new Creature.MoveAction()
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 0]))
                        successors.Add(new Creature.MoveAction()
                        {
                            Diff = new Vector3(0, 1, 0),
                            MoveType = moveType
                        });
                }

            }

            if (!topCovered && (standingOnGround || inWater))
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 1 && dz == 1) continue;

                        if (!IsEmpty(neighborHood[dx, 1, dz]))
                        {
                            successors.Add(new Creature.MoveAction()
                            {
                                Diff = new Vector3(dx, 2, dz),
                                MoveType = Creature.MoveType.Jump
                            });
                        }
                    }
                }

            }


            // Falling
            if (!inWater && !standingOnGround)
            {
                successors.Add(new Creature.MoveAction()
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = Creature.MoveType.Fall
                });
            }


            foreach (Creature.MoveAction v in successors)
            {
                Voxel n = neighborHood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n != null && (n.IsEmpty || n.WaterLevel > 0))
                {
                    Creature.MoveAction newAction = v;
                    newAction.Voxel = n;
                    toReturn.Add(newAction);
                }
            }


            return toReturn;
        }
    }

}