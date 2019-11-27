using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.LayeredSprites
{
    public enum PaletteType
    {
        Hair,
        Skin
    }

    public class Palette
    {
        [JsonIgnore]
        public DwarfCorp.Palette CachedPalette = null;

        public String Asset;
        public PaletteType Layer = PaletteType.Hair;
    }
}
