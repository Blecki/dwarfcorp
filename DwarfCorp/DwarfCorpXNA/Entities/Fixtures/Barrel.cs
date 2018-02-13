using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static class Barrel
    {
        [EntityFactory("Barrel")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Fixture("Barrel", new String[] { "Barrel" }, Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 0));
        }
    }
}
