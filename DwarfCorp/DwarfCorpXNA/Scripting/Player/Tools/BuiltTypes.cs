using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public enum BuildTypes
    {
        None = 0,
        Room = 1,
        Wall = 2,
        Item = 4,
        Craft = 8,
        Cook = 16,
        AllButCook = Room | Wall | Item | Craft
    }
}
