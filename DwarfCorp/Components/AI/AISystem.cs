using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AISystem : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new AISystem();
        }

        public struct AIComponent
        {
            public CreatureAI AI;
            public TimeSpan? TimeOfLastUpdate;
        }

        public List<AIComponent> AIComponents = new List<AIComponent>();
        public int currentIndex = -1;

        public override void ComponentCreated(GameComponent C)
        {
            if (C is CreatureAI ai)
                AIComponents.Add(new AIComponent
                {
                    AI = ai,
                    TimeOfLastUpdate = null
                });
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            AIComponents.RemoveAll(c => Object.ReferenceEquals(C, c.AI));
        }

        public override void Update(DwarfTime GameTime, WorldManager World)
        {
            var aiObjectsUpdatedThisFrame = 0;
            var maxObjects = 32;
            var startIndex = currentIndex;
            PerformanceMonitor.PushFrame("AISystem");
            while (true)
            {
                currentIndex += 1;

                if (currentIndex >= AIComponents.Count)
                    currentIndex = 0;

                if (currentIndex == startIndex)
                    break;

                var thisObject = AIComponents[currentIndex];
                if (thisObject.AI.GetRoot().UpdateFrame == World.EntityUpdateFrame)
                {
                    var elapsedTime = GameTime;
                    if (thisObject.TimeOfLastUpdate.HasValue)
                    {
                        var timeSinceUpdate = GameTime.TotalGameTime - thisObject.TimeOfLastUpdate.Value;
                        elapsedTime = new DwarfTime(GameTime.TotalGameTime, timeSinceUpdate);
                    }

                    AIComponents[currentIndex] = new AIComponent
                    {
                        AI = thisObject.AI,
                        TimeOfLastUpdate = GameTime.TotalGameTime
                    };

                    thisObject.AI.FrameDeltaTime = elapsedTime;
                    thisObject.AI.AIUpdate(elapsedTime, World.ChunkManager, World.Renderer.Camera);
                    aiObjectsUpdatedThisFrame += 1;
                    if (aiObjectsUpdatedThisFrame == maxObjects)
                        break;
                }
            }
            PerformanceMonitor.SetMetric("AI Objects", aiObjectsUpdatedThisFrame);
            PerformanceMonitor.PopFrame();
        }
    }
}
