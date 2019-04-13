using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TextureGeneratorAttribute : Attribute
    {
        public string GeneratorName;

        public TextureGeneratorAttribute(String GeneratorName)
        {
            this.GeneratorName = GeneratorName;
        }
    }
}
