using System;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ZoneFactoryAttribute : Attribute
    {
        public String Name;

        public ZoneFactoryAttribute(String Name)
        {
            this.Name = Name;
        }
    }
}
