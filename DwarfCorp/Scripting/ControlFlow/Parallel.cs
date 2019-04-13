// Parallel.cs
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
    /// Has a list of children which get ticked in sequence as if they are being run
    /// in parallel. Returns success when all of the children return success. Otherwise,
    /// continues running until a child returns failure.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Parallel : Act
    {
        public bool ReturnOnAllSucces { get; set; }
        public Parallel(params Act[] children) :
            this(children.AsEnumerable())
        {
            ReturnOnAllSucces = true;
        }

        public Parallel(IEnumerable<Act> children)
        {
            Name = "Parallel";
            Children = new List<Act>();
            Children.AddRange(children);
            ReturnOnAllSucces = true;
        }

        public override void Initialize()
        {
            foreach(Act child in Children)
            {
                child.Initialize();
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            bool allSuccess = false;

            while(!allSuccess)
            {
                bool runEncountered = false;
                foreach(Act child in Children)
                {
                    Status childStatus = child.Tick();
                    LastTickedChild = child;
                    if(childStatus == Status.Fail)
                    {
                        yield return Status.Fail;
                        yield break;
                    }
                    else if (childStatus != Status.Success)
                    {
                        runEncountered = true;
                    }
                    else
                    {

                        if (!ReturnOnAllSucces)
                        {
                            yield return Status.Success;
                            yield break;
                        }
                    }
                }

                if(!runEncountered)
                {
                    allSuccess = true;
                }
                else
                {
                    yield return Status.Running;
                }
            }

            yield return Status.Success;
        }
    }

}