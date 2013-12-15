using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlanAct : CreatureAct
    {
        public Timer PlannerTimer { get; set; }
        public int MaxExpansions { get; set; }

        public string PathOut { get; set; }

        public string TargetName { get; set; }

        public List<VoxelRef> Path { get { return GetPath(); } set {  SetPath(value);} }
        public VoxelRef Target { get { return GetTarget(); } set {  SetTarget(value);} }

        public PlanSubscriber PlanSubscriber { get; set; }

        private bool WaitingOnResponse { get; set; }

        public PlanAct()
        {

        }

        public PlanAct(CreatureAIComponent agent, string pathOut, string target) :
            base(agent)
        {
            Name = "Plan to " + target;
            PlannerTimer = new Timer(Agent.Stats.PlanRateLimit, false);
            MaxExpansions = Agent.Stats.MaxExpansions;
            PathOut = pathOut;
            TargetName = target;
            PlanSubscriber = new PlanSubscriber(PlayState.PlanService);
            WaitingOnResponse = false;
        }

        public VoxelRef GetTarget()
        {
            return Agent.Blackboard.GetData<VoxelRef>(TargetName);
        }

        public void SetTarget(VoxelRef target)
        {
            Agent.Blackboard.SetData(TargetName, target);
        }

        public List<VoxelRef> GetPath()
        {
            return Agent.Blackboard.GetData<List<VoxelRef>>(PathOut);
        }

        public void SetPath(List<VoxelRef> path)
        {
            Agent.Blackboard.SetData(PathOut, path);
        }

        public override IEnumerable<Status> Run()
        {
            Path = null;
            while(true)
            {
                if (Path != null)
                {
                    yield return Status.Success;
                    break;
                }

                PlannerTimer.Update(LastTime);

                ChunkManager chunks = PlayState.ChunkManager;
                if(PlannerTimer.HasTriggered)
                {
                    Voxel vox = chunks.ChunkData.GetFirstVisibleBlockUnder(Agent.Position, true);

                    if(vox == null)
                    {
                        yield return Status.Fail;
                        break;
                    }

                    List<VoxelRef> voxAbove = new List<VoxelRef>();
                    chunks.ChunkData.GetVoxelReferencesAtWorldLocation(null, vox.Position + new Vector3(0, 1, 0), voxAbove);


                    if(Target == null)
                    {
                        yield return Status.Fail;
                        break;
                    }

                    if(voxAbove.Count > 0)
                    {
                        Path = null;

                        PlanService.AstarPlanRequest aspr = new PlanService.AstarPlanRequest
                        {
                            Subscriber = PlanSubscriber,
                            Start = voxAbove[0],
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
                        yield return Status.Fail;
                        break;
                    }
                }
                else
                {
                    foreach(PlanService.AStarPlanResponse response in PlanSubscriber.AStarPlans.Where(response => response.Success))
                    {
                        Path = response.Path;
                        WaitingOnResponse = false;
                    }

                    yield return Status.Running;
                }
            }
        }
    }

}