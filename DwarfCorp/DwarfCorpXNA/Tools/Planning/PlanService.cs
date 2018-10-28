// PlanService.cs
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
using System.Threading.Tasks;

namespace DwarfCorp
{
    /// <summary>
    /// A request to plan from point A to point B
    /// </summary>
    public class AstarPlanRequest
    {
        public PlanSubscriber Subscriber;
        public CreatureAI Sender;
        public VoxelHandle Start;
        public int MaxExpansions;
        public GoalRegion GoalRegion;
        public float HeuristicWeight = 1;
        public int ID;
        public static int MaxId = 0;
        public AstarPlanRequest()
        {
            ID = MaxId;
            MaxId++;
        }
    }

    /// <summary>
    /// The result of a plan request (has a path on success)
    /// </summary>
    public class AStarPlanResponse
    {
        public bool Success;
        public List<MoveAction> Path;
        public AstarPlanRequest Request;
        public AStarPlanner.PlanResultCode Result;
    }

    /// <summary>
    /// A service call which plans from pointA to pointB voxels.
    /// </summary>
    public class PlanService : Service<AstarPlanRequest, AStarPlanResponse>
    {
        public PlanService() : base("Path Planner", GameSettings.Default.NumPathingThreads)
        {

        }

        public override AStarPlanResponse HandleRequest(AstarPlanRequest req)
        {
            // If there are no subscribers that want this request, it must be old. So remove it.
            if (Subscribers.Find(s => s.ID == req.Subscriber.ID) == null)
            {
                return new AStarPlanResponse
                {
                    Path = null,
                    Success = false,
                    Request = req,
                    Result = AStarPlanner.PlanResultCode.Cancelled
                };
            }

            AStarPlanner.PlanResultCode result;
            List<MoveAction> path = AStarPlanner.FindPath(req.Sender.Movement, req.Start, req.GoalRegion, req.Sender.Manager.World.ChunkManager, 
                req.MaxExpansions, req.HeuristicWeight, Requests.Count, () => { return Subscribers.Find(s => s.ID == req.Subscriber.ID && s.CurrentRequestID == req.ID) != null; }, out result);

            AStarPlanResponse res = new AStarPlanResponse
            {
                Path = path,
                Success = (path != null),
                Request = req,
                Result = result
            };

            return res;
        }

    }

}
