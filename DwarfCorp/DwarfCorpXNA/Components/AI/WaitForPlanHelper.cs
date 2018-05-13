// CreatureAI.cs
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
using System.Linq;
using System.Runtime.Serialization;
//using System.Windows.Forms;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class WaitForPlanHelper
    {
        private PlanSubscriber Subscriber;
        private Timer Timeout;
        private AstarPlanRequest LastRequest;
        public WaitForPlanHelper()
        {


        }

        public WaitForPlanHelper(float timeout, PlanService planService)
        {
            Subscriber = new PlanSubscriber(planService);
            Timeout = new Timer(timeout, true, Timer.TimerMode.Real);
        }

        public AStarPlanResponse WaitForResponse(AstarPlanRequest request)
        {
            // If we already have a request, determine if it has been satisfied.
            if (LastRequest != null)
            {
                // first, if the timer has triggered, return an unsuccessful plan.
                Timeout.Update(DwarfTime.LastTime);
                if (Timeout.HasTriggered)
                {
                    LastRequest = null;
                    return new AStarPlanResponse() { Success = false };
                }

                // Otherwise, see if there are any responses yet.
                while (Subscriber.Responses.Count > 0)
                {
                    AStarPlanResponse response;
                    bool success = Subscriber.Responses.TryDequeue(out response);

                    // If so, determine if the response is what we requested.
                    if (success)
                    {
                        // If not, maybe try another response
                        if (response.Request != LastRequest)
                        {
                            continue;
                        }

                        // Otherwise, we found our guy. return it.
                        LastRequest = null;

                        // Clear the response queue.
                        while (Subscriber.Responses.Count > 0)
                        {
                            AStarPlanResponse dummy;
                            Subscriber.Responses.TryDequeue(out dummy);
                        }
                        return response;
                    }
                }
                // No responses? Return null.
                return null;
            }

            Timeout.Reset();
            // Otherwise, this is a new request. Push it and return null.
            LastRequest = request;
            Subscriber.SendRequest(request);
            return null;
        }
    }
}