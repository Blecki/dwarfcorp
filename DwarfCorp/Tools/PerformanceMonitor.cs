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

        private static Dictionary<String, Object> Metrics = new Dictionary<String, Object>();

        public static void SetMetric(String Name, Object Value)
        {
            lock (Metrics)
                Metrics.Upsert(Name, Value);
        }

        public static IEnumerable<KeyValuePair<String, Object>> EnumerateMetrics()
        {
            var result = new List<KeyValuePair<String, Object>>();
            lock (Metrics)
            {
                foreach (var metric in Metrics)
                    result.Add(metric);
            }
            return result;
        }

        private static float[] FPSBuffer = new float[100];
        private static int k = 0;
        [ThreadStatic]
        private static PerformanceFrame CurrentFrame;

        [ThreadStatic]
        private static Dictionary<String, PerformanceFunction> Functions = new Dictionary<string, PerformanceFunction>();

        private static Stopwatch FPSWatch = null;
        private static Stopwatch FPSFaultTimer = null;
        private static bool SentPerfReport = false;

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
            var FPS = 0;
            double elapsedMilliseconds = 0.0;

            if (FPSWatch == null)
                FPSWatch = Stopwatch.StartNew();
            else
            {
                FPSWatch.Stop();
                FPS = (int)Math.Floor(1.0f / (float)FPSWatch.Elapsed.TotalSeconds);
                elapsedMilliseconds = FPSWatch.Elapsed.TotalMilliseconds;
                FPSWatch = Stopwatch.StartNew();
            }

            FPSBuffer[k % 100] = FPS;
            k++;
            var avgFPS = (int)FPSBuffer.Average();
#if XNA_BUILD
            if (!SentPerfReport && GameSettings.Current.AllowReporting && avgFPS < 20)
            {
                if (FPSFaultTimer != null && FPSFaultTimer.Elapsed.TotalSeconds > 5)
                {
                    /*
                    var settings = FileUtils.SerializeBasicJSON<GameSettings.Settings>(GameSettings.Default);
                    var adapter = GameStates.GameState.Game.GraphicsDevice.Adapter;
                    var deviceDetails = String.Format("Num Cores: {4}\nDevice:\nName: {0}\n ID: {1}\n Description: {2}\n Vendor: {3}", adapter.DeviceName, adapter.DeviceId, adapter.Description, adapter.VendorId, Environment.ProcessorCount);
                    var memory = GameStates.PlayState.BytesToString(System.GC.GetTotalMemory(false));
                    (GameStates.GameState.Game as DwarfGame).TriggerRavenEvent("Low performance detected", String.Format("Average FPS: {0}\nSettings:\n{1}\n{2}\nRAM: {3} {4}", avgFPS, settings, deviceDetails, memory, GameStates.PlayState.BytesToString(Environment.WorkingSet)));
                    SentPerfReport = true;
                    */
                }
                else if (FPSFaultTimer == null)
                {
                    FPSFaultTimer = Stopwatch.StartNew();
                }
            }
            else if (!SentPerfReport && GameSettings.Current.AllowReporting)
            {

                FPSFaultTimer = null;
            }
#endif

            if (DwarfGame.IsConsoleVisible)
            {
                PopFrame();

                var output = DwarfGame.GetConsoleTile("PERFORMANCE");
                output.Lines.Clear();
                output.Lines.Add(String.Format("Frame time: {0:000.000}", elapsedMilliseconds));

                foreach (var function in Functions.OrderBy(f => f.Value.FrameTicks).Reverse())
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

                graph.MinLabelString = String.Format("FPS: {0:000} (avg: {1})", FPS, avgFPS);

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
