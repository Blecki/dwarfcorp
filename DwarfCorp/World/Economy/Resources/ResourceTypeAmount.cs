using System;
using System.Diagnostics;

namespace DwarfCorp
{
    public class ResourceTypeAmount
    {
        public String Type { get; set; }
        public int Count { get; set; }
        
        public ResourceTypeAmount(String Type, int Count)
        {
            this.Type = Type;
            this.Count = Count;
        }

        public ResourceTypeAmount()
        {
            
        }
    }

    public class ResourceApparentTypeAmount
    {
        public String Type { get; set; }
        public int Count { get; set; }

        public ResourceApparentTypeAmount(String Type, int Count)
        {
            this.Type = Type;
            this.Count = Count;
        }

        public ResourceApparentTypeAmount()
        {

        }
    }
}