using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class TextGenerator
    {
        public static Dictionary<string, TextAtom> TextAtoms { get; set; }
        private static bool staticsInitialized = false;

        public static string[] Literals =
        {
            " ",
            ".",
            "!",
            "&",
            "(",
            ")",
            ",",
            "+",
            "-",
            "%",
            "#",
            "_",
            "@",
            "$",
            "^",
            "*",
            "\n",
            "\t",
            "\"",
            "\'"
        };

        public void CreateDefaults()
        {
            TextAtom animals = new TextAtom("$Animal", "Aardvark", "Bee", "Cat", "Dog", "Elephant", "Giraffe", "Hippo", "Iguana", "Jaguar", "Koala", "Lemur", "Manatee", "Nematode", "Ostrich", "Porpoise", "Quail", "Snake", "Tiger", "Ungulate", "Viper", "Whale", "Yak", "Zebra");
            TextAtom bodyparts = new TextAtom("$Bodypart", "Hair", "Ear", "Eye", "Nose", "Teeth", "Tounge", "Chin", "Beard", "Moustache", "Chest", "Shoulders", "Arm", "Hand", "Finger", "Thumb", "Stomach", "Heart", "Liver", "Leg", "Foot", "Toe");
            TextAtom family = new TextAtom("$Family", "Father", "Mother", "Son", "Daughter", "Brother", "Sister", "Uncle", "Aunt", "Gradfather", "Grandmother", "Cousin", "Nephew", "Niece");
            TextAtom corp = new TextAtom("$Corp", "Ltd.", "Partnership", "Corp", "Company", "Corporation", "Inc.", "Incorporated", "Holdings");
            TextAtom land = new TextAtom("$Place", "Island", "Penninsula", "Continent", "Realm", "City", "Burg", "Town", "Villiage", "Mountain", "Hills", "Desert", "Well", "Marsh", "Vale", "Street");
            TextAtom maleName = new TextAtom("$MaleName", "John", "James", "Bill", "Larry", "Edgar", "Chuck", "Matthew", "Peter", "Mark", "Luke", "Jeb", "Bob", "Dunold", "Zult", "Werix", "Krom");
            TextAtom femaleName = new TextAtom("$FemaleName", "Sarah", "Becca", "Zoe", "Cleo", "Ashley", "Liz", "Allison", "Emily", "Erica", "Bethany", "Janet");
            TextAtom colors = new TextAtom("$Color", "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Violet", "Indigo", "Black", "Grey", "White");
            TextAtom materials = new TextAtom("$Material", "Iron", "Stone", "Gold", "Steel", "Wood", "Ice", "Dirt", "Water", "Fire");
            TextAtom adverb = new TextAtom("$Adverb", "Angrily", "Slowly", "Valiantly", "Quickly", "Grudgingly", "Greatly", "Amazingly", "Understandably", "Deniably", "Meticulously", "Eagerly", "Lovingly");
            TextAtom verb = new TextAtom("$Verb", "Sit", "Shake", "Work", "Hit", "Attack", "Destroy", "Build", "Create", "Mine", "Explore", "Guard", "Survive", "Profit", "Defend", "Die", "Live");
            TextAtom interjection = new TextAtom("$Interjection", "Alas", "O", "Yay", "No", "Yes", "Still", "Yet", "But", "However");

            AddAtom(animals);
            AddAtom(bodyparts);
            AddAtom(family);
            AddAtom(corp);
            AddAtom(land);
            AddAtom(maleName);
            AddAtom(femaleName);
            AddAtom(colors);
            AddAtom(materials);
            AddAtom(adverb);
            AddAtom(verb);
            AddAtom(interjection);
        }

        public string GenerateRandom(params string[] atoms)
        {
            string toReturn = "";
            foreach(string s in atoms)
            {
                if(Literals.Contains(s))
                {
                    toReturn += s;
                }
                else if(TextAtoms.ContainsKey(s))
                {
                    toReturn += TextAtoms[s].GetRandom();
                }
                else
                {
                    toReturn += s;
                }
            }

            return toReturn;
        }

        public TextGenerator()
        {
            if(!staticsInitialized)
            {
                TextAtoms = new Dictionary<string, TextAtom>();
                CreateDefaults();
            }
        }

        public static void AddAtom(TextAtom atom)
        {
            TextAtoms[atom.Name] = atom;
        }
    }

}