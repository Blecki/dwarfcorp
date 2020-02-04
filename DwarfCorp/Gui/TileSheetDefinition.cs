using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp.Gui
{
    public enum TileSheetType
    {
        VariableWidthFont,
        TileSheet,
        Generated,
        JsonFont
    }

    public class TileSheetDefinition
    {
        public String Name;
        public String Texture;
        public int TileWidth;
        public int TileHeight;
        public TileSheetType Type = TileSheetType.TileSheet;
        public bool RepeatWhenUsedAsBorder = false;
    }
}
