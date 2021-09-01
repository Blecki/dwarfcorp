using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public class VoxelUpdateHookAttribute : Attribute
    {
        public String Name;

        public VoxelUpdateHookAttribute(String Name)
        {
            this.Name = Name;
        }
    }

}
