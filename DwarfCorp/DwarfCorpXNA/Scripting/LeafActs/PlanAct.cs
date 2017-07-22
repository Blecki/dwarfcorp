// PlanAct.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds a path from point A to point B and fills the blackboard with
    /// this information.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlanAct : CreatureAct
    {
        public Timer PlannerTimer { get; set; }
        public int MaxExpansions { get; set; }

        public string PathOut { get; set; }

        public string TargetName { get; set; }

        public List<MoveAction> Path { get { return GetPath(); } set {  SetPath(value);} }
        public TemporaryVoxelHandle Target { get { return GetTarget(); } set {  SetTarget(value);} }

        public PlanSubscriber PlanSubscriber { get; set; }

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


        public PlanType Type { get; set; }

        public PlanAct()
        {

        }

        public PlanAct(CreatureAI agent, string pathOut, string target, PlanType planType) :
            base(agent)
        {
            Type = planType;
            Name = "Plan to " + target;
            PlannerTimer = new Timer(1.0f, false);
            MaxExpansions = 1000;
            PathOut = pathOut;
            TargetName = target;
            PlanSubscriber = new PlanSubscriber(agent.Manager.World.PlanService);
            WaitingOnResponse = false;
            MaxTimeouts = 4;
            Timeouts = 0;
            Radius = 0;
            Weights = new List<float> {1.0f, 10.0f, 20.0f, 30.0f, 40.0f};
        }

        public TemporaryVoxelHandle GetTarget()
        {
            return Agent.Blackboard.GetData<TemporaryVoxelHandle>(TargetName);
        }

        public void SetTarget(TemporaryVoxelHandle target)
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

        public static bool PathExists(TemporaryVoxelHandle voxA, TemporaryVoxelHandle voxB, CreatureAI creature)
        {
            var path = AStarPlanner.FindPath(creature.Movement, voxA, new VoxelGoalRegion(voxB), creature.Manager.World.ChunkManager, 1000, 1);
            return path != null && path.Count > 0;
        }

        public override IEnumerable<Status> Run()
        {
            Path = null;
            Timeouts = 0;
            PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);

            while (true)
            {
                if (Path != null)
                {
                    yield return Status.Success;
                    break;
                }

                if(Timeouts > MaxTimeouts)
                {
                    yield return Status.Fail;
                    break;
                }

                PlannerTimer.Update(DwarfTime.LastTime);

                ChunkManager chunks = Creature.Manager.World.ChunkManager;
                if (PlannerTimer.HasTriggered || Timeouts == 0)
                {
                    if (!Target.IsValid && Type != PlanType.Edge)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    var voxUnder = new TemporaryVoxelHandle(chunks.ChunkData,
                        GlobalVoxelCoordinate.FromVector3(Agent.Position));

                    if (!voxUnder.IsValid)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    Path = null;
                    AstarPlanRequest aspr = new AstarPlanRequest
                    {
                        Subscriber = PlanSubscriber,
                        Start = voxUnder,
                        MaxExpansions = MaxExpansions,
                        Sender = Agent,
                        HeuristicWeight = Weights[Timeouts]
                    };

                    switch (Type)
                    {
                        case PlanType.Radius:
                            aspr.GoalRegion = new SphereGoalRegion(Target, Radius);
                            break;
                        case PlanType.Into:
                            aspr.GoalRegion = new VoxelGoalRegion(Target);
                            break;
                        case PlanType.Adjacent:
                            aspr.GoalRegion = new AdjacentVoxelGoalRegion2D(Target);
                            break;
                        case PlanType.Edge:
                            aspr.GoalRegion = new EdgeGoalRegion();
                            break;
                    }

                    PlanSubscriber.SendRequest(aspr);
                    PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
                    WaitingOnResponse = true;
                    yield return Status.Running;


                    Timeouts++;
                }
                else
                {
                    if (Target != null && Creature.AI.DrawAIPlan)
                        Drawer3D.DrawLine(Creature.AI.Position, Target.WorldPosition, Color.Blue, 0.25f);
                    Status statusResult = Status.Running;

                    while (PlanSubscriber.Responses.Count > 0)
                    {
                        AStarPlanResponse response;
                        PlanSubscriber.Responses.TryDequeue(out response);

                        if (response.Success)
                        {
                            Path = response.Path;

                            if (Type == PlanType.Adjacent && Path.Count > 0)
                            {
                                Path.RemoveAt(Path.Count - 1);
                            }
                            WaitingOnResponse = false;

                            statusResult = Status.Success;
                        }
                        else if (Timeouts < MaxTimeouts)
                        {
                            yield return Status.Running;
                        }
                        else
                        {
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            statusResult = Status.Fail;

                        }
                    }
                    yield return statusResult;
                }
            }
        }
    }

}