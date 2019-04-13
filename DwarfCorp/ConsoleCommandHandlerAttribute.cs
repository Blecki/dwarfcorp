using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class ConsoleCommandHandlerAttribute : Attribute
    {
        public string Name;

        public ConsoleCommandHandlerAttribute(String Name)
        {
            this.Name = Name;
        }
    }
}
