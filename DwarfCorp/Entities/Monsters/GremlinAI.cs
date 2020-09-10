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
    public class GremlinAI : CreatureAI
    {
        public float DestroyPlayerObjectProbability = -1.0f;
        public string PlantBomb = null;
        public Timer LeaveWorldTimer = new Timer(200, true);

        public GremlinAI()
        {

        }

        public GremlinAI(ComponentManager Manager, String Name, EnemySensor Sensor)
        : base(Manager, Name, Sensor) { }

        public override Task ActOnIdle()
        {
            if (DestroyPlayerObjectProbability > 0 && MathFunctions.RandEvent(DestroyPlayerObjectProbability))
            {
                bool plantBomb = !String.IsNullOrEmpty(PlantBomb) && MathFunctions.RandEvent(0.5f);
                if (!plantBomb && World.PlayerFaction.OwnedObjects.Count > 0)
                {
                    var thing = Datastructures.SelectRandom<GameComponent>(World.PlayerFaction.OwnedObjects);
                    AssignTask(new KillEntityTask(thing, KillEntityTask.KillType.Auto));
                }
                else if (plantBomb)
                {
                    var room = World.FindNearestZone(Position);
                    if (room != null)
                    {
                        AssignTask(new ActWrapperTask(new Sequence(new GoToZoneAct(this, room), new Do(() => { EntityFactory.CreateEntity<GameComponent>(PlantBomb, Position); return true; }))) { Priority = TaskPriority.High });
                    }
                    else if (World.PlayerFaction.OwnedObjects.Count > 0)
                    {
                        var thing = Datastructures.SelectRandom<GameComponent>(World.PlayerFaction.OwnedObjects);
                        AssignTask(new ActWrapperTask(new Sequence(new GoToEntityAct(thing, this), new Do(() => { EntityFactory.CreateEntity<GameComponent>(PlantBomb, Position); return true; }))) { Priority = TaskPriority.High });
                    }
                }
            }

            LeaveWorldTimer.Update(FrameDeltaTime);

            if (LeaveWorldTimer.HasTriggered)
            {
                LeaveWorld();
                LeaveWorldTimer.Reset();
            }

            return base.ActOnIdle();
        }
    }
}
