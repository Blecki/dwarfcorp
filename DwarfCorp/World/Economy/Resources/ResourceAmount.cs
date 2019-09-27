using System;
using System.Diagnostics;

namespace DwarfCorp
{
    public class ResourceAmount
    {
        public String Type { get; set; }
        public int Count { get; set; }
        
        public ResourceAmount(String Type, int Count)
        {
            this.Type = Type;
            this.Count = Count;
        }

        public ResourceAmount()
        {
            
        }

        public ResourceAmount CloneResource()
        {
            return new ResourceAmount(Type, Count);
        }
    }
}