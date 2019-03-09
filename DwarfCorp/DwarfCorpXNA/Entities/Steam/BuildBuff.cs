using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class BuildBuff : GameComponent
    {
        public float BuffMultiplier = 1.5f;
        public virtual float GetBuffMultiplier() { return BuffMultiplier; }

        public BuildBuff(ComponentManager Manager) : base("Build Buff", Manager)
        { }
    }
}
