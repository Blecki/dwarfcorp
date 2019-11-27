using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    public class DwarfAnimationProxy : Animation
    {
        private LayerStack Owner = null;

        public DwarfAnimationProxy(LayerStack Owner)
        {
            this.Owner = Owner;
        }

        public override Texture2D GetTexture()
        {
            return Owner.GetCompositeTexture();
        }

        public override void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            // Obviously shouldn't be hard coded.
            var composite = Owner.GetCompositeTexture();
            if (composite == null) return;

            SpriteSheet = new SpriteSheet(composite, 48, 40);
            base.UpdatePrimitive(Primitive, CurrentFrame);
        }

        public override bool CanUseInstancing { get => false; }
    }

}
