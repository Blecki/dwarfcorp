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
        private static bool Initialized = false;

        private static void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;

            Diseases = FileUtils.LoadJsonListFromDirectory<Disease>(ContentPaths.diseases, null, d => d.Name);

            Console.WriteLine("Loaded Disease Library.");
        }

        public static Disease GetRandomInjury()
        {
            Initialize();
            var r = Diseases.FirstOrDefault(d => d.Name == "Injury").Clone() as Disease;
            r.Name = TextGenerator.GenerateRandom(TextGenerator.Templates["$injuries"]);
            return r;
        }

        public static Disease GetDisease(string name)
        {
            Initialize();
            return Diseases.Where(d => d.Name == name).FirstOrDefault();
        }

        public static Disease GetRandomDisease()
        {
            Initialize();
            return Datastructures.SelectRandom(Diseases.Where(disease => !disease.IsInjury));
        }

        public static void SpreadRandomDiseases(IEnumerable<CreatureAI> creatures)
        {
            Initialize();
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
