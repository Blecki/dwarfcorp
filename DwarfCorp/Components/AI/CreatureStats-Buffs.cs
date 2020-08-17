using System;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public partial class CreatureStats
    {
        public List<StatusEffect> Buffs = new List<StatusEffect>();
        private List<StatusEffect> BuffsToAdd = new List<StatusEffect>();

        public void AddBuff(StatusEffect buff)
        {
            BuffsToAdd.Add(buff);
        }

        public void HandleBuffs(Creature Me, DwarfTime time)
        {
            Motivation.CurrentValue = 0.0f;

            foreach (var newBuff in BuffsToAdd)
            {
                var matchingBuffs = Buffs.Where(b => b.GetType() == newBuff.GetType()).ToList();
                foreach (var matchingBuff in matchingBuffs)
                {
                    matchingBuff.OnEnd(Me);
                    Buffs.Remove(matchingBuff);
                }

                newBuff.OnApply(Me);
                Buffs.Add(newBuff);
            }

            BuffsToAdd.Clear();

            foreach (StatusEffect buff in Buffs)
                buff.Update(time, Me);

            foreach (StatusEffect buff in Buffs.FindAll(buff => !buff.IsInEffect))
            {
                buff.OnEnd(Me);
                Buffs.Remove(buff);
            }
        }
    }
}
