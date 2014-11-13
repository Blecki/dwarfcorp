using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp.Tools.ServiceArchitecture
{
    [JsonObject(IsReference = true)]
    public class Subscriber<TRequest, TResponse>
    {
        public Service<TRequest, TResponse> Service  { get; set; }

        [JsonIgnore] 
        public readonly uint ID;

        private static uint maxID = 0;

        [JsonIgnore]
        public ConcurrentQueue<TResponse> Responses { get; set; }

        public Subscriber()
        {
            Responses = new ConcurrentQueue<TResponse>();
            ID = maxID;
            maxID++;
        }

        public Subscriber(Service<TRequest, TResponse> service)
        {
            Service = service;
            Responses = new ConcurrentQueue<TResponse>();
        }

        public void SendRequest(TRequest request)
        {
            Service.AddRequest(request, ID);
            Service.NeedsServiceEvent.Set();
        }

        public void Subscribe()
        {
            Service.AddSubscriber(this);
        }

        public void Unsubscribe()
        {
            Service.RemoveSubscriber(this);
        }
    }
}
