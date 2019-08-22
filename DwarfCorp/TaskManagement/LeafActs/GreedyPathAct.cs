using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GreedyPathAct : CompoundCreatureAct
    {
        public int PathLength { get; set; }
        public bool Is2D { get; set; }
        public GameComponent Target { get; set; }
        public float Threshold { get; set; }

        public GreedyPathAct()
        {

        }

        public GreedyPathAct(CreatureAI creature, GameComponent target, float threshold)
            : base(creature)
        {
            Target = target;
            Threshold = threshold;
        }

        public IEnumerable<Status> FindGreedyPath()
        {
            Vector3 target = Target.Position;

            if (Is2D) target.Y = Creature.AI.Position.Y;
            List<MoveAction> path = new List<MoveAction>();
            var curr = Creature.Physics.CurrentVoxel;
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            var storage = new MoveActionTempStorage();
            for (int i = 0; i < PathLength; i++)
            {
                IEnumerable<MoveAction> actions =
                    Creature.AI.Movement.GetMoveActions(new MoveState() { Voxel = curr }, bodies, storage);

                MoveAction? bestAction = null;
                float bestDist = float.MaxValue;

                foreach (MoveAction action in actions)
                {
                    // Prevents a stack overflow due to "DestroyObject" task creating a FollowPathAct!
                    if (action.MoveType == MoveType.DestroyObject)
                    {
                        continue;
                    }
                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared();

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestAction = action;
                    }
                }

                Vector3 half = Vector3.One*0.5f;
                if (bestAction.HasValue &&
                    !path.Any(p => p.DestinationVoxel.Equals(bestAction.Value.DestinationVoxel) && p.MoveType == bestAction.Value.MoveType))
                {
                    path.Add(bestAction.Value);
                    MoveAction action = bestAction.Value;
                    action.DestinationVoxel = curr;
                    curr = bestAction.Value.DestinationVoxel;
                    bestAction = action;

                    if (((bestAction.Value.DestinationVoxel.WorldPosition + half) - target).Length() < Threshold)
                    {
                        break;
                    }
                }

            }

            if (path.Count > 0)
            {
                Creature.AI.Blackboard.SetData("RandomPath", path);
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }

        public override void Initialize()
        {
            Tree = new Sequence(new Wrap(FindGreedyPath), new FollowPathAct(Creature.AI, "RandomPath"));
            base.Initialize();
        }
    }
}