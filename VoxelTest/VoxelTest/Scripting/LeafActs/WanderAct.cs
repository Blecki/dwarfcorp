using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class WanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public float Radius { get; set; }

        public WanderAct()
        {
            
        }

        public WanderAct(CreatureAIComponent creature, float seconds, float turnTime, float radius) :
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
            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {
            Vector3 oldPosition = Agent.Position;
            while(!WanderTime.HasTriggered)
            {
                WanderTime.Update(Act.LastTime);
                if(TurnTime.Update(Act.LastTime) || TurnTime.HasTriggered)
                {
                    Creature.LocalTarget = new Vector3((float) PlayState.Random.NextDouble() * Radius - Radius / 2.0f, 0.0f, (float) PlayState.Random.NextDouble() * Radius - Radius / 2.0f) + oldPosition;
                }

                Vector3 output = Creature.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
                output.Y = 0.0f;

                Creature.Physics.ApplyForce(output, (float) Act.LastTime.ElapsedGameTime.TotalSeconds);

                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }

}