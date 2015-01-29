using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

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

        public static string[] GetDefaultStrings(ContentPaths.Text.TextType type)
        {
            string text = "";
            using (var stream = TitleContainer.OpenStream("Content" + ProgramData.DirChar + ContentPaths.Text.Texts[type]))  
            {  
                using (var reader = new StreamReader(stream))  
                {  
                    text = reader.ReadToEnd();  
                }  
            }  
            return text.Split('\n', ' ');
        }

        public static void CreateDefaults()
        {
            TextAtoms = new Dictionary<string, TextAtom>();
            string[] firstNames = GetDefaultStrings(ContentPaths.Text.TextType.FirstNames);
            string[] lastNames = GetDefaultStrings(ContentPaths.Text.TextType.LastNames);
            TextAtom animals = new TextAtom("$Animal", GetDefaultStrings(ContentPaths.Text.TextType.Animals));
            TextAtom bodyparts = new TextAtom("$Bodypart", "Hair", "Ear", "Eye", "Nose", "Teeth", "Tongue", "Chin", "Beard", "Moustache", "Chest", "Shoulders", "Arm", "Hand", "Finger", "Thumb", "Stomach", "Heart", "Liver", "Leg", "Foot", "Toe");
            TextAtom family = new TextAtom("$Family", "Father", "Mother", "Son", "Daughter", "Brother", "Sister", "Uncle", "Aunt", "Grandfather", "Grandmother", "Cousin", "Nephew", "Niece");
            TextAtom corp = new TextAtom("$Corp", "Ltd.", "Partnership", "Corp", "Company", "Corporation", "Inc.", "Incorporated", "Holdings");
            TextAtom land = new TextAtom("$Place", "Island", "Peninsula", "Continent", "Realm", "City", "Burg", "Town", "Village", "Mountain", "Hills", "Desert", "Well", "Marsh", "Vale", "Street");
            TextAtom maleName = new TextAtom("$MaleName", firstNames);
            TextAtom colors = new TextAtom("$Color", "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Violet", "Indigo", "Black", "Grey", "White", "Pink", "Brown", "Cyan");
            TextAtom materials = new TextAtom("$Material", "Iron", "Stone", "Gold", "Steel", "Wood", "Ice", "Dirt", "Water", "Fire", "Bronze");
            TextAtom adverb = new TextAtom("$Adverb", GetDefaultStrings(ContentPaths.Text.TextType.Adverbs));
            TextAtom verb = new TextAtom("$Verb", GetDefaultStrings(ContentPaths.Text.TextType.Verbs));
            TextAtom adjective = new TextAtom("$Adjective", GetDefaultStrings(ContentPaths.Text.TextType.Adjectives));
            TextAtom noun = new TextAtom("$Noun", GetDefaultStrings(ContentPaths.Text.TextType.Nouns));
            TextAtom interjection = new TextAtom("$Interjection", "Alas", "O", "Yay", "No", "Yes", "Still", "Yet", "But", "However", "Eureka", "Yeah", "Bam");
            TextAtom dwarfName = new TextAtom("$DwarfName", firstNames);
            TextAtom dwarfFamily = new TextAtom("$DwarfFamily", lastNames);
            TextAtom goblinName = new TextAtom("$GoblinName", "Lurtzog", "Gorkil", "Baluk", "Agrag", "Shakil", "Gashur", "Mega", "Balug", "Uglur", "Lagdush", "Oldog", "Muzga", "Lugdush");
            TextAtom goblinFamily = new TextAtom("$GoblinFamily", "Ugdush", "Gashur", "Balcmurz", "Orgbag", "Azod", "Rat", "Lukil" );
            TextAtom professions = new TextAtom("$Professions", "Urchin", "Homeless", "Fishmonger", "Beggar", "Factory Worker", "Student", "Fugitive", "Convict", "Carpenter", "Roofer", "Ditch Digger", "Disgraced Noble", "Policeman", "Conscript", "Eunuch", "Prostitute", "Grifter", "Bum", "Vagrant", "Vagabond", "Hobo", "Inebriate", "Servant", "Slave", "Congressdwarf", "Mayor", "Councildwarf");
            TextAtom magical = new TextAtom("$Magical", "University", "School", "Magerium", "Magicians", "Arcana", "Institute", "Learning Center", "Library", "Museum");
            TextAtom military = new TextAtom("$Military", "Thugs", "Band", "Company", "Heroes", "Strongdwarves", "Mercenaries", "Dwarves-at-arms", "Shield", "Axe", "Sword", "Army", "Fighters", "Knights");
            TextAtom industrial = new TextAtom("$Industry", "Industries", "Materials", "Techcenter", "Factories", "Machines", "Parts", "Hammers");
            TextAtom elfName = new TextAtom("$ElfName", "Lolly", "Lilly", "Eelie", "Gumdrop", "Bubblegum" ,"Poppy", "Fizzle", "Pickle", "Elfie", "Candy", "Laddy");
            TextAtom elfFamily = new TextAtom("$ElfFamily", "McWilly", "Wizzlebum", "Lollyland", "Doopy", "McToetoe", "Whipper", "Elfson", "Gribble", "Nibble");
            AddAtom(animals);
            AddAtom(bodyparts);
            AddAtom(family);
            AddAtom(corp);
            AddAtom(land);
            AddAtom(maleName);
            AddAtom(colors);
            AddAtom(elfName);
            AddAtom(elfFamily);
            AddAtom(dwarfName);
            AddAtom(goblinName);
            AddAtom(dwarfFamily);
            AddAtom(goblinFamily);
            AddAtom(materials);
            AddAtom(adverb);
            AddAtom(verb);
            AddAtom(interjection);
            AddAtom(professions);
            AddAtom(magical);
            AddAtom(military);
            AddAtom(industrial);
            AddAtom(adjective);
            AddAtom(noun);
            staticsInitialized = true;
        }

        public static string ToTitleCase(string strX)
        {
            string[] aryWords = strX.Trim().Split(' ');

            List<string> lstLetters = new List<string>();
            List<string> lstWords = new List<string>();

            foreach (string strWord in aryWords)
            {
                int iLCount = 0;
                foreach (char chrLetter in strWord.Trim())
                {
                    if (iLCount == 0)
                    {
                        lstLetters.Add(chrLetter.ToString().ToUpper());
                    }
                    else
                    {
                        lstLetters.Add(chrLetter.ToString().ToLower());
                    }
                    iLCount++;
                }
                lstWords.Add(string.Join("", lstLetters));
                lstLetters.Clear();
            }

            string strNewString = string.Join(" ", lstWords);

            return strNewString;
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
                    toReturn += ToTitleCase(TextAtoms[s].GetRandom());
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