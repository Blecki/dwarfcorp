using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{

    // This class only exists for backwards compatibility with versions pre 19.02.07.
    public class MudGolemAI : GolemAI
    {
        public MudGolemAI()
        {

        }
    }

    public class GolemAI : CreatureAI
    {
        public GolemAI()
        {
            
        }

        public GolemAI(ComponentManager Manager, EnemySensor enemySensor) :
            base(Manager, "MudGolemAI", enemySensor)
        {
            
        }
        public override Task ActOnIdle()
        {
            return null;
        }

        public override Act ActOnWander()
        {
            return null;
        }
    }
}
