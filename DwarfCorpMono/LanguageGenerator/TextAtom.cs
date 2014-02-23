using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class TextAtom
    {
        public string Name { get; set; }
        public List<string> Terms { get; set; }

        public TextAtom(string name, params string[] terms)
        {
            Name = name;
            Terms = new List<string>();

            for (int i = 0; i < terms.Length; i++)
            {
                Terms.Add(terms[i]);
            }
        }

        public TextAtom(string name, List<string> terms)
        {
            Name = name;
            Terms = terms;
        }


        public string GetRandom()
        {
            return Terms[PlayState.random.Next(Terms.Count)];
        }
    }
}
