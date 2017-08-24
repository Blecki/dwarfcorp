using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class InstanceRenderData
    {
        [JsonIgnore]
        public GeometricPrimitive Model { get; set; }
        public Texture2D Texture { get; set; }
        public BlendState BlendMode { get; set; }
        public bool EnableWind = false;
        public bool RenderInSelectionBuffer = true;
    }
}
