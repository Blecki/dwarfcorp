using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature attempts to slow its velocity until stopped.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StopAct : CreatureAct
    {
        public float StopForce { get; set; }

        public StopAct(CreatureAIComponent agent) :
            base(agent)
        {
            Name = "Stop";
            StopForce = Agent.Stats.StoppingForce;
        }

        public override IEnumerable<Status> Run()
        {
            bool hasStopped = false;

            while(!hasStopped)
            {
                if(Agent.Velocity.LengthSquared() < 0.5f)
                {
                    yield return Status.Success;
                    hasStopped = true;
                    break;
                }
                else
                {
                    Agent.Velocity /= StopForce;
                    yield return Status.Running;
                }
            }
        }
    }

}