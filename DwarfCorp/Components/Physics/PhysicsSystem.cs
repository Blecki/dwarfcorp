using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PhysicsSystem : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new PhysicsSystem();
        }

        public override void Update(DwarfTime GameTime, WorldManager World)
        {
            var physicsObject = 0;
            PerformanceMonitor.PushFrame("PhysicsSystem");
            foreach (var physicsComponent in World.ComponentUpdateSet.OfType<Physics>())
            {
                physicsObject += 1;
                if (physicsComponent is ResourceEntity)
                {
                    var x = 5;
                }
                physicsComponent.PhysicsUpdate(GameTime, World.ChunkManager);
            }
            PerformanceMonitor.SetMetric("Physics Objects", physicsObject);
            PerformanceMonitor.PopFrame();
        }
    }
}
