// Act.cs
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// An act is an another Name for a "Behavior". Behaviors are linked together into an "behavior tree". Each behavior is a coroutine
    /// which can either be running, succeed, or fail. 
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Act
    {
        public enum Status
        {
            Running,
            Fail,
            Success
        }


        public List<Act> Children { get; set; }

        public string Name = "Act";
        public bool IsInitialized { get; set; }

        [JsonIgnore]
        public IEnumerator<Status> Enumerator;

        [JsonIgnore]
        public Act LastTickedChild { get; set; }

        public bool IsCanceled { get; set; }

        public Act()
        {
            IsInitialized = false;
            Children = new List<Act>();
        }


        public static implicit operator Act(Func<IEnumerable<Status>> enumerator)
        {
            return enumerator.GetAct();
        }

        public static implicit operator Act(Func<bool> enumerator)
        {
            return enumerator.GetAct();
        }

        public static implicit operator Act(bool condition)
        {
            return new Condition(() => condition);
        }

        public static implicit operator Act(Act.Status status)
        {
            return new Always(status);
        }

        public static Act operator &(Act b1, Act b2)
        {
            return new Sequence(b1, b2);
        }

        public static Act operator |(Act b1, Act b2)
        {
            return new Select(b1, b2);
        }

        public static Act operator *(Act b1, Act b2)
        {
            return new Parallel(b1, b2);
        }

        public static Act operator !(Act b1)
        {
            return new Not(b1);
        }


        public Status Tick()
        {
            if(Enumerator == null)
            {
                Initialize();
            }
            if (Enumerator != null)
            {
                Enumerator.MoveNext();
            }
            else
            {
                return Status.Fail;
            }
            return Enumerator.Current;
        }


        public virtual void Initialize()
        {
            Enumerator = Run().GetEnumerator();
            IsInitialized = true;
        }

        public virtual IEnumerable<Status> Run()
        {
            throw new NotImplementedException();
        }

        public virtual Task AsTask()
        {
            return new ActWrapperTask(this);
        }

        public virtual void OnCanceled()
        {
            IsCanceled = true;
            if(Children != null)
                foreach (Act child in Children)
                {
                    child.OnCanceled();
                }
        }
    }

    public class Always : Act
    {
        public Act.Status AlwaysStatus = Act.Status.Success;
        public Always(Act.Status status)
        {
            AlwaysStatus = status;
        }

        public override IEnumerable<Status> Run()
        {
            yield return AlwaysStatus;
            yield break;
        }
    }

}