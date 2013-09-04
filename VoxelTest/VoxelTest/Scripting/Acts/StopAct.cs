using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class StopAct : Act
    {
        public CreatureAIComponent Agent { get; set;}
        public float StopForce { get; set; }

        public StopAct(CreatureAIComponent agent, float stopForce)
        {
            Agent = agent;
            StopForce = stopForce;
        }

        public override IEnumerable<Status> Run()
        {
            bool hasStopped = false;

            while (!hasStopped)
            {
                if (Agent.Velocity.LengthSquared() < 0.5f)
                {
                    yield return Status.Success;
                    hasStopped = true;
                    break;
                }
                else
                {
                    Agent.Velocity *= StopForce;
                    yield return Status.Running;
                }
            }

        }
    }
}
