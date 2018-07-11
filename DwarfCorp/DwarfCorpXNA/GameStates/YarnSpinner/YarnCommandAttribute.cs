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
        public List<String> ArgumentTypes;

        public enum ArgumentTypeBehaviors
        {
            Unchecked,
            Strict,
            LastIsVaridic
        }

        public ArgumentTypeBehaviors ArgumentTypeBehavior = ArgumentTypeBehaviors.Strict;

        public YarnCommandAttribute(String CommandName, params String[] ArgumentTypes)
        {
            this.CommandName = CommandName;
            this.ArgumentTypes = new List<string>(ArgumentTypes);
        }
    }
}
