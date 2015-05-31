// TextGenerator.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        public static List<List<string> > GetAtoms(string type)
        {
            string text = "";
            using (var stream = TitleContainer.OpenStream("Content" + ProgramData.DirChar + type))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            List<List<string>> toReturn = new List<List<string>>();
            foreach (string line in text.Split('\n', '\r'))
            {
                List<string> current = new List<string>();
                if (string.IsNullOrEmpty(line) || line == "\n" || line == "\r")
                {
                    continue;
                }

                foreach (string word in Regex.Split(line, @"(?<=[\S\n])(?=\s)"))
                {
                    var match = Regex.Match(word, @"(.*)\<(.*)\>(.*)");

                    if (match.Success)
                    {
                        string before = match.Groups[1].Value;
                        string middle = match.Groups[2].Value;
                        string after = match.Groups[3].Value;

                        if (!string.IsNullOrEmpty(before))
                        {
                            current.Add(before);
                        }

                        if (!string.IsNullOrEmpty(middle))
                        {
                            current.Add("$" + middle);
                        }

                        if (!string.IsNullOrEmpty(after))
                        {
                            current.Add(after);
                        }
                    }
                    else
                    {
                        current.Add(word);
                    }

                    
                }

                toReturn.Add(current);
            }

            return toReturn;
        }

        public static string[] GetDefaultStrings(string type)
        {
            string text = "";
            using (var stream = TitleContainer.OpenStream("Content" + ProgramData.DirChar + type))  
            {  
                using (var reader = new StreamReader(stream))  
                {  
                    text = reader.ReadToEnd();  
                }  
            }  
            return text.Split('\n', '\r').Where(a => a != "" && a != " " && a != "\r" && a != "\n").ToArray();
        }

        public static void CreateDefaults()
        {
            TextAtoms = new Dictionary<string, TextAtom>();
            LoadAtoms();
            staticsInitialized = true;
        }

        public static void LoadAtoms()
        {
            string dirname = "." + ProgramData.DirChar + "Content" + ProgramData.DirChar + "Text";
            System.IO.DirectoryInfo directoryInfo = new DirectoryInfo(dirname);

            if (!directoryInfo.Exists) throw new FileNotFoundException("Unable to find text directory : " + dirname);

            foreach (System.IO.FileInfo info in directoryInfo.EnumerateFiles("*.txt"))
            {
                var match = Regex.Match(info.Name, @"(.*)\.txt");

                if (match.Success)
                {
                    AddAtom(new TextAtom("$" + match.Groups[1].Value, GetDefaultStrings("Text" + ProgramData.DirChar + info.Name)));
                }
            }
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
                        lstLetters.Add(chrLetter.ToString(CultureInfo.InvariantCulture).ToUpper());
                    }
                    else
                    {
                        lstLetters.Add(chrLetter.ToString(CultureInfo.InvariantCulture).ToLower());
                    }
                    iLCount++;
                }
                lstWords.Add(string.Join("", lstLetters));
                lstLetters.Clear();
            }

            string strNewString = string.Join(" ", lstWords);

            return strNewString;
        }

        public static string GenerateRandom(List<List<string>> templates)
        {
            return GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
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