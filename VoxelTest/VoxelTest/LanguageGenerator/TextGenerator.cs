using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    /// <summary>
    /// Generates random strings of text based on patterns. Like mad libs.
    /// </summary>
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

        public static void CreateDefaults()
        {
            TextAtoms = new Dictionary<string, TextAtom>();
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
            TextAtom dwarfName = new TextAtom("$DwarfName", "Thuli", "Muli", "Fili", "Kunan", "Bali", "Kari", "Thali", "Arul", "Groda", "Thrinarv", "Igin", "Urund", "Urud", "Undil", "Ugmad", "Thrasanz", "Arud", "Gwari", "Zuri", "Kamin", "Gilli", "Akkar");
            TextAtom dwarfFamily = new TextAtom("$DwarfFamily", "Greythorn", "Redthorn", "Greystone", "Redstone", "Goldstone", "Goldthorn", "Greybeard", "Redbeard", "Bluebeard", "Stonearm", "Witchpipe", "Fathunt", "Casker", "Harpsinger", "Khundushath", "Bilgabar", "Naragzinb", "Nargathur", "Bizaram", "Baragzar", "Kibarak", "Smith", "Belcher", "Bricker", "Greatmine");
            TextAtom goblinName = new TextAtom("$GoblinName", "Lurtzog", "Gorkil", "Baluk", "Agrag", "Shakil", "Gashur", "Mega", "Balug", "Uglur", "Lagdush", "Oldog", "Muzga", "Lugdush");
            TextAtom goblinFamily = new TextAtom("$GoblinFamily", "Ugdush", "Gashur", "Balcmurz", "Orgbag", "Azod", "Rat", "Lukil" );
            TextAtom professions = new TextAtom("$Professions", "Urchin", "Homeless", "Fishmonger", "Beggar", "Factory Worker", "Student", "Fugitive", "Convict", "Carpenter", "Roofer", "Ditch Digger", "Disgraced Noble", "Policeman", "Conscript", "Enuch");
            AddAtom(animals);
            AddAtom(bodyparts);
            AddAtom(family);
            AddAtom(corp);
            AddAtom(land);
            AddAtom(maleName);
            AddAtom(femaleName);
            AddAtom(colors);
            AddAtom(dwarfName);
            AddAtom(goblinName);
            AddAtom(dwarfFamily);
            AddAtom(goblinFamily);
            AddAtom(materials);
            AddAtom(adverb);
            AddAtom(verb);
            AddAtom(interjection);
            AddAtom(professions);
            staticsInitialized = true;
        }

        public static string GenerateRandom(params string[] atoms)
        {
            if(!staticsInitialized)
            {
                CreateDefaults();
            }
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
                else if (Regex.Match(s, @"\${(.*?)}").Success)
                {
                    string match = Regex.Match(s, @"\${(.*?)}").Groups[1].Value;
                    string[] splits = match.Split(',');

                    if(splits.Length > 0)
                        toReturn += splits[PlayState.Random.Next(splits.Length)];
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
                staticsInitialized = true;
            }
        }

        public static void AddAtom(TextAtom atom)
        {
            TextAtoms[atom.Name] = atom;
        }
    }

}