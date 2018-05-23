using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Triggers
{
    public class BuiltRoom : Trigger
    {
        public String RoomType;

        public BuiltRoom(String Type)
        {
            RoomType = Type;
        }
    }
}
