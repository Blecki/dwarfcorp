using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class SteamPoweredBuildBuff : BuildBuff
    {
        public float SteamThreshold = 0.25f;

        private SteamPoweredObject CachedSteamObject = null;

        public SteamPoweredBuildBuff(ComponentManager Manager) : base(Manager)
        { }

        public override float GetBuffMultiplier()
        {
            if (CachedSteamObject == null)
                CachedSteamObject = Parent.GetComponent<SteamPoweredObject>();
            if (CachedSteamObject != null && CachedSteamObject.SteamPressure >= SteamThreshold)
                return BuffMultiplier;
            return 1.0f;
        }
    }
}
