using System.Collections.Generic;

namespace DwarfCorp
{
    public class LeaveWorldTask : Task
    {
        public Timer DieTimer = new Timer(60, true);

        public LeaveWorldTask()
        {
            ReassignOnDeath = false;
        }

        public IEnumerable<Act.Status> GreedyFallbackBehavior(Creature agent)
        {
            var edgeGoal = new EdgeGoalRegion();

            while (true)
            {
                DieTimer.Update(agent.AI.FrameDeltaTime);
                if (DieTimer.HasTriggered)
                {
                    foreach(var status in Die(agent))
                    {
                        continue;
                    }
                    yield break;
                }
                var creatureVoxel = agent.Physics.CurrentVoxel;
                List<MoveAction> path = new List<MoveAction>();
                var storage = new MoveActionTempStorage();
                for (int i = 0; i < 10; i++)
                {
                    if (edgeGoal.IsInGoalRegion(creatureVoxel))
                    {
                        foreach (var status in Die(agent))
                            continue;
                        yield return Act.Status.Success;
                        yield break;
                    }

                    var actions = agent.AI.Movement.GetMoveActions(new MoveState { Voxel = creatureVoxel }, new List<GameComponent>(), storage);

                    float minCost = float.MaxValue;
                    var minAction = new MoveAction();
                    bool hasMinAction = false;
                    foreach (var action in actions)
                    {
                        var vox = action.DestinationVoxel;

                        float cost = edgeGoal.Heuristic(vox) * 10 + MathFunctions.Rand(0.0f, 0.1f) + agent.AI.Movement.Cost(action.MoveType);

                        if (cost < minCost)
                        {
                            minAction = action;
                            minCost = cost;
                            hasMinAction = true;
                        }
                    }

                    if (hasMinAction)
                    {
                        path.Add(minAction);
                        creatureVoxel = minAction.DestinationVoxel;
                    }
                    else
                    {
                        foreach (var status in Die(agent))
                            continue;
                        yield return Act.Status.Success;
                        yield break;
                    }
                }
                if (path.Count == 0)
                {
                    foreach (var status in Die(agent))
                        continue;
                    yield return Act.Status.Success;
                    yield break;
                }
                agent.AI.Blackboard.SetData("GreedyPath", path);
                var pathAct = new FollowPathAct(agent.AI, "GreedyPath");
                pathAct.Initialize();

                foreach (Act.Status status in pathAct.Run())
                {
                    yield return Act.Status.Running;
                }
                yield return Act.Status.Running;
            }
        }

        public IEnumerable<Act.Status> Die(Creature agent)
        {
            agent.GetRoot().Delete();
            yield return Act.Status.Success;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return new Select(
                new Sequence(new SetBlackboardData<VoxelHandle>(agent.AI, "EdgeVoxel", VoxelHandle.InvalidHandle),
                             new Repeat(
                                 new Sequence(
                                    new PlanAct(agent.AI, "PathToVoxel", "EdgeVoxel", PlanAct.PlanType.Edge) { MaxTimeouts = 1 },
                                    new FollowPathAct(agent.AI, "PathToVoxel"))
                                    , 4, true),
                             new Wrap(() => Die(agent)) { Name = "Die" }
                             ),
                new Wrap(() => GreedyFallbackBehavior(agent)) { Name = "Go to edge of world." }
                );
        }
    }
}