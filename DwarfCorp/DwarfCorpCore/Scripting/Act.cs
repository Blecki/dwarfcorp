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
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     An act is an another Name for a "Behavior". Behaviors are linked together into an "behavior tree". Each behavior is
    ///     a coroutine
    ///     which can either be running, succeed, or fail. All AI scripting in the game is composed of Acts.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Act
    {
        /// <summary>
        ///     An Act will be repeatedly called until it returns something other than "Running"
        /// </summary>
        public enum Status
        {
            Running,
            Fail,
            Success
        }


        /// <summary>
        ///     Keeps track of the current status of the Act. Since Acts are enumerable,
        ///     this can be repeatedly queried for a new state.
        /// </summary>
        [JsonIgnore] public IEnumerator<Status> Enumerator;

        /// <summary>
        ///     Acts are given names for debugging purposes only.
        /// </summary>
        public string Name = "Act";


        public Act()
        {
            IsInitialized = false;
            Children = new List<Act>();
        }

        /// <summary>
        ///     An Act may have zero or more children. These are executed depending on the kind of Act.
        /// </summary>
        public List<Act> Children { get; set; }

        /// <summary>
        ///     True whenever the act has been initialized.
        /// </summary>
        public bool IsInitialized { get; set; }


        /// <summary>
        ///     Constructor for an act that takes in a function (or lambda). This can be used to
        ///     quickly construct acts from raw functions without having to define a new class.
        /// </summary>
        /// <param name="enumerator">A function returning "Status" using the yield keyword.</param>
        /// <returns>A new Act without children that merely calls the function until it returns something other than "Running"</returns>
        public static implicit operator Act(Func<IEnumerable<Status>> enumerator)
        {
            return enumerator.GetAct();
        }

        /// <summary>
        ///     Constructor for an act that takes in a function (or lambda). THis can be used to
        ///     quickly construct acts from raw functions without having to define a new class.
        /// </summary>
        /// <param name="enumerator">A function returning "bool" using the yield keyword.</param>
        /// <returns>
        ///     A new Act without children that merely calls the function once. "True" becomes Success, "False" becomes
        ///     Failure.
        /// </returns>
        public static implicit operator Act(Func<bool> enumerator)
        {
            return enumerator.GetAct();
        }

        /// <summary>
        ///     Constructor for an act that merely takes in a boolean conditional.
        /// </summary>
        /// <param name="condition">A condition to evaluate once when calling the act.</param>
        /// <returns>A new Act that merely checks a condition for truth. If True, returns "Success", else returns "Failure"</returns>
        public static implicit operator Act(bool condition)
        {
            return new Condition(() => condition);
        }

        /// <summary>
        ///     Convenience operator for combining two acts in Sequence.
        /// </summary>
        /// <param name="b1">This Act is performed first.</param>
        /// <param name="b2">This Act is performed only when b1 returns "Success"</param>
        /// <returns>A new Act which is the sequence of b1 and b2</returns>
        public static Act operator &(Act b1, Act b2)
        {
            return new Sequence(b1, b2);
        }


        /// <summary>
        ///     Convenience operator for combining two acts in Select.
        /// </summary>
        /// <param name="b1">This Act is performed first.</param>
        /// <param name="b2">This Act is performed second (regardless of what b1 returns)</param>
        /// <returns>A new Act which is the Selector between two acts b1 and b2.</returns>
        public static Act operator |(Act b1, Act b2)
        {
            return new Select(b1, b2);
        }

        /// <summary>
        ///     Conveniene operator for combining two Acts into a Parallel Act.
        /// </summary>
        /// <param name="b1">Child act performed concurrently with b2.</param>
        /// <param name="b2">Child act performed concurrently with b1.</param>
        /// <returns>A new Act that performs the two children in parallel.</returns>
        public static Act operator *(Act b1, Act b2)
        {
            return new Parallel(b1, b2);
        }

        /// <summary>
        ///     Convenience operator for inverting the output of an Act.
        /// </summary>
        /// <param name="b1">The Act to invert.</param>
        /// <returns>A new Act which inverts the output of b1. If b1 returns "Success", this returns "Failure" and vice versa.</returns>
        public static Act operator !(Act b1)
        {
            return new Not(b1);
        }


        /// <summary>
        ///     Performs one iteration of the Act. Initializes the Act if it hasn't been already.
        /// </summary>
        /// <returns>The Status of the Act (Success, Failure, or Running)</returns>
        public Status Tick()
        {
            if (Enumerator == null)
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


        /// <summary>
        ///     Sets up the Act and initializes the Enumerator.
        /// </summary>
        public virtual void Initialize()
        {
            Enumerator = Run().GetEnumerator();
            IsInitialized = true;
        }

        /// <summary>
        ///     Pure virtual function that returns an Act coroutine.
        /// </summary>
        /// <returns>
        ///     An enumerable list of Act.Status using "yield".
        ///     Iterating over this function causes the Act to get Ticked repeatedly.
        /// </returns>
        public virtual IEnumerable<Status> Run()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Creates a "Task" from the Act. A "Task" is merely an Act that has preconditions and success criteria.
        /// </summary>
        /// <returns>
        ///     A new Task whose preconditions are empty, and whose success criteria are that the Act
        ///     returns "Success".
        /// </returns>
        public virtual Task AsTask()
        {
            return new ActWrapperTask(this);
        }

        /// <summary>
        ///     Called whenever an Act is canceled, allowing the Act to clean up dangling state.
        ///     Acts may be canceled because of the player, or because the Actor has encountered an
        ///     extenuating circumstance that makes the Act impossible.
        /// </summary>
        public virtual void OnCanceled()
        {
            if (Children != null)
                foreach (Act child in Children)
                {
                    child.OnCanceled();
                }
        }
    }
}