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
    public class FairyAI : CreatureAI
    {
        private Timer AutoGatherTimer = new Timer(MathFunctions.Rand() * 5 + 3, false);

        public FairyAI()
        {
        }

        public FairyAI(
            ComponentManager Manager,
            string name,
            EnemySensor sensor) :
            base(Manager, name, sensor)
        {
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera) 
        {
            if (!Active)
                return;

            Creature.NoiseMaker.BasePitch = Stats.VoicePitch;

            AutoGatherTimer.Update(gameTime);

            if (AutoGatherTimer.HasTriggered)
            {
                foreach (var body in World.EnumerateIntersectingRootObjects(Physics.BoundingBox.Expand(3.0f)).OfType<ResourceEntity>().Where(r => r.Active && r.AnimationQueue.Count == 0))
                    Creature.GatherImmediately(body, Inventory.RestockType.RestockResource);

                OrderEnemyAttack();
            }

            DeleteBadTasks();
            PreEmptTasks();

            if (CurrentTask.HasValue(out var currentTask))
            {
                bool processAct = true;

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

                    if (currentTask.IsComplete(World))
                        ChangeTask(null);
                    else if (status != Act.Status.Running && !retried)
                        ChangeTask(null);
                }
            }
            else
            {
                var goal = GetEasiestTask(Tasks);
                if (goal == null)
                    goal = World.TaskManager.GetBestTask(this);

                if (goal != null)
                    ChangeTask(goal);
                else
                    ChangeTask(ActOnIdle());
            }

            if (PositionConstraint.Contains(Physics.LocalPosition) == ContainmentType.Disjoint)
            {
                Physics.LocalPosition = MathFunctions.Clamp(Physics.Position, PositionConstraint);
                Physics.PropogateTransforms();
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

            return new LookInterestingTask();
        }
    }
}
