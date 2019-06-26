using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Thought
    {
        public string Description { get; set; }
        public float HappinessModifier { get; set; }
        public DateTime TimeStamp { get; set; }
        public TimeSpan TimeLimit { get; set; }

        public bool IsOver(DateTime Time)
        {
            return (Time - TimeStamp) >= TimeLimit;
        }
    }
}
