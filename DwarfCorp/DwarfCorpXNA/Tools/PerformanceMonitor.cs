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
        private static float[] FPSBuffer = new float[100];
        private static int k = 0;
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
                var FPS = 0;

                if (FPSWatch == null)
                    FPSWatch = Stopwatch.StartNew();
                else
                {
                    FPSWatch.Stop();
                    output.Lines.Add(String.Format("Frame time: {0:000.000}", FPSWatch.Elapsed.TotalMilliseconds));
                    FPS = (int)Math.Floor(1.0f / (float)FPSWatch.Elapsed.TotalSeconds);
                    FPSWatch = Stopwatch.StartNew();
                }

                foreach (var function in Functions)
                    output.Lines.Add(String.Format("{1:0000} {2:000} {0}\n", function.Value.Name, function.Value.FrameCalls, function.Value.FrameTicks / 1000));

                output.Invalidate();

                var fps = DwarfGame.GetConsoleTile("FPS");
                if (fps.Children[0] is Gui.Widgets.TextGrid)
                {
                    fps.RemoveChild(fps.Children[0]);
                    fps.AddChild(new Gui.Widgets.Graph()
                    {
                        AutoLayout = AutoLayout.DockFill,
                    });

                    fps.Layout();
                }

                var graph = fps.Children[0] as Gui.Widgets.Graph;
                graph.Values.Add((float)FPSWatch.Elapsed.TotalMilliseconds);
                while (graph.Values.Count > graph.GraphWidth)
                    graph.Values.RemoveAt(0);
                FPSBuffer[k % 100] = FPS;
                k++;

                graph.MinLabelString = String.Format("FPS: {0:000} (avg: {1})", FPS, (int)FPSBuffer.Average());

                graph.Invalidate();
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
