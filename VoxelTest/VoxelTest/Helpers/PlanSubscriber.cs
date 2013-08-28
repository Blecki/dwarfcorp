using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public class PlanSubscriber
    {
        public PlanService Service { get; set; }
        public ConcurrentQueue<PlanService.AStarPlanResponse> AStarPlans { get; set;}
        public ConcurrentQueue<PlanService.GoapPlanResponse> GoapPlans { get; set;}

        public PlanSubscriber(PlanService service)
        {
            Service = service;
            AStarPlans = new ConcurrentQueue<PlanService.AStarPlanResponse>();
            GoapPlans = new ConcurrentQueue<PlanService.GoapPlanResponse>();
        }

        public void SendRequest(PlanService.AstarPlanRequest request)
        {
            Service.AstarRequests.Enqueue(request);
            Service.NeedsPlanEvent.Set();
        }

        public void SendRequest(PlanService.GoapPlanRequest request)
        {
            Service.GoapRequests.Enqueue(request);
            Service.NeedsPlanEvent.Set();
        }


    }
}
