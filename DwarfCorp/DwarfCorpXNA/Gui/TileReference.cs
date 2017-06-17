using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public class TileReference
    {
        public String Sheet;
        public int Tile;
 
        public TileReference(String Sheet, int Tile)
        {
            this.Sheet = Sheet;
            this.Tile = Tile;
        }
    }
}