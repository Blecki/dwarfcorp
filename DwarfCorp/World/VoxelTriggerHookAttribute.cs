using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public class VoxelTriggerHookAttribute : Attribute
    {
        public String Name;

        public VoxelTriggerHookAttribute(String Name)
        {
            this.Name = Name;
        }
    }

}
