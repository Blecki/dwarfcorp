using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds a path from point A to point B and fills the blackboard with this information.
    /// </summary>
    public class PlanAct : CreatureAct
    {
        public Timer PlannerTimer;
        public int MaxExpansions;
        public string PathOut;
        public string TargetName;
        public List<MoveAction> Path { get { return GetPath(); } set {  SetPath(value);} }
        public VoxelHandle Target { get { return GetTarget(); } set {  SetTarget(value);} }
        public int MaxTimeouts { get; set; }
        public int Timeouts { get; set; }
        private bool WaitingOnResponse { get; set; }
        public float Radius { get; set; }
        public List<float> Weights { get; set; } 

        public enum PlanType
        {
            Adjacent,
            Into,
            Radius,
            Edge
        }

        public AStarPlanner.PlanResultCode LastResult;
        public PlanType Type { get; set; }

        public PlanAct()
        {

        }

        public PlanAct(CreatureAI agent, string pathOut, string target, PlanType planType) :
            base(agent)
        {
            Type = planType;
            Name = "Plan to " + target;
            PlannerTimer = new Timer(9999.0f, false, Timer.TimerMode.Real);
            MaxExpansions = 10000;
            PathOut = pathOut;
            TargetName = target;
            WaitingOnResponse = false;
            MaxTimeouts = 4;
            Timeouts = 0;
            Radius = 0;
            Weights = new List<float>();
            Weights.Add(MathFunctions.Rand(1.0f, 3.0f));
        }

        public override void OnCanceled()
        {
            base.OnCanceled();
        }

        public VoxelHandle GetTarget()
        {
            return Agent.Blackboard.GetData<VoxelHandle>(TargetName);
        }

        public void SetTarget(VoxelHandle target)
        {
            Agent.Blackboard.SetData(TargetName, target);
        }

        public List<MoveAction> GetPath()
        {
            return Agent.Blackboard.GetData<List<MoveAction>>(PathOut);
        }

        public void SetPath(List<MoveAction> path)
        {
            Agent.Blackboard.SetData(PathOut, path);
        }

        public GoalRegion GetGoal()
        {
            GoalRegion goal = null;
            switch (Type)
            {
                case PlanType.Radius:
                    goal = new SphereGoalRegion(Target, Radius);
                    break;
                case PlanType.Into:
                    goal = new VoxelGoalRegion(Target);
                    break;
                case PlanType.Adjacent:
                    goal = new AdjacentVoxelGoalRegion2D(Target);
                    break;
                case PlanType.Edge:
                    goal = new EdgeGoalRegion();
                    break;
            }
            return goal;
        }

        public List<MoveAction> ComputeGreedyFallback(int maxsteps = 10, List<VoxelHandle> exploredVoxels = null)
        {
            var toReturn = new List<MoveAction>();
            var goal = GetGoal();
            var creatureVoxel = Agent.Physics.CurrentVoxel;

            if (goal.IsInGoalRegion(creatureVoxel))
                return toReturn;

            var storage = new MoveActionTempStorage();
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            var currentVoxel = creatureVoxel;

            while (toReturn.Count < maxsteps)
            {
                var actions = Agent.Movement.GetMoveActions(new MoveState() { Voxel = currentVoxel }, bodies, storage);

                float minCost = float.MaxValue;
                var minAction = new MoveAction();
                bool hasMinAction = false;
               
                foreach (var action in actions)
                {
                    if (toReturn.Any(a => a.DestinationVoxel == action.DestinationVoxel && a.MoveType == action.MoveType))
                    {
                        continue;
                    }

                    var vox = action.DestinationVoxel;

                    float cost = goal.Heuristic(vox) * MathFunctions.Rand(1.0f, 1.1f) + Agent.Movement.Cost(action.MoveType);
                    if (exploredVoxels != null && exploredVoxels.Contains(action.DestinationVoxel))
                    {
                        cost *= 10;
                    }

                    if (cost < minCost)
                    {
                        minAction = action;
                        minCost = cost;
                        hasMinAction = true;
                    }
                }

                if (hasMinAction)
                {
                    MoveAction action = minAction;
                    action.DestinationVoxel = currentVoxel;
                    toReturn.Add(action);
                    currentVoxel = minAction.DestinationVoxel;
                    if (goal.IsInGoalRegion(minAction.DestinationVoxel))
                    {
                        return toReturn;
                    }
                }
                else
                {
                    return toReturn;
                }
            }
            return toReturn;
        }

        public override IEnumerable<Status> Run()
        {
            Path = null;
            Timeouts = 0;
            PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
            var lastId = -1;
            Vector3 goalPos = Vector3.Zero;
            Agent.Blackboard.SetData<bool>("NoPath", false);
            while (true)
            {
                if (Path != null)
                {
                    yield return Status.Success;
                    break;
                }

                if(Timeouts > MaxTimeouts)
                {
                    Agent.Blackboard.SetData<bool>("NoPath", true);
                    yield return Status.Fail;
                    break;
                }
                if (WaitingOnResponse && Debugger.Switches.DrawPaths)
                    Drawer3D.DrawLine(Creature.AI.Position, goalPos, Color.Blue, 0.25f);
                PlannerTimer.Update(DwarfTime.LastTime);

                ChunkManager chunks = Creature.Manager.World.ChunkManager;
                if (PlannerTimer.HasTriggered || Timeouts == 0)
                {
                    if (!Target.IsValid && Type != PlanType.Edge)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        Agent.Blackboard.SetData<bool>("NoPath", true);
                        yield return Status.Fail;
                        break;
                    }

                    var voxUnder = VoxelHelpers.FindValidVoxelNear(chunks, Agent.Position);

                    if (!voxUnder.IsValid)
                    {
                        if (Debugger.Switches.DrawPaths)
                            Creature.World.UserInterface.MakeWorldPopup(String.Format("Invalid request"), Creature.Physics, -10, 1);
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        Agent.Blackboard.SetData<bool>("NoPath", true);
                        yield return Status.Fail;
                        break;
                    }

                    Path = null;
                    AstarPlanRequest aspr = new AstarPlanRequest
                    {
                        Subscriber = Agent.PlanSubscriber,
                        Start = voxUnder,
                        MaxExpansions = MaxExpansions,
                        Sender = Agent,
                        HeuristicWeight = Weights[Timeouts]
                    };

                    lastId = aspr.ID;
                    aspr.GoalRegion = GetGoal();
                    goalPos = GetGoal().GetVoxel().GetBoundingBox().Center();
                    Agent.PlanSubscriber.Clear();
                    if(!Agent.PlanSubscriber.SendRequest(aspr, aspr.ID))
                    {
                        yield return Status.Fail;
                        yield break;
                    }
                    PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
                    WaitingOnResponse = true;
                    yield return Status.Running;


                    Timeouts++;
                }
                else
                {
                    Status statusResult = Status.Running;

                    while (Agent.PlanSubscriber.Responses.Count > 0)
                    {
                        AStarPlanResponse response;
                        if(!Agent.PlanSubscriber.Responses.TryDequeue(out response))
                        {
                            yield return Status.Running;
                            continue;
                        }
                        LastResult = response.Result;

                        if (response.Success && response.Request.ID == lastId)
                        {
                            Path = response.Path;
                            WaitingOnResponse = false;

                            statusResult = Status.Success;
                        }
                        else if (response.Request.ID != lastId && response.Path != null && response.Path.Count > 0)
                        {
                            var goal = GetGoal();
                            
                            bool obeysGoal = goal == null ? false : (response.Success && (goal.IsInGoalRegion(response.Path.Last().DestinationVoxel)));

                            if (Debugger.Switches.DrawPaths)
                            {
                                if (obeysGoal)
                                    Creature.World.UserInterface.MakeWorldPopup(String.Format("Using Old Path", response.Result), Creature.Physics, -10, 1);
                                else
                                    Creature.World.UserInterface.MakeWorldPopup(String.Format("Old Path Dropped", response.Result), Creature.Physics, -10, 1);
                            }

                            if (obeysGoal)
                            {
                                Path = response.Path;
                                WaitingOnResponse = false;
                                statusResult = Status.Success;
                            }
                            else
                                continue;
                        }
                        else if (response.Result == AStarPlanner.PlanResultCode.Invalid || response.Result == AStarPlanner.PlanResultCode.NoSolution
                            || response.Result == AStarPlanner.PlanResultCode.Cancelled || response.Result == AStarPlanner.PlanResultCode.Invalid)
                        {
                            if (Debugger.Switches.DrawPaths)
                                Creature.World.UserInterface.MakeWorldPopup(String.Format("Path: {0}", response.Result), Creature.Physics, -10, 1);
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            Agent.Blackboard.SetData<bool>("NoPath", true);
                            statusResult = Status.Fail;
                            yield return Status.Fail;
                        }
                        else if (Timeouts <= MaxTimeouts)
                        {
                            Timeouts++;
                            yield return Status.Running;
                        }
                        else
                        {
                            if (Debugger.Switches.DrawPaths)
                                Creature.World.UserInterface.MakeWorldPopup(String.Format("Max timeouts reached", response.Result), Creature.Physics, -10, 1);
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            Agent.Blackboard.SetData<bool>("NoPath", true);
                            statusResult = Status.Fail;
                        }
                    }
                    yield return statusResult;
                }
            }
        }
    }

    public class PlanWithGreedyFallbackAct : CreatureAct
    {
        public string VoxelName = "EntityVoxel";
        public string PathName = "PathToEntity";
        public PlanAct.PlanType PlanType = PlanAct.PlanType.Radius;
        public float Radius = 1;
        public int MaxTimeouts = 1;

        public PlanWithGreedyFallbackAct()
        {
            Name = "Get near goal";
        }

        public override IEnumerable<Status> Run()
        {

            while (true)
            {
                Creature.AI.Blackboard.Erase(PathName);
                Agent.Blackboard.SetData<bool>("NoPath", false);
                PlanAct planAct = new PlanAct(Creature.AI, PathName, VoxelName, PlanType) { Radius = Radius, MaxTimeouts = MaxTimeouts };
                planAct.Initialize();

                bool planSucceeded = false;
                while (true)
                {
                    Act.Status planStatus = planAct.Tick();

                    if (planStatus == Status.Fail)
                    {
                        yield return Act.Status.Running;
                        break;
                    }

                    else if (planStatus == Status.Running)
                    {
                        yield return Act.Status.Running;
                    }

                    else if (planStatus == Status.Success)
                    {
                        planSucceeded = true;
                        break;
                    }
                    yield return Act.Status.Running;
                }

                if (!planSucceeded && planAct.LastResult == AStarPlanner.PlanResultCode.MaxExpansionsReached)
                {
                    yield return Act.Status.Running;
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    Creature.Physics.Velocity = Vector3.Zero;
                    Timer planTimeout = new Timer(MathFunctions.Rand(30.0f, 120.0f), false, Timer.TimerMode.Real);
                    List<VoxelHandle> exploredVoxels = new List<VoxelHandle>();
                    Color debugColor = new Color(MathFunctions.RandVector3Cube() + Vector3.One * 0.5f);
                    float debugScale = MathFunctions.Rand() * 0.5f + 0.5f;
                    while (!planTimeout.HasTriggered)
                    {
                        // In this case, try to follow a greedy path toward the entity instead of just failing.
                        var greedyPath = planAct.ComputeGreedyFallback(20, exploredVoxels);
                        var goal = planAct.GetGoal();
                        Creature.AI.Blackboard.SetData("GreedyPath", greedyPath);
                        var greedyPathFollow = new FollowPathAct(Creature.AI, "GreedyPath")
                        {
                            //BlendEnd = true,
                            //BlendStart = false
                        };
                        greedyPathFollow.Initialize();

                        foreach (var currStatus in greedyPathFollow.Run())
                        {
                            if (Debugger.Switches.DrawPaths)
                            {
                                foreach (var voxel in exploredVoxels)
                                {
                                    Drawer3D.DrawBox(voxel.GetBoundingBox().Expand(-debugScale), debugColor, 0.05f, false);
                                }
                            }
                            if (!exploredVoxels.Contains(Agent.Physics.CurrentVoxel))
                            {
                                exploredVoxels.Add(Agent.Physics.CurrentVoxel);
                            }
                            if (Debugger.Switches.DrawPaths)
                            {
                                Drawer3D.DrawLine(Agent.Position, goal.GetVoxel().WorldPosition, debugColor, 0.1f);
                            }
                            if (goal.IsInGoalRegion(Agent.Physics.CurrentVoxel))
                            {
                                yield return Act.Status.Success;
                                yield break;
                            }
                            yield return Act.Status.Running;
                        }
                        planTimeout.Update(DwarfTime.LastTime);
                    }
                    continue;
                }
                else if (!planSucceeded)
                {
                    Agent.Blackboard.SetData<bool>("NoPath", true);
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Success;
                yield break;
            }

        }
    }
}