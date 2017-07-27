using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public class TileReference
    {
        protected bool Equals(TileReference other)
        {
            return string.Equals(Sheet, other.Sheet) && Tile == other.Tile;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Sheet != null ? Sheet.GetHashCode() : 0)*397) ^ Tile;
            }
        }

        public String Sheet;
        public int Tile;
 
        public TileReference(String Sheet, int Tile)
        {
            this.Sheet = Sheet;
            this.Tile = Tile;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TileReference))
            {
                return false;
            }
            var objAsTile = obj as TileReference;
            return Equals(objAsTile);
        }
    }
}