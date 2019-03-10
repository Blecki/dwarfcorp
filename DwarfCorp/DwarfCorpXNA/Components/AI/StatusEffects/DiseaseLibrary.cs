using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    public class DiseaseLibrary
    {
        private static List<Disease> Diseases { get; set; }

        static DiseaseLibrary()
        {
            Diseases = FileUtils.LoadJsonListFromMultipleSources<Disease>(ContentPaths.diseases, null, d => d.Name);
        }

        public static Disease GetRandomInjury()
        {
            return Datastructures.SelectRandom(Diseases.Where(disease => disease.IsInjury));
        }

        public static Disease GetDisease(string name)
        {
            return Diseases.Where(d => d.Name == name).FirstOrDefault();
        }

        public static Disease GetRandomDisease()
        {
            return Datastructures.SelectRandom(Diseases.Where(disease => !disease.IsInjury));
        }

        public static void SpreadRandomDiseases(IEnumerable<CreatureAI> creatures)
        {
            var disease = Datastructures.SelectRandom(Diseases.Where(d => d.AcquiredRandomly));
            if (disease == null)
                return;
            var creature = Datastructures.SelectRandom(creatures);
            if (creature == null)
                return;
            creature.Creature.AcquireDisease(disease.Name);
        }
    }
}
