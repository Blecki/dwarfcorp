// TestBehaviors.cs
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
    /// Functions to test behaviors (or acts)
    /// </summary>
    public class TestBehaviors
    {
        public static IEnumerable<Act.Status> AlwaysTrue()
        {
            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> AlwaysFalse()
        {
            yield return Act.Status.Fail;
        }

        public static IEnumerable<Act.Status> BusyLoop()
        {
            for(long i = 0; i < 10; i++)
            {
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> BusyFail()
        {
            for(long i = 0; i < 10; i++)
            {
                yield return Act.Status.Running;
            }

            yield return Act.Status.Fail;
        }

        public static Act SimpleSequence()
        {
            return new Sequence(
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.AlwaysTrue)
                );
        }

        public static Act SimpleSelect()
        {
            return new Select(
                new Wrap(TestBehaviors.AlwaysFalse),
                new Wrap(TestBehaviors.AlwaysFalse),
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.AlwaysTrue)
                );
        }

        public static Act SimpleParallel()
        {
            return new Parallel
                (
                new Wrap(TestBehaviors.AlwaysTrue),
                new Wrap(TestBehaviors.BusyLoop)
                );
        }

        public static Act SimpleFor()
        {
            return new Repeat(new Wrap(TestBehaviors.AlwaysTrue), 10, false);
        }

        public static Act SimpleWhile()
        {
            return new WhileLoop(new Wrap(TestBehaviors.AlwaysTrue), new Wrap(TestBehaviors.BusyFail));
        }

        public static Act ComplexBehavior()
        {
            return new Sequence(
                new Not(
                    new Not(
                        new Select
                            (
                            SimpleWhile(),
                            SimpleFor(),
                            SimpleParallel()
                            )
                        )),
                new Wait(1.0f),
                new Condition(() => { return 5 > 4; })
                );
        }

        public static Act Convertor()
        {
            return SimpleSequence()
                   & (5 > 4)
                   & new Func<bool>(TestBehaviors.SimpleCondition)
                   & new Func<IEnumerable<Act.Status>>(AlwaysTrue);
        }


        public static bool SimpleCondition()
        {
            return 5 + 7 < 9 + 9;
        }

        public static Act OverloaderTest()
        {
            return ((SimpleFor() & SimpleSequence()) | (SimpleWhile() | SimpleSequence())) * new Wait(5) & new Func<bool>(SimpleCondition).GetAct();
        }

        public static void RunTests()
        {
            Act seq = SimpleSequence();
            Act select = SimpleSelect();
            Act par = SimpleParallel();
            Act forLoop = SimpleFor();
            Act whileLoop = SimpleWhile();
            Wait wait = new Wait(5.0f);
            Condition cond = new Condition(() => { return 1 > 2 || 5 == 3 || 1 + 1 == 2; });

            Act complex = ComplexBehavior();
            Act overloader = OverloaderTest();
            Act converter = Convertor();

            seq.Initialize();
            foreach(Act.Status status in seq.Run())
            {
                Console.Out.WriteLine("Seq status: " + status.ToString());
            }

            select.Initialize();
            foreach(Act.Status status in select.Run())
            {
                Console.Out.WriteLine("select status: " + status.ToString());
            }

            par.Initialize();
            foreach(Act.Status status in par.Run())
            {
                Console.Out.WriteLine("par status: " + status.ToString());
            }

            forLoop.Initialize();
            foreach(Act.Status status in forLoop.Run())
            {
                Console.Out.WriteLine("for status: " + status.ToString());
            }

            whileLoop.Initialize();
            foreach(Act.Status status in whileLoop.Run())
            {
                Console.Out.WriteLine("while status: " + status.ToString());
            }

            cond.Initialize();

            foreach(Act.Status status in cond.Run())
            {
                Console.Out.WriteLine("cond status: " + status.ToString());
            }

            DateTime first = DateTime.Now;
            wait.Initialize();
            DwarfTime.LastTime = new DwarfTime(DateTime.Now - first, DateTime.Now - DateTime.Now);
            DateTime last = DateTime.Now;
            foreach(Act.Status status in wait.Run())
            {
                DwarfTime.LastTime = new DwarfTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Wait status: " + status.ToString() + "," + wait.Time.CurrentTimeSeconds);
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            complex.Initialize();
            foreach(Act.Status status in complex.Run())
            {
                DwarfTime.LastTime = new DwarfTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Complex status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            overloader.Initialize();
            foreach(Act.Status status in overloader.Run())
            {
                DwarfTime.LastTime = new DwarfTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Overloader status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            converter.Initialize();
            foreach(Act.Status status in converter.Run())
            {
                DwarfTime.LastTime = new DwarfTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("converter status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }
        }
    }

}