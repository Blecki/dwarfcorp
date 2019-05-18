using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Palette : List<Color>
    {
        public Palette()
        {

        }

        public Palette(IEnumerable<Color> Data) : base(Data)
        {

        }
    }
}