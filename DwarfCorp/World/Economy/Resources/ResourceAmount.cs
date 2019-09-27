using System;
using System.Diagnostics;

namespace DwarfCorp
{
    // Todo: Hate the quantity class. Also, Clean!
    public class ResourceAmount : Quantitiy<String>
    {
        public ResourceAmount(ResourceAmount amount)
        {
            Type = amount.Type;
            Count = amount.Count;
        }

        public ResourceAmount(String type, int num)
        {
            Type = type;
            Count = num;
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