using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public class VoxelEventHookAttribute : Attribute
    {
        public String Name;

        public VoxelEventHookAttribute(String Name)
        {
            this.Name = Name;
        }
    }

}
