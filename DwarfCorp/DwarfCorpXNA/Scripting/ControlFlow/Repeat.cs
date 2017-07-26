// ForLoop.cs
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
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Wraps an act in a loop which occurs over a fixed number of iterations.
    /// Restarts its child on each iteration.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Repeat : Act
    {
        public Act Child { get; set; }
        public int Iters { get; set; }
        public bool BreakOnSuccess { get; set; }

        public Repeat(Act child, int iters, bool breakOnSuccess)
        {
            Name = "For(" + iters + ")";
            Iters = iters;
            Child = child;
            BreakOnSuccess = breakOnSuccess;
        }

        public override void Initialize()
        {
            Children.Clear();
            Children.Add(Child);
            Child.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            bool failEncountered = false;
            for(int i = 0; i < Iters; i++)
            {
                Child.Initialize();

                while(true)
                {
                    Status childStatus = Child.Tick();
                    LastTickedChild = Child;
                    if(childStatus == Status.Fail)
                    {
                        failEncountered = true;
                        yield return Status.Running;
                        break;
                    }
                    else if(childStatus == Status.Success)
                    {
                        failEncountered = false;
                        yield return Status.Running;
                        break;
                    }
                    else
                    {
                        yield return Status.Running;
                    }
                }

                if(!failEncountered && BreakOnSuccess)
                {
                    break;
                }
            }

            if(failEncountered)
            {
                yield return Status.Fail;
            }
            else
            {
                yield return Status.Success;
            }
        }
    }

}