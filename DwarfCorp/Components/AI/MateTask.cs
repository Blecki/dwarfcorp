using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MateTask : Task
    {
        public CreatureAI Them;

        public MateTask()
        {
            ReassignOnDeath = false;
        }
        public MateTask(CreatureAI closestMate)
        {
            Them = closestMate;
            Name = "Mate with " + closestMate.GlobalID;
            ReassignOnDeath = false;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (Them == null || Them.IsDead || agent == null || agent.AI == null || agent.IsDead)
            {
                return true;
            }

            return base.ShouldDelete(agent);
        }

        public override bool IsComplete(WorldManager World)
        {
            if (Them == null || Them.IsDead)
                return true;
            return false;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (Them == null || Them.IsDead ||  agent == null || agent.IsDead || agent.AI == null)
                return float.MaxValue;
            return (Them.Position - agent.AI.Position).LengthSquared();
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (Them == null || Them.IsDead || agent == null || agent.IsDead || agent.AI == null)
                return Feasibility.Infeasible;
            return Mating.CanMate(agent, Them.Creature) ? Feasibility.Feasible : Feasibility.Infeasible ;
        }

        public IEnumerable<Act.Status> Mate(Creature me)
        {
            Timer mateTimer = new Timer(5.0f, true);
            while (!mateTimer.HasTriggered)
            {
                if (me.IsDead || Them == null || Them.IsDead)
                {
                    yield return Act.Status.Fail;
                    yield break;
                }

                me.Physics.Velocity = Vector3.Zero;
                Them.Physics.Velocity = Vector3.Zero;
                Them.Physics.LocalPosition = me.Physics.Position*0.1f + Them.Physics.Position*0.9f;
                if (MathFunctions.RandEvent(0.01f))
                {
                    me.NoiseMaker.MakeNoise("Hurt", me.AI.Position, true, 0.1f);
                    me.World.ParticleManager.Trigger("puff", me.AI.Position, Color.White, 1);
                }
                mateTimer.Update(me.AI.FrameDeltaTime);
                yield return Act.Status.Running;
            }

            if (me.IsDead || Them == null || Them.IsDead)
            {
                yield return Act.Status.Fail;
                yield break;
            }


            if (Mating.CanMate(me, Them.Creature))
                Mating.Mate(me, Them.Creature, me.World.Time);
            else
            {
                AutoRetry = false;
                me.AI.SetTaskFailureReason("Failed to mate.");
                yield return Act.Status.Fail;
            }

            yield return Act.Status.Success;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return new Sequence(new GoToEntityAct(Them.Physics, agent.AI),
                                new Wrap(() => Mate(agent)));
        }
    }
}
