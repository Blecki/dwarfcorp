using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
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
            for (long i = 0; i < 10; i++)
            {
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> BusyFail()
        {
            for (long i = 0; i < 10; i++)
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
            return new ForLoop(new Wrap(TestBehaviors.AlwaysTrue), 10);
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
            foreach (Act.Status status in seq.Run())
            {
                Console.Out.WriteLine("Seq status: " + status.ToString());
            }

            select.Initialize();
            foreach (Act.Status status in select.Run())
            {
                Console.Out.WriteLine("select status: " + status.ToString());
            }

            par.Initialize();
            foreach (Act.Status status in par.Run())
            {
                Console.Out.WriteLine("par status: " + status.ToString());
            }

            forLoop.Initialize();
            foreach (Act.Status status in forLoop.Run())
            {
                Console.Out.WriteLine("for status: " + status.ToString());
            }

            whileLoop.Initialize();
            foreach (Act.Status status in whileLoop.Run())
            {
                Console.Out.WriteLine("while status: " + status.ToString());
            }

            cond.Initialize();

            foreach (Act.Status status in cond.Run())
            {
                Console.Out.WriteLine("cond status: " + status.ToString());
            }

            DateTime first = DateTime.Now;
            wait.Initialize();
            Act.LastTime = new Microsoft.Xna.Framework.GameTime(DateTime.Now - first, DateTime.Now - DateTime.Now);
            DateTime last = DateTime.Now;
            foreach (Act.Status status in wait.Run())
            {
                Act.LastTime = new Microsoft.Xna.Framework.GameTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Wait status: " + status.ToString() + "," + wait.Time.CurrentTimeSeconds);
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            complex.Initialize();
            foreach (Act.Status status in complex.Run())
            {
                Act.LastTime = new Microsoft.Xna.Framework.GameTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Complex status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            overloader.Initialize();
            foreach (Act.Status status in overloader.Run())
            {
                Act.LastTime = new Microsoft.Xna.Framework.GameTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("Overloader status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }

            converter.Initialize();
            foreach (Act.Status status in converter.Run())
            {
                Act.LastTime = new Microsoft.Xna.Framework.GameTime(DateTime.Now - first, DateTime.Now - last);
                Console.Out.WriteLine("converter status: " + status.ToString());
                last = DateTime.Now;
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
