// Service.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class Service <TRequest, TResponse> : IDisposable
    {
        [JsonIgnore]
        public ConcurrentQueue<KeyValuePair<uint, TRequest> > Requests { get; set; }

        private readonly Object subscriberlock = new object();

        protected List<Subscriber<TRequest, TResponse>> Subscribers { get; set; }

        [JsonIgnore]
        public AutoResetEvent NeedsServiceEvent = null;

        [JsonIgnore]
        public Thread ServiceThreadObject = null;

        public bool ExitThreads = false;

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
                if (ServiceThreadObject != null)
                {
                    ExitThreads = true;
                    ServiceThreadObject.Join();
                    ExitThreads = false;
                }
                ServiceThreadObject = new Thread(this.ServiceThread);
                ServiceThreadObject.Name = "ServiceThread";
                ServiceThreadObject.Start();
            }
            catch (System.AccessViolationException e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }



        public void Die()
        {
            ExitThreads = true;

            if(ServiceThreadObject != null)
                ServiceThreadObject.Join();
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

            while (!DwarfGame.ExitGame && !ExitThreads)
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

        public void Dispose()
        {
            if(NeedsServiceEvent != null)
                NeedsServiceEvent.Dispose();
        }
    }
}
