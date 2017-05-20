using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Tutorial
{
    public class JsonTutorialEntry
    {
        public string Name;
        public string Text;
    }

    public class JsonTutorialSet
    {
        public List<JsonTutorialEntry> Tutorials;
    }
}
