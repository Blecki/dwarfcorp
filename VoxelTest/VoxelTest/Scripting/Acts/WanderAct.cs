using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class WanderAct : Act
    {
        public CreatureAIComponent Creature { get; set; }
        public Timer WanderTime { get; set; }
        public float Radius { get; set; }

        public WanderAct(Creature creature, float seconds, float radius)
        {
            Name = "Wander " + seconds;
            WanderTime = new Timer(seconds, false);
            Radius = radius;
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            while (!WanderTime.HasTriggered)
            {
                Creature.Wander(Act.LastTime, Radius);
                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }
}
