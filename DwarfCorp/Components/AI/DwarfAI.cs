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

        private enum RandomTask
        {
            Gamble,
            Eat,
            Drink,
            Walk,
            Sit,
            Train
        }

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

        private class IdleTask
        {
            public String Name;
            public bool PreferWhenBored = false;
            public Func<float> Chance = () => 1.0f;
            public Func<DwarfAI, Task> Create = (_ai) => null;
            public Func<DwarfAI, WorldManager, bool> Available = (_ai, _world) => true;
        }

        private static List<IdleTask> IdleTasks;

        private static void InitializeIdleTasks()
        {
            if (IdleTasks == null)
            {
                IdleTasks = new List<IdleTask>();

                IdleTasks.Add(new IdleTask // Join dice game - actually should be checked before even looking for an idle task.
                {
                    Name = "Join Gamble",
                    PreferWhenBored = true,
                    Chance = () => 1.0f,
                    Create = (AI) =>
                    {
                        var task = new Scripting.GambleTask() { Priority = TaskPriority.High };
                        if (task.IsFeasible(AI.Creature) == Feasibility.Feasible)
                            return task;
                        return null;
                    },
                    Available = (AI, World) => World.GamblingState.State == Scripting.Gambling.Status.Gaming ||
                        World.GamblingState.State == Scripting.Gambling.Status.WaitingForPlayers && World.GamblingState.Participants.Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Go on a walk",
                    PreferWhenBored = true,
                    Chance = () => 1.0f,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new LongWanderAct(AI)
                        {
                            PathLength = 50,
                            Radius = 30,
                            Name = "Go on a walk",
                            Is2D = true
                        })
                        {
                            Name = "Go on a walk.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Default.Boredom_Walk,
                            EnergyDecrease = GameSettings.Default.Energy_Refreshing,
                        };
                    }
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Binge drink",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Default.BingeChance,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(
                            new Repeat(
                                new FindAndEatFoodAct(AI, true)
                                {
                                    FoodTag = Resource.ResourceTags.Alcohol,
                                    FallbackTag = Resource.ResourceTags.Alcohol
                                },
                                3, false)
                            {
                                Name = "Binge drink."
                            })
                        {
                            Name = "Binge drink.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Default.Boredom_Eat,
                            EnergyDecrease = GameSettings.Default.Energy_Restful,
                        };
                    },
                    Available = (AI, World) => World.ListResourcesWithTag(Resource.ResourceTags.Alcohol).Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Binge eat",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Default.BingeChance,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new Repeat(new FindAndEatFoodAct(AI, true), 3, false)
                        {
                            Name = "Binge eat."
                        })
                        {
                            Name = "Binge eat.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Default.Boredom_Eat,
                            EnergyDecrease = GameSettings.Default.Energy_Restful,
                        };
                    },
                    Available = (AI, World) => !AI.Stats.Hunger.IsSatisfied()
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Relax",
                    PreferWhenBored = true,
                    Chance = () => 1.0f,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new GoToChairAndSitAct(AI)
                        {
                            SitTime = 60,
                            Name = "Relax."
                        })
                        {
                            Name = "Relax.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Default.Boredom_Sleep,
                            EnergyDecrease = GameSettings.Default.Energy_Restful
                        };
                    },
                    Available = (AI, World) => !AI.Stats.Hunger.IsSatisfied()
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Start Dice Game",
                    PreferWhenBored = true,
                    Chance = () => 1.0f,
                    Create = (AI) =>
                    {
                        var task = new Scripting.GambleTask() { Priority = TaskPriority.High };
                        if (task.IsFeasible(AI.Creature) == Feasibility.Feasible)
                            return task;
                        return null;
                    }
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Craft",
                    PreferWhenBored = true,
                    Chance = () => 0.005f,
                    Create = (AI) =>
                    {
                        if (Library.GetRandomApplicableCraftable(AI.Faction, AI.World).HasValue(out var item))
                        {
                            var resources = new List<ResourceAmount>();
                            foreach (var resource in item.RequiredResources)
                            {
                                var amount = AI.World.GetResourcesWithTags(new List<Quantitiy<Resource.ResourceTags>>() { resource });
                                if (amount == null || amount.Count == 0)
                                    break;
                                resources.Add(Datastructures.SelectRandom(amount));
                            }

                            if (resources.Count > 0)
                                return new CraftResourceTask(item, 1, 1, resources) { IsAutonomous = true, Priority = TaskPriority.Low };
                        }

                        return null;
                    },
                    Available = (AI, World) => AI.Stats.IsTaskAllowed(TaskCategory.CraftItem)
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Train",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Default.TrainChance,
                    Create = (AI) =>
                    {
                        if (!AI.Stats.IsTaskAllowed(TaskCategory.Research))
                        {
                            var closestTraining = AI.Faction.FindNearestItemWithTags("Train", AI.Position, true, AI);
                            if (closestTraining != null)
                                return new ActWrapperTask(new GoTrainAct(AI)) { Name = "train", ReassignOnDeath = false, Priority = TaskPriority.Medium };
                        }
                        else
                        {
                            var closestTraining = AI.Faction.FindNearestItemWithTags("Research", AI.Position, true, AI);
                            if (closestTraining != null)
                                return new ActWrapperTask(new GoTrainAct(AI) { Magical = true }) { Name = "do magic research", ReassignOnDeath = false, Priority = TaskPriority.Medium };
                        }

                        return null;
                    },
                    Available = (AI, World) => AI.Stats.IsTaskAllowed(TaskCategory.Attack)
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Mourn",
                    PreferWhenBored = false,
                    Chance = () => GameSettings.Default.TrainChance,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new MournGraves(AI))
                        {
                            Priority = TaskPriority.Medium,
                            AutoRetry = false
                        };
                    }
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Gather Potions",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Default.TrainChance,
                    Create = (AI) =>
                    {
                        return new GatherPotionsTask();
                    },
                    Available = (AI, World) => World.ListResourcesWithTag(Resource.ResourceTags.Potion).Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Default",
                    PreferWhenBored = false,
                    Chance = () => 2.0f,
                    Create = (AI) => new LookInterestingTask()
                });
            }
        }

        private Task ChooseIdleTask(bool IsBored)
        {
            InitializeIdleTasks();

            var availableTasks = IdleTasks.Where(t =>
            {
                if (IsBored)
                    if (t.PreferWhenBored == false)
                        return false;

                return t.Available(this, World);
            });

            var totalChance = availableTasks.Sum(t => t.Chance());
            var random = MathFunctions.Random.NextDouble() * totalChance;

            foreach (var t in availableTasks)
            {
                if (random < t.Chance())
                    return t.Create(this);
                random -= t.Chance();
            }

            return new LookInterestingTask();
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

                Stats.Energy.CurrentValue += (float)(CurrentTask.EnergyDecrease * gameTime.ElapsedGameTime.TotalSeconds);
            }

            // Heal thyself
            if (!Stats.Health.IsSatisfied())
            {
                Task toReturn = new GetHealedTask();
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            // Try to go to sleep if we are low on energy and it is night time.
            if (Stats.Energy.IsCritical())
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
                    toReturn.Priority = TaskPriority.Urgent;
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            if (Stats.Boredom.IsDissatisfied())
            {
                if (!Tasks.Any(task => task.BoredomIncrease < 0))
                {
                    Task toReturn = ChooseIdleTask(true);
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
                        if (GetRoot().GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                        {
                            Manager.World.MakeAnnouncement( // Can't use a popup because the dwarf will soon not exist. Also - this is a serious event!
                                Message: String.Format("{0} has quit! The last straw: {1}",
                                    Stats.FullName,
                                    thoughts.Thoughts.Last(t => t.HappinessModifier < 0.0f).Description),
                                ClickAction: null,
                                logEvent: true,
                                eventDetails: String.Join("\n", thoughts.Thoughts.Where(t => t.HappinessModifier < 0.0f).Select(t => t.Description)));

                            thoughts.Thoughts.Clear();
                        }
                        else
                            Manager.World.MakeAnnouncement( // Can't use a popup because the dwarf will soon not exist. Also - this is a serious event!
                           Message: String.Format("{0} has quit!", Stats.FullName),
                           ClickAction: null,
                           logEvent: true,
                           eventDetails: "So sick of this place!");

                        LeaveWorld();

                        if (GetRoot().GetComponent<Inventory>().HasValue(out var inv))
                            inv.Die();

                        if (GetRoot().GetComponent<SelectionCircle>().HasValue(out var sel))
                            sel.Die();

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
                    if (goal == null)
                        goal = World.TaskManager.GetBestTask(this);

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
                if (!CurrentAct.HasValue()) // Should be impossible to have a current task and no current act.
                {
                    // Try and recover the correct act.
                    // <blecki> I always run with a breakpoint set here... just in case.
                    ChangeAct(CurrentTask.CreateScript(Creature));

                    // This is a bad situation!
                    if (!CurrentAct.HasValue())
                        ChangeTask(null);
                }

                if (CurrentAct.HasValue(out Act currentAct))
                {
                    var status = currentAct.Tick();
                    bool retried = false;

                    if (CurrentAct.HasValue(out Act newCurrentAct) && CurrentTask != null)
                    {
                        if (status == Act.Status.Fail)
                        {
                            LastFailedAct = newCurrentAct.Name;

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

                    if (CurrentTask != null && CurrentTask.IsComplete(World))
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

            if (GetRoot().GetComponent<Flammable>().HasValue(out var flames) && flames.IsOnFire)
                return new LongWanderAct(this) { Name = "Freak out!", PathLength = 2, Radius = 5 }.AsTask();

            if (World.GamblingState.State == Scripting.Gambling.Status.Gaming ||
                World.GamblingState.State == Scripting.Gambling.Status.WaitingForPlayers && World.GamblingState.Participants.Count > 0)
            {
                var task = new Scripting.GambleTask() { Priority = TaskPriority.High };
                if (task.IsFeasible(Creature) == Feasibility.Feasible)
                    return task;
            }

            if (!Stats.IsOnStrike)
            {
                var candidate = World.TaskManager.GetBestTask(this);
                if (candidate != null)
                    return candidate;
            }

            if (Stats.CurrentLevel.HealingPower > 0 && Faction.Minions.Any(minion => !minion.Creature.Stats.Health.IsSatisfied()))
            {
                var minion = Faction.Minions.FirstOrDefault(m => m != this && !m.Stats.Health.IsSatisfied());
                if (minion != null)
                {
                    return new MagicHealAllyTask(minion);
                }
            }

            if (NumDaysNotPaid > 0)
            {
                if (Faction.Economy.Funds >= Stats.CurrentLevel.Pay)
                {
                    var task = new ActWrapperTask(new GetMoneyAct(this, Math.Min(Stats.CurrentLevel.Pay * NumDaysNotPaid, Faction.Economy.Funds)) { IncrementDays = false })
                    { AutoRetry = true, Name = "Get paid.", Priority = TaskPriority.High };
                    if (!HasTaskWithName(task))
                        return task;
                }
            }

            if (Creature.Inventory.Resources.Count > 0)
                foreach (var status in Creature.RestockAll())
                    ; // RestockAll generates tasks for the dwarf.           

            // Todo: Need dwarf to deposit money that's not theirs?


            if (Tasks.Count == 0)
                return ChooseIdleTask(false);
            else
                return Tasks[0];

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
