using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method)]
    public class YarnCommandAttribute : Attribute
    {
        public string CommandName;

        public YarnCommandAttribute(String CommandName)
        {
            this.CommandName = CommandName;
        }
    }
}
