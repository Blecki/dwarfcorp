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

        private static Gui.Widgets.DwarfConsole GetOutputWidget()
        {
            var display = DwarfGame.ConsolePanel.EnumerateChildren().Where(c =>
            {
                if (c.Tag is String tag) return tag == "PERFORMANCE";
                return false;
            }).FirstOrDefault() as Gui.Widgets.DwarfConsole;

            if (display == null)
            {
                display = DwarfGame.ConsolePanel.AddChild(new Gui.Widgets.DwarfConsole
                {
                    AutoLayout = AutoLayout.DockLeft,
                    Background = new TileReference("basic", 1),
                    BackgroundColor = new Vector4(1.0f, 1.0f, 1.0f, 0.25f),
                    MinimumSize = new Point(200, 0),
                    Tag = "PERFORMANCE"
                }) as Gui.Widgets.DwarfConsole;

                DwarfGame.RebuildConsole();
            }

            return display;
        }
        
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

                var output = GetOutputWidget();
                output.Lines.Clear();

                if (FPSWatch == null)
                    FPSWatch = Stopwatch.StartNew();
                else
                {
                    FPSWatch.Stop();
                    output.Lines.Add(String.Format("Frame time: {0:000.000} FPS: {1:000}\n", FPSWatch.Elapsed.TotalMilliseconds, 1.0f / FPSWatch.Elapsed.TotalSeconds));
                    FPSWatch = Stopwatch.StartNew();
                }

                foreach (var function in Functions)
                    output.Lines.Add(String.Format("{1:0000} {2:000} {3:00000000} {0}\n", function.Value.Name, function.Value.FrameCalls, function.Value.FrameTicks / 1000, function.Value.FrameTicks));

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
