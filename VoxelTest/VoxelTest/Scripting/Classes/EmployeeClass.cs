using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class EmployeeClass 
    {
        public class Level
        {
            public int Index;
            public string Name;
            public float Pay;
            public int XP;
        }

        public List<Level> Levels { get; set; }
        public string Name { get; set; }

    }
}
