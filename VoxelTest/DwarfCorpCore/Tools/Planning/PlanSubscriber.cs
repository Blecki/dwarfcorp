using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using DwarfCorp.Tools.ServiceArchitecture;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A service subscriber which is used to find paths from pointA to pointB
    /// </summary>
    public class PlanSubscriber : Subscriber<AstarPlanRequest, AStarPlanResponse>
    {
        public PlanSubscriber(PlanService service)
        {
            Service = service;
            Service.AddSubscriber(this);
        }

    }

}