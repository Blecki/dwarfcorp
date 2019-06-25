using System;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RoomFactoryAttribute : Attribute
    {
        public String Name;

        public RoomFactoryAttribute(String Name)
        {
            this.Name = Name;
        }
    }
}
