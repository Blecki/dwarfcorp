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
        public GeometricPrimitive Model;

        public String PrimitiveName;
        public bool EnableWind = false;
        public bool RenderInSelectionBuffer = true;
        public bool EnableGhostClipping = true;
    }
}
