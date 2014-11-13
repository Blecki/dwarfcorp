using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class MinimapIcon : Body
    {
        public ImageFrame Icon { get; set; }
        public float IconScale { get; set; }
        public MinimapIcon()
        {
            
        }

        public MinimapIcon(Body parent, ImageFrame icon) : 
            base("Icon", parent, Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            Icon = icon;
            IconScale = 1.0f;
            AddToOctree = false;
            FrustrumCull = false;
            IsVisible = false;
        }
    }
}

