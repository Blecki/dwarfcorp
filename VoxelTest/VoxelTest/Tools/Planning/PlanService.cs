using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A request to plan from point A to point B
    /// </summary>
    public class AstarPlanRequest
    {
        public PlanSubscriber Subscriber;
        public CreatureAIComponent Sender;
        public VoxelRef Goal;
        public VoxelRef Start;
        public int MaxExpansions;
    }

    /// <summary>
    /// The result of a plan request (has a path on success)
    /// </summary>
    public class AStarPlanResponse
    {
        public bool Success;
        public List<VoxelRef> Path;
    }

    /// <summary>
    /// A service call which plans from pointA to pointB voxels.
    /// </summary>
    public class PlanService : Service<AstarPlanRequest, AStarPlanResponse>
    {
        public override AStarPlanResponse HandleRequest(AstarPlanRequest req)
        {
            List<VoxelRef> path = AStarPlanner.FindPath(req.Start, req.Goal, PlayState.ChunkManager, req.MaxExpansions);

            AStarPlanResponse res = new AStarPlanResponse
            {
                Path = path,
                Success = (path != null)
            };

            return res;
        }
    }

}