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
        public List<String> Immunities = new List<String>();

        public void AcquireDisease(Disease disease)
        {
            if (disease == null)
                return;

            if (Immunities.Any(immunity => immunity == disease.Name))
                return;

            if (!Buffs.Any(b => b is Disease d && d.Name == disease.Name))
            {
                var buff = disease.Clone() as Disease;
                AddBuff(buff);
                if (!buff.IsInjury)
                    Immunities.Add(disease.Name);
            }
        }
    }
}
