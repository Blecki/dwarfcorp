using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class PowerAnvil : CraftedFixture
    {
        [EntityFactory("Power Anvil")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var r = new CraftedFixture("Power Anvil", new String[] { "Anvil" }, 
                Manager, 
                Position, 
                new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), 
                new Point(0, 3), Data.GetData<List<ResourceAmount>>("Resources", null));
            r.AddChild(new BuildBuff(Manager) { BuffMultiplier = 2.0f });
            return r;
        }
    }    
}
