using System.Collections.Generic;

namespace DwarfCorp
{
    public class Wait : CreatureAct
    {
        public Timer Time { get; set; }

        public Wait(CreatureAI Creature, float time) : base(Creature)
        {
            Name = "Wait " + time;
            Time = new Timer(time, true);
        }

        public Wait(CreatureAI Creature, Timer time) : base(Creature)
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
            LastTickedChild = this;
            while(!Time.HasTriggered)
            {
                Time.Update(Agent.FrameDeltaTime);
                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }

}