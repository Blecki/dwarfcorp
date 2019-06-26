using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DwarfAI : CreatureAI
    {
        [JsonProperty] private int lastXPAnnouncement = -1;
        private Timer restockTimer = new Timer(10.0f, false);
        private Timer SpeakTimer = new Timer(5.0f, true);
        [JsonProperty] private int NumDaysNotPaid = 0;
        private Timer IdleTimer = new Timer(2.0f, true);
        [JsonProperty] private double UnhappinessTime = 0.0f;
        private Timer AutoGatherTimer = new Timer(MathFunctions.Rand() * 5 + 3, false);

        public DwarfAI()
        {
        }

        public DwarfAI(
            ComponentManager Manager,
            string name,
            EnemySensor sensor) :
            base(Manager, name, sensor)
        {
        }

        private Task SatisfyBoredom()
        {
            if (World.GamblingState.State == Scripting.Gambling.Status.Gaming ||
                World.GamblingState.State == Scripting.Gambling.Status.WaitingForPlayers && World.GamblingState.Participants.Count > 0)
            {
                var task = new Scripting.GambleTask() { Priority = Task.PriorityType.High };
                if (task.IsFeasible(Creature) == Task.Feasibility.Feasible)
                {
                    return task;
                }
            }

            switch (MathFunctions.RandInt(0, 5))
            {
                case 0:
                {
                    return new ActWrapperTask(new LongWanderAct(this)
                        {
                            PathLength = 50,
                            Radius = 30,
                            Name = "Go on a walk",
                            Is2D = true
                        })
                    {
                        Name = "Go on a walk.",
                        Priority = Task.PriorityType.High,
                        BoredomIncrease = GameSettings.Default.Boredom_Walk
                    };
                }
                case 1:
                {
                    if (World.ListResourcesWithTag(Resource.ResourceTags.Alcohol).Count > 0)
                        return new ActWrapperTask(new Repeat(new FindAndEatFoodAct(this, true) { FoodTag = Resource.ResourceTags.Alcohol, FallbackTag = Resource.ResourceTags.Alcohol}, 3, false) { Name = "Binge drink." }) { Name = "Binge drink.", Priority = Task.PriorityType.High, BoredomIncrease = GameSettings.Default.Boredom_Eat };

                    if (!Stats.Hunger.IsSatisfied())
                        return new ActWrapperTask(new Repeat(new FindAndEatFoodAct(this, true), 3, false) { Name = "Binge eat." }) { Name = "Binge eat.", Priority = Task.PriorityType.High, BoredomIncrease = GameSettings.Default.Boredom_Eat };

                    return ActOnIdle();
                }
                case 2:
                {
                    return new ActWrapperTask(new GoToChairAndSitAct(this) { SitTime = 60, Name = "Relax." }) { Name = "Relax.", Priority = Task.PriorityType.High, BoredomIncrease = GameSettings.Default.Boredom_Sleep };
                }
                case 3:
                {
                    var task = new Scripting.GambleTask() { Priority = Task.PriorityType.High };
                    if (task.IsFeasible(Creature) == Task.Feasibility.Feasible)
                       return task;

                    break;
                }
                case 4:
                {
                    return ActOnIdle();
                }
            }
            
            return ActOnIdle();

        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera) 
        {
            //base.Update(gameTime, chunks, camera);

            if (!Active)
                return;
            Creature.NoiseMaker.BasePitch = Stats.VoicePitch;

            AutoGatherTimer.Update(gameTime);
            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            if (AutoGatherTimer.HasTriggered)
            {
                foreach (var body in World.EnumerateIntersectingObjects(Physics.BoundingBox.Expand(3.0f)).OfType<ResourceEntity>().Where(r => r.Active && r.AnimationQueue.Count == 0))
                    Creature.GatherImmediately(body, Inventory.RestockType.RestockResource);

                OrderEnemyAttack();
            }

            DeleteBadTasks();
            PreEmptTasks();

            if (CurrentTask != null)
            {
                Stats.Boredom.CurrentValue -= (float)(CurrentTask.BoredomIncrease * gameTime.ElapsedGameTime.TotalSeconds);
                if (Stats.Boredom.IsCritical())
                    Creature.AddThought("I have been overworked recently.", new TimeSpan(0, 4, 0, 0), -2.0f);
            }

            // Heal thyself
            if (Stats.Health.IsDissatisfied() && Stats.Species.CanSleep)
            {
                Task toReturn = new GetHealedTask();
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            // Try to go to sleep if we are low on energy and it is night time.
            if (!Stats.Energy.IsSatisfied() && Manager.World.Time.IsNight())
            {
                Task toReturn = new SatisfyTirednessTask();
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            // Try to find food if we are hungry.
            if (Stats.Hunger.IsDissatisfied() && World.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask() { MustPay = true };
                if (Stats.Hunger.IsCritical())
                    toReturn.Priority = Task.PriorityType.Urgent;
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            if (Stats.CanGetBored && Stats.Boredom.IsDissatisfied())
            {
                if (!Tasks.Any(task => task.BoredomIncrease < 0))
                {
                    Task toReturn = SatisfyBoredom();
                    if (toReturn != null && !Tasks.Contains(toReturn) && CurrentTask != toReturn)
                        AssignTask(toReturn);
                }
            }

            restockTimer.Update(DwarfTime.LastTime);
            if (restockTimer.HasTriggered && Creature.Inventory.Resources.Count > 10)
                Creature.RestockAllImmediately();

            if (CurrentTask == null) // We need something to do.
            {
                if (Stats.Happiness.IsSatisfied()) // We're happy, so make sure we aren't on strike.
                {
                    Stats.IsOnStrike = false;
                    UnhappinessTime = 0.0f;
                }

                if (Stats.IsOnStrike) // We're on strike, so track how long this job has sucked.
                {
                    UnhappinessTime += gameTime.ElapsedGameTime.TotalMinutes;
                    if (UnhappinessTime > GameSettings.Default.HoursUnhappyBeforeQuitting) // If we've been unhappy long enough, quit.
                    {
                        var thoughts = GetRoot().GetComponent<DwarfThoughts>();
                        Manager.World.MakeAnnouncement( // Can't use a popup because the dwarf will soon not exist. Also - this is a serious event!
                            Message: String.Format("{0} has quit!{1}",
                                Stats.FullName,
                                (thoughts == null ? "" : (" The last straw: " + thoughts.Thoughts.Last(t => t.HappinessModifier < 0.0f).Description))),
                            ClickAction: null,
                            logEvent: true,
                            eventDetails: (thoughts == null ? "So sick of this place!" : String.Join("\n", thoughts.Thoughts.Where(t => t.HappinessModifier < 0.0f).Select(t => t.Description)))
                            );

                        LeaveWorld();

                        GetRoot().GetComponent<Inventory>().Die();
                        GetRoot().GetComponent<SelectionCircle>().Die();

                        if (thoughts != null)
                            thoughts.Thoughts.Clear();

                        Faction.Minions.Remove(this);
                        World.PersistentData.SelectedMinions.Remove(this);

                        return;
                    }
                }
                else if (Stats.Happiness.IsDissatisfied()) // We aren't on strike, but we hate this place.
                {
                    if (MathFunctions.Rand(0, 1) < 0.25f) // We hate it so much that we might just go on strike! This can probably be tweaked. As it stands,
                                                          // dorfs go on strike almost immediately every time.
                    {
                        Manager.World.UserInterface.MakeWorldPopup(String.Format("{0} ({1}) refuses to work!",
                               Stats.FullName, Stats.CurrentClass.Name), Creature.Physics, -10, 10);
                        Manager.World.Tutorial("happiness");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.25f);
                        Stats.IsOnStrike = true;
                    }
                }

                if (!Stats.IsOnStrike) // We aren't on strike, so find a new task.
                {
                    var goal = GetEasiestTask(Tasks);

                    if (goal != null)
                    {
                        IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                        ChangeTask(goal);
                    }
                    else
                    {
                        var newTask = ActOnIdle();
                        if (newTask != null)
                            ChangeTask(newTask);
                    }
                }
                else
                    ChangeTask(ActOnIdle());
            }
            else
            {
                if (CurrentAct == null) // Should be impossible to have a current task and no current act.
                {
                    // Try and recover the correct act.
                    // <blecki> I always run with a breakpoint set here... just in case.
                    ChangeAct(CurrentTask.CreateScript(Creature));

                    // This is a bad situation!
                    if (CurrentAct == null)
                        ChangeTask(null);
                }

                if (CurrentAct != null)
                {
                    var status = CurrentAct.Tick();
                    bool retried = false;
                    if (CurrentAct != null && CurrentTask != null)
                    {
                        if (status == Act.Status.Fail)
                        {
                            LastFailedAct = CurrentAct.Name;

                            if (!FailedTasks.Any(task => task.TaskFailure.Equals(CurrentTask)))
                                FailedTasks.Add(new FailedTask() { TaskFailure = CurrentTask, FailedTime = World.Time.CurrentDate });

                            if (CurrentTask.ShouldRetry(Creature))
                            {
                                if (!Tasks.Contains(CurrentTask))
                                {
                                    ReassignCurrentTask();
                                    retried = true;
                                }
                            }
                        }
                    }

                    if (CurrentTask != null && CurrentTask.IsComplete(Faction))
                        ChangeTask(null);
                    else if (status != Act.Status.Running && !retried)
                        ChangeTask(null);
                }
            }

            // With a small probability, the creature will drown if its under water.
            if (MathFunctions.RandEvent(0.01f))
            {
                var above = VoxelHelpers.GetVoxelAbove(Physics.CurrentVoxel);
                var below = VoxelHelpers.GetVoxelBelow(Physics.CurrentVoxel);
                bool shouldDrown = (above.IsValid && (!above.IsEmpty || above.LiquidLevel > 0));
                if ((Physics.IsInLiquid || (!Movement.CanSwim && (below.IsValid && (below.LiquidLevel > 5)))) 
                    && (!Movement.CanSwim || shouldDrown))
                {
                    Creature.Damage(Movement.CanSwim ? 1.0f : 30.0f, Health.DamageType.Normal);
                }
            }

            if (PositionConstraint.Contains(Physics.LocalPosition) == ContainmentType.Disjoint)
            {
                Physics.LocalPosition = MathFunctions.Clamp(Physics.Position, PositionConstraint);
                Physics.PropogateTransforms();
            }
        }

        public override void AddXP(int XP)
        {
            Stats.XP += XP;

            string sign = XP > 0 ? "+" : "";

            IndicatorManager.DrawIndicator(sign + XP + " XP",
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, XP > 0 ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red));

            if (Stats.IsOverQualified && lastXPAnnouncement != Stats.LevelIndex && Faction == Manager.World.PlayerFaction)
            {
                lastXPAnnouncement = Stats.LevelIndex;

                Manager.World.MakeAnnouncement(
                    new Gui.Widgets.QueuedAnnouncement
                    {
                        Text = String.Format("{0} ({1}) wants a promotion!",
                            Stats.FullName, Stats.CurrentClass.Name),
                        ClickAction = (gui, sender) =>
                        {
                            GameStateManager.PushState(new EconomyState(Manager.World.Game, Manager.World));
                        }
                    });

                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
                Manager.World.Tutorial("level up");
            }
        }

        protected override void MakeBattleAnnouncement(CreatureAI Enemy)
        {
            Manager.World.MakeAnnouncement(
                new Gui.Widgets.QueuedAnnouncement
                {
                    Text = String.Format("{0} is fighting {1}.", Stats.FullName,
                                    TextGenerator.IndefiniteArticle(Enemy.Stats.CurrentClass.Name)),
                    ClickAction = (gui, sender) => ZoomToMe()
                });

            Manager.World.Tutorial("combat");
        }

        public override Task ActOnIdle()
        {
            if (Creature.Physics.IsInLiquid)
                return new FindLandTask();
            var flames = GetRoot().GetComponent<Flammable>();
            if (flames != null && flames.IsOnFire)
                return new LongWanderAct(this) { Name = "Freak out!", PathLength = 2, Radius = 5 }.AsTask();

                if (World.GamblingState.State == Scripting.Gambling.Status.Gaming ||
                    World.GamblingState.State == Scripting.Gambling.Status.WaitingForPlayers && World.GamblingState.Participants.Count > 0)
                {
                    var task = new Scripting.GambleTask() { Priority = Task.PriorityType.High };
                    if (task.IsFeasible(Creature) == Task.Feasibility.Feasible)
                    {
                        return task;
                    }
                }

            if (!Stats.IsOnStrike)
            {
                var candidate = World.TaskManager.GetBestTask(this);
                if (candidate != null)
                    return candidate;

                if (Stats.CurrentLevel.HealingPower > 0 && Faction.Minions.Any(minion => !minion.Creature.Stats.Health.IsSatisfied()))
                {
                    var minion = Faction.Minions.FirstOrDefault(m => m != this && !m.Stats.Health.IsSatisfied());
                    if (minion != null)
                    {
                        return new MagicHealAllyTask(minion);
                    }
                }
            }

            if (NumDaysNotPaid > 0)
            {
                if (Faction.Economy.Funds >= Stats.CurrentLevel.Pay)
                {
                    var task = new ActWrapperTask(new GetMoneyAct(this, Math.Min(Stats.CurrentLevel.Pay * NumDaysNotPaid, Faction.Economy.Funds)) { IncrementDays = false })
                    { AutoRetry = true, Name = "Get paid.", Priority = Task.PriorityType.High };
                    if (!HasTaskWithName(task))
                    {
                        return task;
                    }
                }
            }

            if (Creature.Inventory.Resources.Count > 0)
                foreach (var status in Creature.RestockAll())
                    ; // RestockAll generates tasks for the dwarf.           

            // Todo: Need dwarf to deposit money that's not theirs?


            if (Tasks.Count == 0)
            {
                // Craft random items for fun.
                if (Stats.IsTaskAllowed(Task.TaskCategory.CraftItem) && MathFunctions.RandEvent(0.0005f)) // Todo: These chances need to be configurable.
                {
                    var item = Library.GetRandomApplicableCraftItem(Faction, World);

                    if (item != null)
                    {
                        var resources = new List<ResourceAmount>();
                        foreach (var resource in item.RequiredResources)
                        {
                            var amount = World.GetResourcesWithTags(new List<Quantitiy<Resource.ResourceTags>>() { resource });
                            if (amount == null || amount.Count == 0)
                                break;
                            resources.Add(Datastructures.SelectRandom(amount));
                        }

                        if (resources.Count > 0)
                            return new CraftResourceTask(item, 1, 1, resources) { IsAutonomous = true, Priority = Task.PriorityType.Low };
                    }
                }

                // Find a room to train in, if applicable.
                if (Stats.IsTaskAllowed(Task.TaskCategory.Attack) && MathFunctions.RandEvent(GameSettings.Default.TrainChance))
                {
                    if (!Stats.IsTaskAllowed(Task.TaskCategory.Research))
                    {
                        var closestTraining = Faction.FindNearestItemWithTags("Train", Position, true, this);
                        if (closestTraining != null)
                            return new ActWrapperTask(new GoTrainAct(this)) { Name = "train", ReassignOnDeath = false, Priority = Task.PriorityType.Medium };
                    }
                    else
                    {
                        var closestTraining = Faction.FindNearestItemWithTags("Research", Position, true, this);
                        if (closestTraining != null)
                            return new ActWrapperTask(new GoTrainAct(this) { Magical = true }) { Name = "do magic research", ReassignOnDeath = false, Priority = Task.PriorityType.Medium };
                    }
                }

                if (IdleTimer.HasTriggered && MathFunctions.RandEvent(0.005f))
                {
                    return new ActWrapperTask(new MournGraves(this))
                    {
                        Priority = Task.PriorityType.Medium,
                        AutoRetry = false
                    };
                }

                // Otherwise, try to find a chair to sit in
                if (IdleTimer.HasTriggered && MathFunctions.RandEvent(0.25f) && Faction == World.PlayerFaction)
                {
                    return new ActWrapperTask(new GoToChairAndSitAct(this))
                    {
                        Priority = Task.PriorityType.Eventually,
                        AutoRetry = false
                    };
                }
            }

            if (MathFunctions.RandEvent(0.1f) && World.ListResourcesWithTag(Resource.ResourceTags.Potion).Count > 0)
                return new GatherPotionsTask();
            
            return new LookInterestingTask();
        }

        public override void Converse(CreatureAI other)
        {
            if (SpeakTimer.HasTriggered)
            {
                Creature.AddThought("I spoke to a friend recently.", new TimeSpan(0, 8, 0, 0), 5.0f);
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Dots);
                Creature.Physics.Face(other.Position);
                SpeakTimer.Reset(SpeakTimer.TargetTimeSeconds);
            }
        }

        public void OnNotPaid()
        {
            NumDaysNotPaid++;

            if (NumDaysNotPaid < 2)
                Creature.AddThought("I have not been paid!", new TimeSpan(1, 0, 0, 0), -25.0f);
            else
                Creature.AddThought("I have not been paid for days!", new TimeSpan(1, 0, 0, 0), -25.0f * NumDaysNotPaid);
        }

        public void OnPaid()
        {
            NumDaysNotPaid = 0;
        }
    }
}
