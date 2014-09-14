﻿using System;
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

        public List<Voxel> Path { get { return GetPath(); } set {  SetPath(value);} }
        public Voxel Target { get { return GetTarget(); } set {  SetTarget(value);} }

        public PlanSubscriber PlanSubscriber { get; set; }

        public int MaxTimeouts { get; set; }

        public int Timeouts { get; set; }

        private bool WaitingOnResponse { get; set; }

        public enum PlanType
        {
            Adjacent,
            Into
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
            PlanSubscriber = new PlanSubscriber(PlayState.PlanService);
            WaitingOnResponse = false;
            MaxTimeouts = 4;
            Timeouts = 0;
        }

        public Voxel GetTarget()
        {
            return Agent.Blackboard.GetData<Voxel>(TargetName);
        }

        public void SetTarget(Voxel target)
        {
            Agent.Blackboard.SetData(TargetName, target);
        }

        public List<Voxel> GetPath()
        {
            return Agent.Blackboard.GetData<List<Voxel>>(PathOut);
        }

        public void SetPath(List<Voxel> path)
        {
            Agent.Blackboard.SetData(PathOut, path);
        }

        public override IEnumerable<Status> Run()
        {
            Path = null;
            Timeouts = 0;
            PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
            
            while(true)
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

                PlannerTimer.Update(LastTime);

                ChunkManager chunks = PlayState.ChunkManager;
                if(PlannerTimer.HasTriggered || Timeouts == 0)
                {
                    Voxel vox = chunks.ChunkData.GetFirstVoxelUnder(Agent.Position);

                    if(vox == null)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    Voxel voxAbove = chunks.ChunkData.GetVoxelerenceAtWorldLocation(null, vox.Position + new Vector3(0, 1, 0));


                    if(Target == null)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    if(voxAbove != null)
                    {
                        Path = null;

                   

                        AstarPlanRequest aspr = new AstarPlanRequest
                        {
                            Subscriber = PlanSubscriber,
                            Start = voxAbove,
                            Goal = Target,
                            MaxExpansions = MaxExpansions,
                            Sender = Agent
                        };

                        PlanSubscriber.SendRequest(aspr);
                        PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
                        WaitingOnResponse = true;
                        yield return Status.Running;

                    }
                    else
                    {
                        Path = null;
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    Timeouts++;
                }
                else
                {
                    Status statusResult = Status.Running;

                    while(PlanSubscriber.Responses.Count > 0)
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