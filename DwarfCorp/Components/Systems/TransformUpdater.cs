using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Threading;

namespace DwarfCorp
{
    public class ComponentTransformModule : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new ComponentTransformModule();
        }

        public override ModuleManager.UpdateTypes UpdatesWanted => ModuleManager.UpdateTypes.Update | ModuleManager.UpdateTypes.Shutdown;

        private class WorkerThread
        {
            public Thread Thread;
            public EventWaitHandle Signal;
        }

        private List<WorkerThread> WorkerThreads = new List<WorkerThread>();
        private ConcurrentQueue<GameComponent> PendingUpdates = new ConcurrentQueue<GameComponent>();
        private int RunningThreads = 4;
        private EventWaitHandle ThreadController = new EventWaitHandle(false, EventResetMode.ManualReset);

        public ComponentTransformModule()
        {
            for (var i = 0; i < RunningThreads; ++i)
                WorkerThreads.Add(SpawnThread());

            foreach (var thread in WorkerThreads)
                thread.Thread.Start(thread);
         }

        private WorkerThread SpawnThread()
        {
            var worker = new WorkerThread
            {
                Thread = new Thread(ThreadFunction) { IsBackground = true },
                Signal = new EventWaitHandle(false, EventResetMode.ManualReset)
            };

            return worker;
        }

        private void ThreadFunction(Object Info)
        {
            while (true)//!DwarfGame.ExitGame)
            {
                ThreadController.WaitOne();
                while (PendingUpdates.TryDequeue(out var component))
                    component.ProcessTransformChange();
                (Info as WorkerThread).Signal.Set();
            }
        }

        public override void Update(DwarfTime GameTime, WorldManager World)
        {
            PerformanceMonitor.PushFrame("Transform Update");
            PerformanceMonitor.SetMetric("Transforms", World.ComponentUpdateSet.Count);
            foreach (var item in World.ComponentUpdateSet)
                PendingUpdates.Enqueue(item);
            foreach (var thread in WorkerThreads)
                if (!thread.Thread.IsAlive)
                    throw new InvalidProgramException("Thread should be alive.");
            
            ThreadController.Set();
            foreach (var thread in WorkerThreads)
                thread.Signal.WaitOne();
            //while (RunningThreads > 0) { }
            PerformanceMonitor.PopFrame();
        }

        public override void Shutdown()
        {
            foreach (var thread in WorkerThreads)
                thread.Thread.Abort();
        }
    }
}
