using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class SteamSystem : UpdateSystem
    {
        [UpdateSystemFactory]
        private static UpdateSystem __factory(WorldManager World)
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
            DwarfGame.GetConsoleTile("STEAM PRESSURE").Text = "STEAM OBJECTS " + Objects.Count;
        }
    }
}
