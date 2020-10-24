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
    public class MinimapIconModule : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new MinimapIconModule();
        }

        private List<MinimapIcon> MinimapIcons = new List<MinimapIcon>();
        public IEnumerable<MinimapIcon> GetMinimapIcons() { return MinimapIcons; }

        public override ModuleManager.UpdateTypes UpdatesWanted => ModuleManager.UpdateTypes.ComponentCreated
            | ModuleManager.UpdateTypes.ComponentDestroyed
            | ModuleManager.UpdateTypes.Shutdown;

        public MinimapIconModule()
        {
        }

        public override void ComponentCreated(GameComponent C)
        {
            if (C is MinimapIcon icon)
                MinimapIcons.Add(icon); 
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            if (C is MinimapIcon icon)
                MinimapIcons.Remove(icon);
        }

        public override void Shutdown()
        {
            MinimapIcons.Clear();
        }
    }
}
