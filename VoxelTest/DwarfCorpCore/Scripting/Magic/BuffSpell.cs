using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BuffSpell : Spell
    {
        public List<Creature.Buff> Buffs { get; set; } 
        
        public BuffSpell()
        {
            
        }

        public BuffSpell(params Creature.Buff[] buffs)
        {
            Buffs = buffs.ToList();
            Mode = SpellMode.SelectEntities;
            Name = "Buff spell";
            Description = "Apply buffs to selected creatures";
            Hint = "Click and drag to select creatures";
            ManaCost = 20;
            Image = new NamedImageFrame(ContentPaths.GUI.icons, 32, 0, 2);
        }


        public override void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            foreach (Body body in entities)
            {
                Creature creature = body.GetChildrenOfType<Creature>().FirstOrDefault();

                if (creature == null) continue;
                else
                {
                    foreach (Creature.Buff buff in Buffs)
                    {
                        if (OnCast(tree))
                        {
                            creature.AddBuff(buff.Clone());
                        }
                    }
                }
            }
            base.OnEntitiesSelected(tree, entities);
        }
    }
}
