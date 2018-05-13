using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class PerformanceMonitor
    {
        private class PerformanceFunction
        {
            public String Name;
            public int FrameCalls;
            public long FrameTicks;
        }

        private class PerformanceFrame
        {
            public PerformanceFrame ParentFrame;
            public Stopwatch Stopwatch;
            public long Ticks;
            public PerformanceFunction Function;
        }

        [ThreadStatic]
        private static PerformanceFrame CurrentFrame;

        [ThreadStatic]
        private static Dictionary<String, PerformanceFunction> Functions = new Dictionary<string, PerformanceFunction>();

        private static Stopwatch FPSWatch = null;

        public static void BeginFrame()
        {
            if (DwarfGame.IsConsoleVisible)
            {
                CurrentFrame = null;
                Functions.Clear();
                __pushFrame("Root");
            }
        }        

        public static void Render()
        {
            if (DwarfGame.IsConsoleVisible)
            {
                PopFrame();

                var output = DwarfGame.GetConsoleTile("PERFORMANCE");
                output.Lines.Clear();

                if (FPSWatch == null)
                    FPSWatch = Stopwatch.StartNew();
                else
                {
                    FPSWatch.Stop();
                    output.Lines.Add(String.Format("Frame time: {0:000.000}", FPSWatch.Elapsed.TotalMilliseconds));
                    FPSWatch = Stopwatch.StartNew();
                }

                foreach (var function in Functions)
                    output.Lines.Add(String.Format("{1:0000} {2:000} {0}\n", function.Value.Name, function.Value.FrameCalls, function.Value.FrameTicks / 1000));

                output.Invalidate();
            }
        }

        public static void PushFrame(String Name)
        {
            if (DwarfGame.IsConsoleVisible && CurrentFrame != null)
                __pushFrame(Name);
        }

        private static void __pushFrame(String Name)
        {
            PerformanceFunction Function;
            if (!Functions.TryGetValue(Name, out Function))
            {
                Function = new PerformanceFunction
                {
                    Name = Name
                };

                Functions.Add(Name, Function);
            }

            Function.FrameCalls += 1;

            CurrentFrame = new PerformanceFrame
            {
                ParentFrame = CurrentFrame,
                Stopwatch = Stopwatch.StartNew(),
                Function = Function
            };
        }

        public static void PopFrame()
        {
            if (DwarfGame.IsConsoleVisible && CurrentFrame != null)
            {
                    CurrentFrame.Stopwatch.Stop();

                    CurrentFrame.Ticks = CurrentFrame.Stopwatch.ElapsedTicks;
                    CurrentFrame.Function.FrameTicks += CurrentFrame.Ticks;
                    CurrentFrame = CurrentFrame.ParentFrame;
            }
        }
    }
}
