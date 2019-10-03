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
    public class KoboldAI : CreatureAI
    {
        public float StealFromPlayerProbability = 0.5f;
        public Timer LeaveWorldTimer = new Timer(200, true);

        public KoboldAI()
        {

        }

        public KoboldAI(ComponentManager Manager, String Name, EnemySensor Sensor)
        : base(Manager, Name, Sensor) { }

        public override Task ActOnIdle()
        {
            if (StealFromPlayerProbability > 0 && MathFunctions.RandEvent(StealFromPlayerProbability))
            {
                var resources = World.ListResources();
                if (resources.Count > 0)
                {
                    var resource = Datastructures.SelectRandom(resources);
                    if (resource.Value.Count > 0)
                    {
                        AssignTask(new ActWrapperTask(new GetResourcesOfType(this, new List<ResourceTypeAmount>() { new ResourceTypeAmount(resource.Value.Type, 1) })) { Name = "Steal stuff", Priority = TaskPriority.High });
                    }
                }
            }

            LeaveWorldTimer.Update(DwarfTime.LastTime);

            if (LeaveWorldTimer.HasTriggered)
            {
                LeaveWorld();
                LeaveWorldTimer.Reset();
            }

            return base.ActOnIdle();
        }
    }
}
