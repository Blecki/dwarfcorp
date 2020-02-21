using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PacingCreatureAI : CreatureAI
    {
        public PacingCreatureAI()
        {

        }

        public PacingCreatureAI(ComponentManager Manager, string name, EnemySensor sensors) :
            base(Manager, name, sensors)
        {

        }

        public override Act ActOnWander()
        {
            return new Sequence(
                new WanderAct(this, 6, 0.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f),
                new LongWanderAct(this) { PathLength = 10, Radius = 50 });
        }
    }
   
}
