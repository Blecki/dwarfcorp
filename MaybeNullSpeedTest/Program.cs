using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaybeNullSpeedTest
{
    class Program
    {
        class Foo
        {
            public void DoSomething() { }
        }

        static Foo bar = new Foo();
        static double rawTime, wrappedTime;
        static Random R = new Random();

        static Foo RawRef()
        {
            if (R.NextDouble() < 0.1f)
                return null;
            return bar;
        }

        static DwarfCorp.MaybeNull<Foo> WrappedRef()
        {
            if (R.NextDouble() < 0.1f)
                return null;
            return bar;
        }

        static void Main(string[] args)
        {
            int iters = 10000;
            int tests = 10000;

#if DEBUG
            Console.WriteLine("DEBUG: Running {0} tests of {1} iterations", tests, iters);
#else
            Console.WriteLine("RELEASE: Running {0} tests of {1} iterations", tests, iters);
#endif

            for (var i = 0; i < tests; ++i)
            {
                Console.SetCursorPosition(0, 1);
                Console.Write("{0} of {1}", i, tests);

                RunTest(iters);
            }

            var averageRaw = rawTime / (double)tests;
            var averageWrap = wrappedTime / (double)tests;

            Console.WriteLine();
            Console.WriteLine("Raw: {0}", averageRaw);
            Console.WriteLine("Wra: {0}", averageWrap);
            Console.WriteLine("Rat: {0}", averageWrap / averageRaw);

            Console.ReadLine();

        }

        static void RunTest(int iterations)
        {
            // Need high precision timers.
            var start = DateTime.Now;

            for (var i = 0; i < iterations; ++i)
            {
                var x = RawRef();
                if (x != null)
                    x.DoSomething();
            }

            var end = DateTime.Now;

            rawTime += (end - start).TotalMilliseconds;

            start = DateTime.Now;

            for (var i = 0; i < iterations; ++i)
                if (WrappedRef().HasValue(out var x))
                    x.DoSomething();

            end = DateTime.Now;

            wrappedTime += (end - start).TotalMilliseconds;

        }
    }
}
