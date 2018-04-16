// Condition.cs
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
    /// Wraps a boolean function so that it returns success when the function
    /// evaluates to "true", and failure otherwise.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Condition : Act
    {
        private Func<bool> Function { get; set; }

        public Condition()
        {

        }

        public Condition(bool condition)
        {
            Name = "Condition";
            Function = () => condition;
        }

        public Condition(Func<bool> condition)
        {
            Name = "Condition: " + condition.Method.Name;
            Function = condition;
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            if(Function())
            {
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Domain : Act
    {
        private Func<bool> Function { get; set; }
        public Act Child { get; set; }
        public Domain()
        {

        }

        public Domain(bool condition, Act child)
        {
            Name = "Domain";
            Function = () => condition;
            Child = child;
        }

        public Domain(Func<bool> condition, Act child)
        {
            Name = "Domain: " + condition.Method.Name;
            Function = condition;
            Child = child;
        }

        public override void Initialize()
        {
            Child.Initialize();
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Child.OnCanceled();
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            while (true)
            {
                if (Function())
                {
                    var childStatus = Child.Tick();
                    LastTickedChild = Child;
                    if (childStatus == Act.Status.Running)
                    {
                        yield return Act.Status.Running;
                        continue;
                    }

                    yield return childStatus;
                    yield break;
                }
                else
                {
                    yield return Status.Fail;
                }
            }
        }
    }

}