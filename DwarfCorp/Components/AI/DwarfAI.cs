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
        [JsonProperty] private int lastXPAnnouncement = -1; // Todo: Kill
        [JsonProperty] private int lastXPAnnouncementStat = -1;
        [JsonProperty] private int NumDaysNotPaid = 0;
        [JsonProperty] private double UnhappinessTime = 0.0f;
        [JsonProperty] private double TimeSinceLastAssignedTask = 0.0f; // The time since this dwarf last took on a task from the order pool.

        [JsonIgnore] public bool BreakOnUpdate = false;

        private Timer SpeakTimer = new Timer(5.0f, true);
        private Timer IdleTimer = new Timer(2.0f, true);
        private Timer AutoGatherTimer = new Timer(MathFunctions.Rand() * 5 + 3, false);

        public DwarfAI()
        {

        }

        public DwarfAI(ComponentManager Manager, string name, EnemySensor sensor) :
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

        public override void OnAttacked(Creature By)
        {
            // Make the other creature defend itself.
            var otherKill = new KillEntityTask(By.Physics, KillEntityTask.KillType.Auto)
            {
                AutoRetry = true,
                ReassignOnDeath = false
            };

            if (!HasTaskWithName(otherKill))
                AssignTask(otherKill);

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
                    Chance = () => 100.0f, // This is only available while a game is in progress... so make it highly likely.
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
                    Chance = () => GameSettings.Current.IdleBehavior_Walk,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new LongWanderAct(AI)
                        {
                            PathLength = 20,
                            Radius = 10,
                            Name = "Go on a walk",
                            Is2D = true,
                            SpeedAdjust = 0.5f
                        })
                        {
                            AutoRetry = false,
                            Name = "Go on a walk.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Current.Boredom_Walk,
                            EnergyDecrease = GameSettings.Current.Energy_Refreshing,
                        };
                    }
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Binge drink",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Binge,
                    Create = (AI) =>
                    {
                        if (AI.World.GetResourcesWithTag("Alcohol").Count > 0)
                        return new ActWrapperTask(
                            new Repeat(
                                new FindAndEatFoodAct(AI, true)
                                {
                                    FoodTag = "Alcohol",
                                    FallbackTag = "Alcohol"
                                },
                                3, false)
                            {
                                Name = "Binge drink."
                            })
                        {
                            Name = "Binge drink.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Current.Boredom_Eat,
                            EnergyDecrease = GameSettings.Current.Energy_Restful,
                        };
                        else
                        {
                            AI.Creature.AddThought("I need a drink.", new TimeSpan(4, 0, 0), -25f);
                            return null;
                        }
                    },
                    //Available = (AI, World) => World.GetResourcesWithTag("Alcohol").Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Binge eat",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Binge,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new Repeat(new FindAndEatFoodAct(AI, true), 3, false)
                        {
                            Name = "Binge eat."
                        })
                        {
                            Name = "Binge eat.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Current.Boredom_Eat,
                            EnergyDecrease = GameSettings.Current.Energy_Restful,
                        };
                    },
                    Available = (AI, World) => !AI.Stats.Hunger.IsSatisfied()
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Relax",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Relax,
                    Create = (AI) =>
                    {
                        return new ActWrapperTask(new GoToChairAndSitAct(AI)
                        {
                            SitTime = 15,
                            Name = "Relax."
                        })
                        {
                            Name = "Relax.",
                            Priority = TaskPriority.High,
                            BoredomIncrease = GameSettings.Current.Boredom_Sleep,
                            EnergyDecrease = GameSettings.Current.Energy_Restful
                        };
                    }
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Start Dice Game",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Gamble,
                    Create = (AI) =>
                    {
                        var task = new Scripting.GambleTask() { Priority = TaskPriority.High };
                        if (task.IsFeasible(AI.Creature) == Feasibility.Feasible)
                            return task;
                        return null;
                    },
                    Available = (AI, world) => AI.Stats.Boredom.IsDissatisfied()
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Heal Allies",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Heal,
                    Create = (AI) =>
                    {
                        var minion = AI.Faction.Minions.FirstOrDefault(m => m != AI && !m.Stats.Health.IsSatisfied());
                        if (minion != null)
                            return new MagicHealAllyTask(minion);
                        return null;
                    },
                    Available = (AI, world) => AI.Creature.Equipment.HasValue(out var eq) && eq.GetItemInSlot("Tool").HasValue(out var tool) && tool.Tool_Magic && AI.Stats.Wisdom > 0 && AI.Faction.Minions.Any(minion => !minion.Creature.Stats.Health.IsSatisfied())
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Craft",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Craft,
                    Create = (AI) =>
                    {
                        if (Library.GetRandomApplicableCraftable(AI.Faction, AI.World).HasValue(out var item))
                        {
                            var resources = new List<Resource>();
                            var allow = true;
                            foreach (var resource in item.Craft_Ingredients)
                            {
                                var available = AI.World.GetResourcesWithTag(resource.Tag);
                                if (available.Count == 0)
                                    allow = false;
                                else
                                    resources.Add(Datastructures.SelectRandom(available));
                            }

                            if (allow && resources.Count > 0)
                                return new CraftResourceTask(item, 1, 1, resources.Select(r => new ResourceApparentTypeAmount(r.DisplayName, 1)).ToList()) { IsAutonomous = true, Priority = TaskPriority.Low };
                        }

                        return null;
                    },
                    Available = (AI, World) => GameSettings.Current.AllowIdleCrafting && AI.Stats.IsTaskAllowed(TaskCategory.CraftItem)
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Train",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Train,
                    Create = (AI) =>
                    {
                        var closestTraining = AI.Faction.FindNearestItemWithTags("Train", AI.Position, true, AI);
                        if (closestTraining != null)
                            return new ActWrapperTask(new GoTrainAct(AI)) { Name = "train", ReassignOnDeath = false, Priority = TaskPriority.Medium };
                        return null;
                    },
                    Available = (AI, World) => AI.Stats.IsTaskAllowed(TaskCategory.Attack)
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Research",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Research,
                    Create = (AI) =>
                    {
                        var closestTraining = AI.Faction.FindNearestItemWithTags("Research", AI.Position, true, AI);
                        if (closestTraining != null)
                            return new ActWrapperTask(new GoTrainAct(AI) { Magical = true }) { Name = "do magic research", ReassignOnDeath = false, Priority = TaskPriority.Medium };
                        return null;
                    },
                    Available = (AI, World) => AI.Stats.IsTaskAllowed(TaskCategory.Research)
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Mourn",
                    PreferWhenBored = false,
                    Chance = () => GameSettings.Current.IdleBehavior_Mourn,
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
                    Chance = () => GameSettings.Current.IdleBehavior_Potions,
                    Create = (AI) =>
                    {
                        return new GatherPotionsTask();
                    },
                    Available = (AI, World) => World.GetResourcesWithTag("Potion").Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Restock",
                    PreferWhenBored = true,
                    Chance = () => GameSettings.Current.IdleBehavior_Restock,
                    Create = (AI) =>
                    {
                        AI.Creature.AssignRestockAllTasks(TaskPriority.Medium, true);
                        if (AI.Tasks.Count > 0)
                            return AI.Tasks[0];
                        return null;
                    },
                    Available = (AI, World) => AI.Creature.Inventory.Resources.Count > 0
                });

                IdleTasks.Add(new IdleTask
                {
                    Name = "Default",
                    PreferWhenBored = false,
                    Chance = () => GameSettings.Current.IdleBehavior_Loiter,
                    Create = (AI) => new LookInterestingTask()
                });
            }
        }

        private MaybeNull<Task> ChooseIdleTask(bool IsBored)
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

        private void AssignGeneratedTask(Task Task)
        {
            AssignTask(Task);
        }

        override public void AIUpdate(DwarfTime gameTime, ChunkManager chunks, Camera camera) 
        {
            //base.Update(gameTime, chunks, camera);

            if (BreakOnUpdate)
                BreakOnUpdate = false;

            if (!Active)
                return;

            TimeSinceLastAssignedTask += gameTime.ElapsedGameTime.TotalMinutes;

            //SetMessage("");
            Creature.NoiseMaker.BasePitch = Stats.VoicePitch;

            AutoGatherTimer.Update(gameTime);
            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            if (AutoGatherTimer.HasTriggered && !Creature.Stats.IsAsleep)
            {
                foreach (var body in World.EnumerateIntersectingRootObjects(Physics.BoundingBox.Expand(3.0f)).OfType<ResourceEntity>().Where(r => r.Active && r.AnimationQueue.Count == 0))
                    Creature.GatherImmediately(body, Inventory.RestockType.RestockResource);

                OrderEnemyAttack();
            }

            DeleteBadTasks();
            PreEmptTasks();

            // Check for managerial buff
            var managerBufferComponent = Parent.GetComponent<RadiusBuffer>();
            if (Stats.IsManager && !managerBufferComponent.HasValue())
            {
                var managerBuffer = new RadiusBuffer(Manager, "Manager Buffer", Matrix.Identity, new Vector3(16, 16, 16), Vector3.Zero)
                {
                    SenseRadius = 16,
                    Buff = new ManagerMotivationStatusEffect(0.1f * Stats.Intelligence)
                };

                Parent.AddChild(managerBuffer);
            }
            else if (!Stats.IsManager && managerBufferComponent.HasValue(out var outdatedBuff))
                outdatedBuff.Die();

            if (Stats.IsManager)
                Stats.AddBuff(new BaselineMotivationStatusEffect(1.0f)); // Managers are always highly motivated.
            else
            {
                var employees = World.CalculateSupervisedEmployees();
                if (employees != 0)
                {
                    // Make dwarves unhappy if there is too much supervision; or if there is too little.
                    var supervisionFactor = (float)World.CalculateSupervisionCap() / (float)employees;
                    var supervisionDelta = Math.Abs(supervisionFactor - 1);
                    if (supervisionDelta > 0.25f) Creature.AddThought("Supervision here sucks.", new TimeSpan(4, 0, 0), -25.0f);
                    if (Stats.Happiness.CurrentValue < 25.0f) Stats.AddBuff(new HappinessMotivationStatusEffect(-0.5f));
                    if (Stats.Happiness.CurrentValue > 75.0f) Stats.AddBuff(new HappinessMotivationStatusEffect(0.25f));
                    Stats.AddBuff(new BaselineMotivationStatusEffect(supervisionFactor));
                }
            }

            // Freak out if on fire!
            if (GetRoot().GetComponent<Flammable>().HasValue(out var flames) && flames.IsOnFire)
                ChangeTask(new ActWrapperTask(new LongWanderAct(this) { Name = "Freak out!", PathLength = 2, Radius = 5 }));

            // Heal thyself
            if (!Stats.Health.IsSatisfied())
                AssignGeneratedTask(new GetHealedTask());

            // Try to go to sleep if we are low on energy.
            if (Stats.Energy.IsDissatisfied())
                AssignGeneratedTask(new SatisfyTirednessTask()); // Satisfy Tiredness is 'High' priority, so they will truck on for urgent tasks.

            if (Stats.Energy.IsCritical())
                ChangeTask(new SatisfyTirednessTask() { Priority = TaskPriority.Urgent }); // But they've reached their limit.

            // Try to find food if we are hungry.
            if (Stats.Hunger.IsDissatisfied() && World.CountResourcesWithTag("Edible") > 0)
                AssignGeneratedTask(new SatisfyHungerTask()
                {
                    MustPay = true,
                    Priority = Stats.Hunger.IsCritical() ? TaskPriority.Urgent : TaskPriority.Medium
                });

            // If we haven't gotten paid, don't wait for idle to go collect.
            if (NumDaysNotPaid > 0 && Faction.Economy.Funds >= Stats.DailyPay) // Todo - Track how much we were owed for the day we weren't paid. Don't assume it's constant.
                AssignGeneratedTask(new ActWrapperTask(new GetMoneyAct(this, Math.Min(Stats.DailyPay * NumDaysNotPaid, Faction.Economy.Funds)) { IncrementDays = false })
                {
                    AutoRetry = true,
                    Name = "Get paid.",
                    Priority = TaskPriority.High
                });

            // If we're bored, make sure we queue up a boredom reducing task.
            if (Stats.Boredom.IsDissatisfied() && !Tasks.Any(task => task.BoredomIncrease < 0) && ChooseIdleTask(true).HasValue(out var idleTask))
                AssignGeneratedTask(idleTask);

            // Track unhappiness time.
            if (Stats.Happiness.IsSatisfied()) 
                UnhappinessTime = 0.0f;
            else
                UnhappinessTime += gameTime.ElapsedGameTime.TotalMinutes;

            if (CurrentTask.HasValue(out var currentTask))
            {
                bool processAct = true;

                Stats.Boredom.CurrentValue -= (float)(currentTask.BoredomIncrease * gameTime.ElapsedGameTime.TotalSeconds);
                Stats.Energy.CurrentValue += (float)(currentTask.EnergyDecrease * gameTime.ElapsedGameTime.TotalSeconds);
           
                if (!CurrentAct.HasValue()) // Should be impossible to have a current task and no current act.
                {
                    // Try and recover the correct act.
                    // <blecki> I always run with a breakpoint set here... just in case.
                    ChangeAct(currentTask.CreateScript(Creature));

                    // This is a bad situation!
                    if (!CurrentAct.HasValue())
                    {
                        ChangeTask(null);
                        processAct = false;
                    }
                }

                if (processAct && CurrentAct.HasValue(out Act currentAct))
                {
                    try
                    {
                        var status = currentAct.Tick();
                        var retried = false;

                        if (CurrentAct.HasValue(out Act newCurrentAct))
                            if (status == Act.Status.Fail)
                            {
                                LastFailedAct = newCurrentAct.Name;

                                if (!FailedTasks.Any(task => task.TaskFailure.Equals(currentTask)))
                                    FailedTasks.Add(new FailedTask() { TaskFailure = currentTask, FailedTime = World.Time.CurrentDate });

                                if (currentTask.ShouldRetry(Creature))
                                    if (!Tasks.Contains(currentTask))
                                    {
                                        ReassignCurrentTask();
                                        retried = true;
                                    }
                            }

                        if (currentTask.IsComplete(World) || currentTask.WasCancelled)
                            ChangeTask(null);
                        else if (status != Act.Status.Running && !retried)
                            ChangeTask(null);
                    }
                    catch (Exception e)
                    {
                        Program.LogSentryBreadcrumb("Error", currentAct.ToString());
                        Program.CaptureException(new Exception("Dwarf Act threw", e));
                        ChangeTask(null);
                    }
                }
            }
            else
            {
                if (Stats.Happiness.IsSatisfied()) // We're happy, so make sure we aren't on strike.
                {
                    Stats.IsOnStrike = false;
                    UnhappinessTime = 0.0f;
                }

                if (Stats.IsOnStrike) // We're on strike, so track how long this job has sucked.
                {
                    UnhappinessTime += gameTime.ElapsedGameTime.TotalMinutes;
                    if (UnhappinessTime > GameSettings.Current.HoursUnhappyBeforeQuitting) // If we've been unhappy long enough, quit.
                    {
                        if (GetRoot().GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                        {
                            Manager.World.MakeAnnouncement( // Can't use a popup because the dwarf will soon not exist. Also - this is a serious event!
                                Message: String.Format("{0} has quit! The last straw: {1}", Stats.FullName, thoughts.Thoughts.Last(t => t.HappinessModifier < 0.0f).Description),
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
                    UnhappinessTime += gameTime.ElapsedGameTime.TotalMinutes;
                    if (MathFunctions.Rand(0, 10) < UnhappinessTime) // We hate it so much that we might just go on strike! The longer we are unhappy, the more likely to go on strike.
                    {
                        Manager.World.UserInterface.MakeWorldPopup(String.Format("{0} refuses to work!",
                               Stats.FullName), Creature.Physics, -10, 10);
                        Manager.World.Tutorial("happiness");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.25f);
                        Stats.IsOnStrike = true;
                    }
                }

                if (!Stats.IsOnStrike) // We aren't on strike, so find a new task.
                {
                    var goal = GetEasiestTask(Tasks);
                    if (goal == null) // We don't have a queued task, so lets go get one.
                    {
                        var motivationThreshold = 5.0f * (1.0f - Stats.Motivation.CurrentValue); // The higher the value, the more likely they are to pick a new task.
                        if (TimeSinceLastAssignedTask > motivationThreshold) // The longest a dwarf should wait even when incredibly unmotivated is 5 real life minutes.
                            goal = World.TaskManager.GetBestTask(this);
                    }

                    if (goal != null)
                    {
                        IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                        ChangeTask(goal);
                    }
                    else
                        ChangeTask(ActOnIdle());
                }
                else
                    ChangeTask(ActOnIdle());

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
                    Creature.Damage(FrameDeltaTime, Movement.CanSwim ? 1.0f : 30.0f, Health.DamageType.Normal);
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
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, XP > 0 ? GameSettings.Current.Colors.GetColor("Positive", Color.Green) : GameSettings.Current.Colors.GetColor("Negative", Color.Red));

            if (Faction == Manager.World.PlayerFaction && lastXPAnnouncementStat != Stats.GetCurrentLevel() && Stats.CanSpendXP)
            {
                lastXPAnnouncementStat = Stats.GetCurrentLevel();

                Manager.World.MakeAnnouncement(new Gui.Widgets.QueuedAnnouncement
                {
                    Text = String.Format("{0} is ready to spend some XP!", Stats.FullName),
                    ClickAction = (gui, sender) => GameStateManager.PushState(new EconomyState(Manager.World.Game, Manager.World)) // This should open the employee view instead.
                });

                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
                Manager.World.Tutorial("level up");
            }

        }

        protected override void MakeBattleAnnouncement(CreatureAI Enemy)
        {
            Manager.World.MakeAnnouncement(new Gui.Widgets.QueuedAnnouncement
            {
                Text = String.Format("{0} is fighting {1}.", Stats.FullName, TextGenerator.IndefiniteArticle(Enemy.Stats.CurrentClass.HasValue(out var c) ? c.Name : "cretin")),
                ClickAction = (gui, sender) => ZoomToMe()
            });

            Manager.World.Tutorial("combat");
        }

        public override Task ActOnIdle()
        {
            // Get out of the water!
            if (Creature.Physics.IsInLiquid)
                return new FindLandTask();

            if (ChooseIdleTask(false).HasValue(out var task))
                return task;
            else
                return null;
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
