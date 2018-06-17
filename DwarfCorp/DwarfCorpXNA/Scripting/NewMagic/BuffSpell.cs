using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.NewMagic
{
    class BuffSpell : Spell
    {
        public List<Buff> Buffs { get; set; }

        public override bool ApplyObjects(Creature self, IEnumerable<Body> objects)
        {
            foreach (Body body in objects)
            {
                var creature = body.EnumerateAll().OfType<Creature>().FirstOrDefault();

                if (creature == null) continue;
                else
                {
                    foreach (var buff in Buffs)
                    {
                        creature.AddBuff(buff.Clone());
                    }
                }
            }
            base.ApplyObjects(self, objects);
            return true;
        }

        public override bool ApplySelf(Creature self)
        {
            foreach(var buff in Buffs)
            {
                self.AddBuff(buff);
            }
            base.ApplySelf(self);
            return true;
        }
    }
}
