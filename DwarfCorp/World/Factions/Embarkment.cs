using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace DwarfCorp
{
    public class Embarkment
    {
        public String Name;
        public int Difficulty;
        public List<String> Party;
        public Dictionary<String, int> Resources;
        public DwarfBux Money;
    }
}
