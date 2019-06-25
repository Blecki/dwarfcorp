using System;

namespace DwarfCorp
{
    [Flags]
    public enum CollisionType
    {
        None = 1,
        Static = 2,
        Dynamic = 4,
        Both = Static | Dynamic
    }
}