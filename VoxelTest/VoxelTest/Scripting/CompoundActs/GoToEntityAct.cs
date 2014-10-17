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


        public override void Initialize()
        {
            Creature.AI.Blackboard.Erase("PathToEntity");
            Creature.AI.Blackboard.Erase("EntityVoxel");
            Tree = new Sequence(
                new Wrap(() => Creature.ClearBlackboardData("PathToEntity")), 
                new Wrap(() => Creature.ClearBlackboardData("EntityVoxel")),
                InHands() |
                 new Sequence(new SetTargetVoxelFromEntityAct(Agent, EntityName, "EntityVoxel"),
                    new PlanAct(Agent, "PathToEntity", "EntityVoxel", PlanAct.PlanType.Adjacent),
                    new Parallel(new FollowPathAct(Agent, "PathToEntity"), new Wrap(CollidesWithTarget)) { ReturnOnAllSucces = false },
                    new StopAct(Agent)));
            Tree.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }

}