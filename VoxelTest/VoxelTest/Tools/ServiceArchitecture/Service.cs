using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.Tools.ServiceArchitecture;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// The Service architecutre divides a task into servers (Service) and subscribers (Subscriber).
    /// The server looks at a list of requests, processes them, and then broadcasts the results
    /// to subscribers. This all happens in parallel.
    /// </summary>
    /// <typeparam Name="TRequest">The type of the request.</typeparam>
    /// <typeparam Name="TResponse">The type of the response</typeparam>
    [JsonObject(IsReference = true)]
    public class Service <TRequest, TResponse>
    {
        [JsonIgnore]
        public ConcurrentQueue<KeyValuePair<uint, TRequest> > Requests { get; set; }

        private readonly Object subscriberlock = new object();

        protected List<Subscriber<TRequest, TResponse>> Subscribers { get; set; }

        [JsonIgnore]
        public AutoResetEvent NeedsServiceEvent = null;

        [JsonIgnore]
        public Thread ServiceThreadObject = null;

        public Service()
        {
            Subscribers = new List<Subscriber<TRequest, TResponse>>();

            NeedsServiceEvent = new AutoResetEvent(false);
            
            if (Requests == null)
            {
                Requests = new ConcurrentQueue<KeyValuePair<uint, TRequest>>();
            }


            Restart();
        }

        public void AddSubscriber(Subscriber<TRequest, TResponse> subscriber)
        {
            lock(subscriberlock)
            {
                Subscribers.Add(subscriber);
            }
        }

        public void RemoveSubscriber(Subscriber<TRequest, TResponse> subscriber)
        {
            lock(subscriberlock)
            {
                Subscribers.Remove(subscriber);
            }
        }


        public void Restart()
        {
            try
            {
                ServiceThreadObject = new Thread(this.ServiceThread);
                ServiceThreadObject.Start();
            }
            catch (System.AccessViolationException e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        public void AddRequest(TRequest request, uint subscriberID)
        {
            Requests.Enqueue(new KeyValuePair<uint, TRequest>(subscriberID, request));
        }

        public void ServiceThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsServiceEvent,
                Program.ShutdownEvent
            };

            while (!DwarfGame.ExitGame)
            {
                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                if (wh == Program.ShutdownEvent)
                {
                    break;
                }

                Update();
            }
        }

        public virtual TResponse HandleRequest(TRequest request)
        {
            return default(TResponse);
        }

        public void BroadcastResponse(TResponse response, uint id)
        {
            lock(subscriberlock)
            {
                foreach(Subscriber<TRequest, TResponse> subscriber in Subscribers.Where(subscriber => subscriber.ID == id))
                {
                    subscriber.Responses.Enqueue(response);
                }
            }
        }

        public void Update()
        {
            while (Requests.Count > 0)
            {
                KeyValuePair<uint, TRequest> req;
                if (!Requests.TryDequeue(out req))
                {
                    break;
                }

                TResponse response = HandleRequest(req.Value);
                BroadcastResponse(response, req.Key);
            }
        }

    }
}
