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

        private static PerformanceFrame CurrentFrame;
        private static Dictionary<String, PerformanceFunction> Functions = new Dictionary<string, PerformanceFunction>();
        private static Gui.Root GuiRoot;
        private static Gui.Widgets.FreeText Widget;
        
        public static void BeginFrame()
        {
            if (GuiRoot == null)
            {
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                Widget = GuiRoot.RootItem.AddChild(new Gui.Widgets.FreeText
                {
                    AutoLayout = Gui.AutoLayout.DockFill,
                    TextColor = new Microsoft.Xna.Framework.Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                    TextSize = 2,
                }) as Gui.Widgets.FreeText;
            }

            if (Debugger.Switches.MonitorPerformance)
            {
                CurrentFrame = null;
                Functions.Clear();
                PushFrame("Root");
            }
        }

        public static void EndFrame()
        {
            if (Debugger.Switches.MonitorPerformance)
                PopFrame();
        }

        public static void Render()
        {
            if (Debugger.Switches.MonitorPerformance)
            {
                var builder = new StringBuilder();
                foreach (var function in Functions)
                    builder.AppendFormat("{0} : {1} {2}\n", function.Value.Name, function.Value.FrameCalls, function.Value.FrameTicks);
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
            if (Debugger.Switches.MonitorPerformance)
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
        }

        public static void PopFrame()
        {
            if (Debugger.Switches.MonitorPerformance)
            {

                if (CurrentFrame != null)
                {
                    CurrentFrame.Stopwatch.Stop();

                    CurrentFrame.Ticks = CurrentFrame.Stopwatch.ElapsedTicks;
                    CurrentFrame.Function.FrameTicks += CurrentFrame.Ticks;
                    CurrentFrame = CurrentFrame.ParentFrame;
                }
            }
        }
    }
}
