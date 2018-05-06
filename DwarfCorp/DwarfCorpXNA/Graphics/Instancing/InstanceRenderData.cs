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
        public GeometricPrimitive Model;
        public BlendState BlendMode;
        public bool EnableWind = false;
        public bool RenderInSelectionBuffer = true;
        public bool EnableGhostClipping = true;
    }
}
