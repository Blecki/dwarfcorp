using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DecalType
    {
        public byte ID;
        public String Name;
        public Point Tile;
        public Color MinimapColor = Color.White;
    }
}
