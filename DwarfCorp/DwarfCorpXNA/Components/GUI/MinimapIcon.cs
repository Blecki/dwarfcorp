using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MinimapIcon : Body
    {
        public NamedImageFrame Icon;
        public float IconScale;

        public MinimapIcon()
        {

        }

        public MinimapIcon(ComponentManager Manager, NamedImageFrame icon) :
            base(Manager, "Icon", Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            Icon = icon;
            IconScale = 1.0f;
            IsVisible = false;
        }

        public override void Delete()
        {
            base.Delete();
        }

        public override void Die()
        {
            base.Die();
        }
    }
}

