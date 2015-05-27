using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component projects a billboard shadow to the ground below an entity.
    /// </summary>
    public class SelectionCircle : Sprite
    {
        public SelectionCircle()
            : base()
        {

        }

        public SelectionCircle(ComponentManager manager, Body parent) :
            base(manager, "Selection", parent, Matrix.CreateRotationX((float)Math.PI), new SpriteSheet(ContentPaths.Effects.selection_circle), false)
        {
            LightsWithVoxels = false;
            AddToCollisionManager = false;
            OrientationType = OrientMode.Fixed;
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Effects.selection_circle), "sh", 32, 32, shP, false, Color.White, 1, 1.1f, 1.1f, false);
            AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            base.SetCurrentAnimation("sh");

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);

            LocalTransform = shadowTransform;

        }

      
    }

}