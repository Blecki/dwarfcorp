using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds the voxel below a given entity, and goes to it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToEntityAct : CompoundCreatureAct
    {
        public Body Entity { get { return Agent.Blackboard.GetData<Body>(EntityName);  } set {Agent.Blackboard.SetData(EntityName, value);} }

        public string EntityName { get; set; }

        public GoToEntityAct()
        {

        }

        public bool EntityIsInHands()
        {
            return Entity == Agent.Hands.GetFirstGrab();
        }

        public Condition InHands()
        {
            return new Condition(EntityIsInHands);
        }

        public GoToEntityAct(string entity, CreatureAI creature) :
            base(creature)
        {
            Name = "Go to entity " + entity;
            EntityName = entity;
        }

        public GoToEntityAct(Body entity, CreatureAI creature) :
            base(creature)
        {
            Name = "Go to entity";
            EntityName = "TargetEntity";
            Entity = entity;
        }

        public IEnumerable<Status> CollidesWithTarget()
        {
            while (true)
            {
                if (Entity.BoundingBox.Intersects(Creature.Physics.BoundingBox))
                {
                    yield return Status.Success;
                    yield break;
                }

                yield return Status.Running;
            }
        }


        public override IEnumerable<Status> Run()
        {
            Tree = new Sequence(new SetTargetEntityAct(Entity, Agent),
                    InHands() |
                    new Sequence(new SetTargetVoxelFromEntityAct(Agent, "EntityVoxel"),
                        new PlanAct(Agent, "PathToEntity", "EntityVoxel", PlanAct.PlanType.Adjacent),
                        new Parallel(new FollowPathAct(Agent, "PathToEntity"),new Wrap(CollidesWithTarget)) {ReturnOnAllSucces = false},
                        new StopAct(Agent)));
            Tree.Initialize();
            return base.Run();
        }
    }

}