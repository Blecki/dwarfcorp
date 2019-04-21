using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MinimapIcon : GameComponent
    {
        public NamedImageFrame Icon;
        public float IconScale;

        public MinimapIcon()
        {
        }

        public MinimapIcon(ComponentManager Manager, NamedImageFrame icon) :
            base(Manager, "Icon", Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            UpdateRate = 10;
            Icon = icon;
            IconScale = 1.0f;
            IsVisible = false;
        }
    }
}

