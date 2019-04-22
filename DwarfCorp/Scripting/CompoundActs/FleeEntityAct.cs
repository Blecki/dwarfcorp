using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class FleeEntityAct : CompoundCreatureAct
    {
        public int PathLength;
        public GameComponent Entity;

        public FleeEntityAct()
        {

        }

        public FleeEntityAct(CreatureAI creature)
            : base(creature)
        {

        }

        public IEnumerable<Status> FindPath()
        {
            Vector3 target = Entity.Position;
            if (Creature.AI.PositionConstraint.Contains(target) == ContainmentType.Disjoint)
            {
                target = MathFunctions.RandVector3Box(Creature.AI.PositionConstraint);
            }

            List<MoveAction> path = new List<MoveAction>();
            VoxelHandle curr = Creature.Physics.CurrentVoxel;
            var storage = new MoveActionTempStorage();

            for (int i = 0; i < PathLength; i++)
            {
                var actions = Creature.AI.Movement.GetMoveActions(new MoveState() { Voxel = curr }, new List<GameComponent>(), storage);

                MoveAction? bestAction = null;
                float bestDist = float.MinValue;
                foreach (MoveAction action in actions)
                {
                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared();

                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        bestAction = action;
                    }
                }

                if (bestAction.HasValue &&
                    !path.Any(p => p.DestinationVoxel.Equals(bestAction.Value.DestinationVoxel) && p.MoveType == bestAction.Value.MoveType &&
                                   Creature.AI.PositionConstraint.Contains(bestAction.Value.DestinationVoxel.WorldPosition + Vector3.One * 0.5f) == ContainmentType.Contains))
                {
                    MoveAction action = bestAction.Value;
                    action.DestinationVoxel = curr;
                    path.Add(action);
                    curr = bestAction.Value.DestinationVoxel;
                }
                else
                {
                    break;
                }
            }
            if (path.Count > 0)
            {
                Creature.AI.Blackboard.SetData("FleePath", path);
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }

        public override void Initialize()
        {
            Tree = new Sequence(new Wrap(FindPath), new FollowPathAct(Creature.AI, "FleePath"));
            base.Initialize();
        }
    }
}