using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class WanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public float Radius { get; set; }
        public Vector3 LocalTarget { get; set; }
        public WanderAct()
        {
            
        }

        public WanderAct(CreatureAI creature, float seconds, float turnTime, float radius) :
            base(creature)
        {
            Name = "Wander " + seconds;
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            Radius = radius;
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            LocalTarget = Agent.Position;
            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {
            Vector3 oldPosition = Agent.Position;
            bool firstIter = true;
            Creature.Controller.Reset();
            
            while(!WanderTime.HasTriggered)
            {
                WanderTime.Update(Act.LastTime);
                if(TurnTime.Update(Act.LastTime) || TurnTime.HasTriggered || firstIter)
                {
                    LocalTarget = new Vector3(MathFunctions.Rand() * Radius - Radius / 2.0f, 0.0f, MathFunctions.Rand() * Radius - Radius / 2.0f) + oldPosition;
                    firstIter = false;
                }

                Vector3 output = Creature.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds, LocalTarget, Agent.Position);
                output.Y = 0.0f;

                Creature.Physics.ApplyForce(output, (float) Act.LastTime.ElapsedGameTime.TotalSeconds);

                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }

}