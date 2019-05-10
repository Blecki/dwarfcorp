using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SelectionCircle : GameComponent
    {
        public SelectionCircle()
            : base()
        {
        }

        public SelectionCircle(ComponentManager manager) :
            base(manager, "Selection", Matrix.CreateRotationX((float)Math.PI), Vector3.One, Vector3.Zero)
        {
            var shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);

            LocalTransform = shadowTransform;
            CreateCosmeticChildren(manager);
            SetFlagRecursive(Flag.Visible, false);
        }

        public void FitToParent()
        {
            var shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            var bbox = (Parent as GameComponent).GetBoundingBox();
            shadowTransform.Translation = new Vector3(0.0f, -0.5f * (bbox.Max.Y - bbox.Min.Y), 0.0f);
            float scale = bbox.Max.X - bbox.Min.X;
            shadowTransform = shadowTransform * Matrix.CreateScale(scale);
            LocalTransform = shadowTransform;
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            AddChild(new SimpleSprite(Manager, "Sprite", Matrix.Identity, new SpriteSheet(ContentPaths.Effects.selection_circle), Point.Zero)
            {
                LightsWithVoxels = false,
                OrientationType = SimpleSprite.OrientMode.Fixed
            }
            ).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(Manager);
        }
    }
}