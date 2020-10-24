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

        public override ModuleManager.UpdateTypes UpdatesWanted => ModuleManager.UpdateTypes.ComponentCreated 
            | ModuleManager.UpdateTypes.ComponentDestroyed 
            | ModuleManager.UpdateTypes.Update;

        public struct AIComponent
        {
            public CreatureAI AI;
            public TimeSpan? TimeOfLastUpdate;
        }

        public List<AIComponent> AIComponents = new List<AIComponent>();
        public int CurrentComponentIndex = -1;

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
            if (AIComponents.Count == 0)
                return;

            var aiObjectsUpdatedThisFrame = 0;
            var aiObjectsChecked = 0;
            PerformanceMonitor.PushFrame("AISystem");
            while (true)
            {
                CurrentComponentIndex += 1;

                if (CurrentComponentIndex >= AIComponents.Count)
                    CurrentComponentIndex = 0;

                aiObjectsChecked += 1;

                if (CurrentComponentIndex < AIComponents.Count)
                {
                    var thisObject = AIComponents[CurrentComponentIndex];
                    if (thisObject.AI.GetRoot().UpdateFrame == World.EntityUpdateFrame)
                    {
                        var elapsedTime = GameTime;
                        if (thisObject.TimeOfLastUpdate.HasValue)
                        {
                            var timeSinceUpdate = GameTime.TotalGameTime - thisObject.TimeOfLastUpdate.Value;
                            elapsedTime = new DwarfTime(GameTime.TotalGameTime, timeSinceUpdate);
                        }

                        AIComponents[CurrentComponentIndex] = new AIComponent
                        {
                            AI = thisObject.AI,
                            TimeOfLastUpdate = GameTime.TotalGameTime
                        };

                        thisObject.AI.FrameDeltaTime = elapsedTime;
                        thisObject.AI.AIUpdate(elapsedTime, World.ChunkManager, World.Renderer.Camera);
                        aiObjectsUpdatedThisFrame += 1;
                        if (aiObjectsUpdatedThisFrame == GameSettings.Current.MaxAIUpdates)
                            break;
                    }
                }

                if (aiObjectsChecked == AIComponents.Count)
                    break; // Prevent multiple updates if there are less AI objects than the max single frame update count.
            }
            PerformanceMonitor.SetMetric("AI Objects", aiObjectsUpdatedThisFrame);
            PerformanceMonitor.PopFrame();
        }
    }
}
