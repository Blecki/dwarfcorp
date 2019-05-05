using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class HealOtherDwarfAct : CompoundCreatureAct
    {
        public CreatureAI Ally { get; set; }

        public HealOtherDwarfAct()
        {
            Name = "Heal a friend";
        }

        public HealOtherDwarfAct(CreatureAI me, CreatureAI other) :
            base(me)
        {
            Ally = other;
            Name = String.Format("Heal {0}", other.Stats.FullName);
        }

        public override void OnCanceled()
        {
            if (Ally != null)
                Ally.ResetPositionConstraint();
            base.OnCanceled();
        }

        public IEnumerable<Act.Status> PlaceOnBed(GameComponent bed)
        {
            Ally.ResetPositionConstraint();
            var pos = bed.GetRotatedBoundingBox().Center() + Vector3.Up * 0.5f;
            Ally.Physics.LocalPosition = pos;
            Ally.PositionConstraint = new BoundingBox(pos - new Vector3(0.5f, 0.0f, 0.5f), Agent.Position + new Vector3(0.5f, 0.5f, 0.5f));
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ReleaseAlly()
        {
            Ally.ResetPositionConstraint();
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> PickupAlly()
        {
            Ally.PositionConstraint = new BoundingBox(Agent.Position - new Vector3(0.5f, 0.0f, 0.5f), Agent.Position + new Vector3(0.5f, 1.6f, 0.5f));
            Ally.Physics.LocalPosition = Agent.Position + Vector3.Up * 0.5f + Agent.Physics.GlobalTransform.Forward * 1.0f;
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> HealAlly()
        {
            Timer healTimer = new Timer(5.0f, false);
            while (!Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied())
            {
                Agent.Physics.Face(Ally.Position);
                Agent.Creature.CurrentCharacterMode = CharacterMode.Sitting;
                healTimer.Update(DwarfTime.LastTime);
                if (healTimer.HasTriggered)
                {
                    int amount = MathFunctions.RandInt(1, (int)Agent.Stats.Wisdom);
                    Ally.Creature.Heal(amount);
                    IndicatorManager.DrawIndicator((amount).ToString() + " HP",
                    Ally.Position, 1.0f,
                    GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
                    Ally.Creature.DrawLifeTimer.Reset();
                }
                yield return Act.Status.Running;
            }
            Ally.ResetPositionConstraint();
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            var closestBed = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true, null);

            if (closestBed == null)
            {
                Tree = new Select(new Sequence(
                    new Domain(() => !Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied(),
                        new GoToEntityAct(Ally.Physics, Agent)),
                        new Wrap(() => PickupAlly()),
                        new Wrap(() => HealAlly()) { Name = "Do CPR." }),
                        new Wrap(() => ReleaseAlly())) | new Wrap(() => ReleaseAlly());
            }
            else
            {
                Tree = new Select(
                        new Sequence(
                            new Domain(() => !Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied(), 
                                new GoToEntityAct(Ally.Physics, Agent)),
                            new Domain(() => !Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied(), 
                                new Parallel(
                                        new Repeat(new Wrap(() => PickupAlly()), -1, false),
                                        new GoToEntityAct(closestBed, Agent)) { ReturnOnAllSucces = false }),
                            new Domain(() => !Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied(), 
                                new Wrap(() => PlaceOnBed(closestBed))),
                            new Wrap(() => HealAlly()) {  Name = "Do CPR."}, new Wrap(() => ReleaseAlly())),
                        new Wrap(() => ReleaseAlly()));
            }

            base.Initialize();
        }
    }

    public class MagicHealAllyAct : CompoundCreatureAct
    {
        public CreatureAI Ally { get; set; }

        public MagicHealAllyAct()
        {
            Name = "Heal a friend";
        }

        public MagicHealAllyAct(CreatureAI me, CreatureAI other) :
            base(me)
        {
            Ally = other;
            Name = String.Format("Heal {0}", other.Stats.FullName);
        }


        public IEnumerable<Act.Status> HealAlly()
        {
            Timer healTimer = new Timer(1.0f, false);
            while (!Ally.IsDead && !Ally.Stats.Status.Health.IsSatisfied() && (Ally.Position - Agent.Position).Length() < 3)
            {
                Agent.Physics.Face(Ally.Position);
                Agent.Creature.CurrentCharacterMode = Agent.Creature.AttackMode;
                healTimer.Update(DwarfTime.LastTime);
                if (healTimer.HasTriggered)
                {
                    Agent.Creature.Sprite.ReloopAnimations(Agent.Creature.AttackMode);
                    int amount = Agent.Stats.CurrentLevel.HealingPower;
                    Ally.Creature.Heal(amount);
                    IndicatorManager.DrawIndicator((amount).ToString() + " HP",
                    Ally.Position, 1.0f,
                    GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
                    Ally.Creature.DrawLifeTimer.Reset();
                    Agent.World.ParticleManager.TriggerRay("star_particle", Agent.Position, Ally.Position);
                    SoundManager.PlaySound(ContentPaths.Audio.tinkle, Agent.Position, true, 1.0f);
                }
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            Tree = new Select(new Sequence(
                    new Domain(() => !Ally.IsDead &&! Ally.Stats.Status.Health.IsSatisfied(),
           new GoToEntityAct(Ally.Physics, Agent)),
           new Wrap(() => HealAlly()) { Name = "Heal ally." }));
            base.Initialize();
        }
    }


    public class MagicHealAllyTask : Task
    {
        public CreatureAI Ally;
        public MagicHealAllyTask()
        {
            Name = "Heal Ally Magically";
            ReassignOnDeath = false;
            Priority = PriorityType.High;
        }

        public MagicHealAllyTask(CreatureAI ally)
        {
            Ally = ally;
            Name = String.Format("Heal {0} magically.", ally.Stats.FullName);
            ReassignOnDeath = false;
            Priority = PriorityType.High;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.AI != Ally && !Ally.IsDead && !Ally.Stats.Status.Health.IsSatisfied() && agent.Stats.CurrentLevel.HealingPower > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool IsComplete(Faction faction)
        {
            return Ally.IsDead || Ally.Stats.Status.Health.IsSatisfied();
        }

        public override Act CreateScript(Creature agent)
        {
            return new MagicHealAllyAct(agent.AI, Ally);
        }
    }
    public class HealAllyTask : Task
    {
        public CreatureAI Ally;
        public HealAllyTask()
        {
            Name = "Heal Ally";
            ReassignOnDeath = true;
            Priority = PriorityType.High;
        }

        public HealAllyTask(CreatureAI ally)
        {
            Ally = ally;
            Name = String.Format("Heal {0}", ally.Stats.FullName);
            ReassignOnDeath = true;
            Priority = PriorityType.High;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.AI != Ally && !Ally.IsDead && Ally.Stats.Status.Health.IsDissatisfied() ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool IsComplete(Faction faction)
        {
            return Ally.IsDead || !Ally.Stats.Status.Health.IsDissatisfied();
        }

        public override Act CreateScript(Creature agent)
        {
            return new HealOtherDwarfAct(agent.AI, Ally);
        }
    }
}
