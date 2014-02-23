using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Does nothing for a given number of seconds, and then returns success.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Wait : Act
    {
        public Timer Time { get; set; }

        public Wait(float time)
        {
            Name = "Wait " + time;
            Time = new Timer(time, true);
        }

        public Wait(Timer time)
        {
            Name = "Wait " + time.TargetTimeSeconds;
            Time = time;
        }

        public override void Initialize()
        {
            Time.Reset(Time.TargetTimeSeconds);
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            while(!Time.HasTriggered)
            {
                Time.Update(Act.LastTime);
                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }

}