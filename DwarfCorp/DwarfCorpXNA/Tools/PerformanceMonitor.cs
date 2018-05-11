using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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

        private static Gui.Root GuiRoot;
        private static Gui.Widgets.FreeText Widget;
        private static Stopwatch FPSWatch = null;
        
        public static void BeginFrame()
        {
            if (Debugger.Switches.MonitorPerformance)
            {
                CurrentFrame = null;
                Functions.Clear();
                __pushFrame("Root");
            }
        }        

        public static void Render()
        {
            if (Debugger.Switches.MonitorPerformance)
            {
                PopFrame();

                if (GuiRoot == null)
                {
                    GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                    Widget = GuiRoot.RootItem.AddChild(new Gui.Widgets.FreeText
                    {
                        AutoLayout = Gui.AutoLayout.DockFill,
                        TextColor = new Microsoft.Xna.Framework.Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                    }) as Gui.Widgets.FreeText;
                }

                var builder = new StringBuilder();

                if (FPSWatch == null)
                {
                    FPSWatch = Stopwatch.StartNew();
                    builder.AppendLine();
                }
                else
                {
                    FPSWatch.Stop();
                    builder.AppendFormat("Frame time: {0:000.000} FPS: {1:000}\n", FPSWatch.Elapsed.TotalMilliseconds, 1.0f / FPSWatch.Elapsed.TotalSeconds);
                    FPSWatch = Stopwatch.StartNew();
                }

                foreach (var function in Functions)
                    builder.AppendFormat("{1:0000} {2:000} {3:00000000} {0}\n", function.Value.Name, function.Value.FrameCalls, function.Value.FrameTicks / 1000, function.Value.FrameTicks);
                if (GuiRoot != null)
                {
                    Widget.Text = builder.ToString();
                    Widget.Invalidate();
                    GuiRoot.Draw();
                }
            }
        }

        public static void PushFrame(String Name)
        {
            if (Debugger.Switches.MonitorPerformance && CurrentFrame != null)
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
            if (Debugger.Switches.MonitorPerformance && CurrentFrame != null)
            {
                    CurrentFrame.Stopwatch.Stop();

                    CurrentFrame.Ticks = CurrentFrame.Stopwatch.ElapsedTicks;
                    CurrentFrame.Function.FrameTicks += CurrentFrame.Ticks;
                    CurrentFrame = CurrentFrame.ParentFrame;
            }
        }
    }
}
