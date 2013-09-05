using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PlanAct : CreatureAct
    {
        public Timer PlannerTimer { get; set; }
        public int MaxExpansions { get; set; }

        public PlanAct(CreatureAIComponent agent, float rateLimit, int maxExpansions) :
            base(agent)
        {
            Name = "Plan";
            PlannerTimer = new Timer(rateLimit, false);
            MaxExpansions = maxExpansions;
        }

        public override IEnumerable<Status> Run()
        {
            bool pathFound = false;
            while (!pathFound)
            {
                if (Agent.CurrentPath != null)
                {
                    yield return Status.Success;
                    pathFound = true;
                    break;
                }

                PlannerTimer.Update(Act.LastTime);

                ChunkManager chunks = Agent.Creature.Master.Chunks;
                if (PlannerTimer.HasTriggered)
                {

                    Voxel vox = chunks.GetFirstVisibleBlockUnder(Agent.Position, true);
                    List<VoxelRef> voxAbove = new List<VoxelRef>();
                    chunks.GetVoxelReferencesAtWorldLocation(null, vox.Position + new Vector3(0, 1, 0), voxAbove);

                    if (Agent.TargetVoxel == null)
                    {
                        yield return Status.Fail;
                        pathFound = false;
                        break;
                    }

                    if (voxAbove.Count > 0)
                    {
                        Agent.CurrentPath = null;

                        PlanService.AstarPlanRequest aspr = new PlanService.AstarPlanRequest();
                        aspr.subscriber = Agent.PlanSubscriber;
                        aspr.start = voxAbove[0];
                        aspr.goal = Agent.TargetVoxel;
                        aspr.maxExpansions = MaxExpansions;
                        aspr.sender = Agent;

                        Agent.PlanSubscriber.SendRequest(aspr);
                        PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);

                        yield return Status.Running;
                    }
                    else
                    {
                        Agent.CurrentPath = null;
                        yield return Status.Fail;
                        break;
                    }
 
                }
                else
                {
                    yield return Status.Running;
                }
            }
        }
    }
}
