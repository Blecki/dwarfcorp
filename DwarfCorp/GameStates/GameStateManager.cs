using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates
{
    public static class GameStateManager
    {
        private static List<GameState> StateStack = new List<GameState>();
        public static bool StateStackIsEmpty => StateStack.Count == 0;
        private static GameState CurrentState;
        private static GameState NextState;
        public static bool DrawScreensaver => CurrentState != null && CurrentState.EnableScreensaver;

        private static object _mutex = new object();

        public static void PopState(bool enterNext = true)
        {
            lock (_mutex)
            {
                if (StateStack.Count > 0)
                {
                    DwarfGame.LogSentryBreadcrumb("GameState", String.Format("Leaving state {0}", StateStack[0].GetType().FullName));
                    StateStack[0].OnPopped();
                    StateStack.RemoveAt(0);
                }

                if (StateStack.Count > 0 && enterNext)
                {
                    var state = StateStack.ElementAt(0);
                    NextState = state;
                    NextState.OnEnter(); // Split into OnEnter and OnExposed
                }
            }
        }

        public static void ClearState()
        {
            lock (_mutex)
            {
                while (StateStack.Count > 0)
                    PopState(false);
            }
        }

        public static void PushState(GameState state)
        {
            lock (_mutex)
            {
                NextState = state;
                NextState.OnEnter();
                StateStack.Insert(0, NextState);
            }
        }

        public static void Update(DwarfTime time)
        {
            lock (_mutex)
            {
                if (NextState != null && NextState.IsInitialized)
                {
                    if (CurrentState != null)
                        CurrentState.OnCovered();

                    CurrentState = NextState;
                    NextState = null;
                }

                if (CurrentState != null && CurrentState.IsInitialized)
                    CurrentState.Update(time);

                if (DwarfGame.IsConsoleVisible)
                {
                    PerformanceMonitor.SetMetric("MEMORY", BytesToString(System.GC.GetTotalMemory(false)));
                    PerformanceMonitor.SetMetric("COMPS BUILT", DwarfSprites.LayerStack.CompositesRebuilt.ToString());
                    DwarfSprites.LayerStack.CompositesRebuilt = 0;

                    var statsDisplay = DwarfGame.GetConsoleTile("STATS");

                    statsDisplay.Lines.Clear();
                    statsDisplay.Lines.Add("** STATISTICS **");
                    foreach (var metric in PerformanceMonitor.EnumerateMetrics())
                        statsDisplay.Lines.Add(String.Format("{0} {1}", metric.Value.ToString(), metric.Key));
                    statsDisplay.Invalidate();
                }
            }
        }

        public static void Render(DwarfTime time)
        {
            lock (_mutex)
            {
                for (int i = StateStack.Count - 1; i >= 0; i--)
                {
                    var state = StateStack[i];
                    if (state != null && (state.RenderUnderneath || i == 0 || Object.ReferenceEquals(state, CurrentState) || Object.ReferenceEquals(state, NextState)))
                    {
                        if (state.IsInitialized)
                            state.Render(time);
                        else if (!state.IsInitialized)
                            state.RenderUnitialized(time);
                    }
                }
            }
        }

        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return String.Format("{0:000} {1}", Math.Sign(byteCount) * num, suf[place]);
        }
    }
}