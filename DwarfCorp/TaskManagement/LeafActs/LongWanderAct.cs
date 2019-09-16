using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class LongWanderAct : CompoundCreatureAct
    {
        public int PathLength { get; set; }
        public float Radius { get; set; }
        public bool Is2D { get; set; }
        public float SpeedAdjust = 1.0f;

        public LongWanderAct()
        {
            
        }

        public LongWanderAct(CreatureAI creature) : base(creature)
        {
            
        }

        public IEnumerable<Status> FindRandomPath()
        {
            var target = MathFunctions.RandVector3Cube()*Radius + Creature.AI.Position;

            if (Creature.AI.PositionConstraint.Contains(target) == ContainmentType.Disjoint)
                target = MathFunctions.RandVector3Box(Creature.AI.PositionConstraint);
            if (Is2D)
                target.Y = Creature.AI.Position.Y;

            var path = new List<MoveAction>();
            var curr = Creature.Physics.CurrentVoxel;
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            var storage = new MoveActionTempStorage();
            var previousMoveState = new MoveState { Voxel = curr };

            for (int i = 0; i < PathLength; i++)
            {
                var actions = Creature.AI.Movement.GetMoveActions(previousMoveState, bodies, storage);

                MoveAction? bestAction = null;
                var bestDist = float.MaxValue;

                foreach (MoveAction action in actions)
                {
                    if (Is2D && (action.MoveType == MoveType.Climb || action.MoveType == MoveType.ClimbWalls || action.MoveType == MoveType.Fall))
                        continue;

                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared() * Creature.AI.Movement.Cost(action.MoveType);

                    if (dist < bestDist && ! path.Any(a => a.DestinationVoxel == action.DestinationVoxel))
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
                    path.Add(action);
                    previousMoveState = action.DestinationState;
                }
                else
                    break;
            }

            if (path.Count > 0)
            {
                Creature.AI.Blackboard.SetData("RandomPath", path);
                yield return Status.Success;
            }
            else
                yield return Status.Fail;
        }

        public override void Initialize()
        {
            Tree = new Sequence(new Wrap(FindRandomPath), new FollowPathAct(Creature.AI, "RandomPath", SpeedAdjust));
            base.Initialize();
        }
    }

}