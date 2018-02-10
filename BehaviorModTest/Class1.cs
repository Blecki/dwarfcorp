using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp;
using Microsoft.Xna.Framework;

namespace BehaviorModTest
{
    public class Class1
    {
        [EntityFactory("ModCrate")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Crate(Manager, Position);
        }

    }
}
