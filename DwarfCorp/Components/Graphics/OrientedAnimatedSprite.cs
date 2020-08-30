using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    /// <summary>
    /// This is a special kind of billboard which has different animations for facing in
    /// four different directions. The correct direction is chosen based on the position
    /// of the camera.
    /// </summary>
    public class OrientedAnimatedSprite : AnimatedSprite
    {
        

        public SpriteOrientation CurrentOrientation { get; set; }

        protected string currentMode = "";

        public override void SetCurrentAnimation(string name, bool Play = false)
        {
            //if (currentMode != name || Play)
            //{
                currentMode = name;
                var s = currentMode + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
                SetCurrentAnimation(Animations[s], Play);
            //        AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.NoStateChange);
            //}
        }


        public OrientedAnimatedSprite()
        {
            
        }

        public OrientedAnimatedSprite(ComponentManager manager, string name, Matrix localTransform) :
                base(manager, name, localTransform)
        {
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            CurrentOrientation = SpriteOrientationHelper.CalculateSpriteOrientation(camera, GlobalTransform);

            var s = currentMode + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
            {
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);
                AnimPlayer.Update(gameTime, true);
            }
        }
    }
}
