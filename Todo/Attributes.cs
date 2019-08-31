using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class DefaultSwitchAttribute : System.Attribute
    {
        public int Order = 0;

        public DefaultSwitchAttribute(int Order = 0)
        {
            this.Order = Order;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class UnknownSwitchAttribute : System.Attribute
    {

    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class GreedyArgumentAttribute : System.Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class SwitchDocumentationAttribute: System.Attribute
    {
        public String Documentation = "";
        public SwitchDocumentationAttribute(String Documentation)
        {
            this.Documentation = Documentation;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class CommandAttribute : System.Attribute
    {
        public string Name;
        public string ShortDescription = "";
        public string LongHelpText = "Long help not specified for this command.";
        public string ErrorText = "";
        public List<String> Synonyms = new List<string>();

        public CommandAttribute(String Name, String ShortDescription = "", String LongHelpText = "Long help not specified for this command.", String ErrorText = "", String Synonyms = "")
        {
            this.Name = Name;
            this.ShortDescription = ShortDescription;
            this.LongHelpText = LongHelpText;
            this.ErrorText = ErrorText;
            this.Synonyms.AddRange(Synonyms.Split(' '));
        }
    }
}