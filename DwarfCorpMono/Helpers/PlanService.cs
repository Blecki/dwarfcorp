using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
namespace DwarfCorp
{
    public class PlanService
    {
        public class AstarPlanRequest
        {
            public PlanSubscriber subscriber;
            public CreatureAIComponent sender;
            public VoxelRef goal;
            public VoxelRef start;
            public int maxExpansions;
        }

        public class AStarPlanResponse
        {
            public bool success;
            public List<VoxelRef> path;
        }

        public class GoapPlanRequest
        {
            public PlanSubscriber subscriber;
            public CreatureAIComponent sender;
            public Goal goal;
            public WorldState start;
        }

        public class GoapPlanResponse
        {
            public bool success;
            public List<Action> path;
        }

        public ConcurrentQueue<AstarPlanRequest> AstarRequests { get; set; }
        public ConcurrentQueue<GoapPlanRequest> GoapRequests { get; set; }
        public AutoResetEvent NeedsPlanEvent = new AutoResetEvent(true);
        public Thread PlanThreadObject = null;

        public PlanService()
        {
            AstarRequests = new ConcurrentQueue<AstarPlanRequest>();
            GoapRequests = new ConcurrentQueue<GoapPlanRequest>();
            Restart();
        }


        public void Restart()
        {
            try
            {
                PlanThreadObject = new Thread(this.PlanThread);
                PlanThreadObject.Start();
            }
            catch (System.AccessViolationException e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

        public void AddRequest(AstarPlanRequest request)
        {
            AstarRequests.Enqueue(request);
        }

        public void AddRequest(GoapPlanRequest request)
        {
            GoapRequests.Enqueue(request);
        }

        public void PlanThread()
        {
            EventWaitHandle[] waitHandles = new EventWaitHandle[] { NeedsPlanEvent, Program.shutdownEvent };

            bool shouldExit = false;
            while (!shouldExit && !GeometricPrimitive.ExitGame)
            {

                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                if (wh == Program.shutdownEvent)
                {
                    shouldExit = true;
                    break;
                }

                Update();
            }
        
        }

        public void Update()
        {
            while (AstarRequests.Count > 0)
            {
                AstarPlanRequest req = null;
                if (!AstarRequests.TryDequeue(out req)) { break; }


                List<VoxelRef> path = AStarPlanner.FindPath(req.start, req.goal, req.sender.Master.Chunks, req.maxExpansions);

                AStarPlanResponse res = new AStarPlanResponse();
                res.path = path;
                res.success = (path != null);

                req.subscriber.AStarPlans.Enqueue(res);
            }

            while (GoapRequests.Count > 0)
            {
                GoapPlanRequest req = null;
                if (!GoapRequests.TryDequeue(out req)) { break; }

                List<Action> path = req.sender.Goap.PlanToGoal(req.goal);

                GoapPlanResponse res = new GoapPlanResponse();
                res.path = path;
                res.success = path != null;

                req.subscriber.GoapPlans.Enqueue(res);
            }
        }
    }
}
