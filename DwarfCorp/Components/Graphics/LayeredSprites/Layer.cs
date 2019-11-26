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
    public class Layer
    {
        [JsonIgnore] public IndexedTexture CachedTexture = null;

        public LayerType Type;
        public List<String> Names = new List<string>();
    }
}
