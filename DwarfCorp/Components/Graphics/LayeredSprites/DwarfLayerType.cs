using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    public class LayerType
    {
        public String Name;
        public int Precedence;
        public bool Fundamental = true;
        public String PaletteType = "Skin";
        public bool Gendered = false;
    }
}