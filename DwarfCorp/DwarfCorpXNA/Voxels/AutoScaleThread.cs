using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DwarfCorp
{
    public class AutoScaleThread
    {
        private Thread Thread;
        private Action<float> Worker;
        private ChunkManager Manager;

        public float TimeBudget = 0.05f; // How long the thread can be allowed to run per second.
        public float Frequency = 0.2f;
        public float FrequencyStep = 0.3f;
        public float MaxFrequency = 2.0f;
        public float MinFrequency = 0.1f;
        public int MaxRecordsTracked = 10;

        private DateTime LastRan = DateTime.Now;

        private class Run
        {
            public DateTime StartTime;
            public TimeSpan Duration;
        }

        private LinkedList<Run> RecentRuns = new LinkedList<Run>();

        public AutoScaleThread(ChunkManager Manager, Action<float> Worker)
        {
            this.Manager = Manager;
            this.Worker = Worker;

            Thread = new Thread(MainLoop);
        }

        public void Start()
        {
            Thread.Start();
        }

        public void Join()
        {
            Thread.Join();
        }

        private void MainLoop()
        {
#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !Manager.ExitThreads)
                {
                    if (Program.ShutdownEvent.WaitOne(1))
                        break;

                    
                    if (!Manager.PauseThreads)
                    {
                        var timeNow = DateTime.Now;
                        var delta = timeNow - LastRan;

                        if (delta.TotalSeconds >= Frequency)
                        {
                            LastRan = timeNow;

                            Worker(1.0f);

                            var endTime = DateTime.Now;

                            RecentRuns.AddLast(new Run
                            {
                                StartTime = timeNow,
                                Duration = endTime - timeNow
                            });

                            while (RecentRuns.Count > MaxRecordsTracked)
                                RecentRuns.RemoveFirst();
                            var sumTime = RecentRuns.Sum(r => r.Duration.TotalSeconds);
                            var runTimeSpan = (endTime - RecentRuns.First.Value.StartTime).TotalSeconds;
                            var timeUsed = sumTime / runTimeSpan;
                            
                            if (timeUsed > TimeBudget)
                            {
                                Frequency = Math.Min(Frequency * (1.0f +  FrequencyStep), MaxFrequency);
                            }
                            else if (timeUsed < TimeBudget)
                            {
                                Frequency = Math.Max(Frequency * (1.0f - FrequencyStep), MinFrequency);
                            }
                        }
                        else
                        {
                            var sleepTime = (Frequency - delta.TotalSeconds) * 1000;
                            Thread.Sleep((int)sleepTime);
                        }
                    }

                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }
    }
}
