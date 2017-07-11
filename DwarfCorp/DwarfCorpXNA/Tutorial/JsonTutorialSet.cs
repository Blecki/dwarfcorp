using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Tutorial
{
    public class JsonTutorialEntry
    {
        public String Name;
        public String Text;
        public String Title;
        public String GuiHilite;
    }

    public class JsonTutorialSet
    {
        public List<JsonTutorialEntry> Tutorials;
    }
}
