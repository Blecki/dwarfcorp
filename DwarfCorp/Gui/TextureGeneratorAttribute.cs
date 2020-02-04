using System;

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
