using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class SteamSystem : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new SteamSystem();
        }

        private List<SteamPoweredObject> Objects = new List<SteamPoweredObject>();

        public override void ComponentCreated(GameComponent C)
        {
            if (C is SteamPoweredObject steamObject)
                Objects.Add(steamObject);
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            if (C is SteamPoweredObject steamObject)
                Objects.Remove(steamObject);
        }

        public override void Update(DwarfTime GameTime)
        {
            // Todo: Limit update rate.

            foreach (var steamObject in Objects)
            {
                if (steamObject.Generator)
                    steamObject.SteamPressure = steamObject.GeneratedSteam;
                else
                {
                    var total = steamObject.SteamPressure;
                    var count = 1.0f;
                    foreach (var neighbor in steamObject.NeighborPipes.Select(id => steamObject.Manager.FindComponent(id)).OfType<SteamPoweredObject>())
                    {
                        total += neighbor.SteamPressure;
                        count += 1;
                    }

                    steamObject.SteamPressure = total / count;
                    steamObject.SteamPressure *= 0.995f;
                }
            }
        }
    }
}
