using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DwarfCorp.DwarfSprites
{
    public class Layer
    {
        [JsonIgnore] public IndexedTexture CachedTexture = null;

        public String Type;
        public List<String> Names = new List<string>();
    }
}
