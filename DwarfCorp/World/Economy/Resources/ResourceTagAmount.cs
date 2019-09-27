using System;
using System.Diagnostics;

namespace DwarfCorp
{
    public class ResourceTagAmount
    {
        public String Tag { get; set; }
        public int Count { get; set; }
        
        public ResourceTagAmount(String Type, int Count)
        {
            this.Tag = Type;
            this.Count = Count;
        }

        public ResourceTagAmount()
        {
            
        }
    }
}