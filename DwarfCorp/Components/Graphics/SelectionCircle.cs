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

        public SelectionCircle(ComponentManager manager, GameComponent Creature) :
            base(manager, "Selection", Matrix.CreateRotationX((float)Math.PI), Vector3.One, Vector3.Zero)
        {
            FitToCreature(Creature);
            CreateCosmeticChildren(manager);
            SetFlagRecursive(Flag.Visible, false);
        }

        public void FitToCreature(GameComponent Parent)
        {
            var shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            var bbox = (Parent as GameComponent).GetBoundingBox();
            shadowTransform.Translation = new Vector3(0.0f, (Parent.BoundingBoxSize.Y * -0.5f) - Parent.LocalBoundingBoxOffset.Y, 0.0f);
            float scale = Parent.BoundingBoxSize.X * 2;
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