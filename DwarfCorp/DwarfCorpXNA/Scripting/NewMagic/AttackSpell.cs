using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.NewMagic
{
    class AttackSpell : Spell
    {
        // Todo: This class has warnings but is never actually used?

        public Attack Attack;
        public float Bonus = 0.0f;

        public override bool ApplyObjects(Creature self, IEnumerable<Body> objects)
        {
            var body = Datastructures.SelectRandom(objects);
            base.ApplyObjects(self, objects);
            if (body != null)
            {
                return Attack.Perform(self, body, DwarfTime.LastTime, Bonus, body.Position, self.Faction.Name);
            }
            return true;
        }
    }
}
