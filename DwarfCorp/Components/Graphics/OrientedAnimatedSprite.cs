using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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

        protected string CurrentAnimationName = "";

        public override void SetCurrentAnimation(String Name, bool Play = false)
        {
            CurrentAnimationName = Name;
            var s = CurrentAnimationName + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            base.SetCurrentAnimation(s, Play);
        }

        public OrientedAnimatedSprite()
        {
            
        }

        public OrientedAnimatedSprite(ComponentManager manager, string name, Matrix localTransform) :
                base(manager, name, localTransform)
        {
        }

        override public void Render(
            DwarfTime gameTime, 
            ChunkManager chunks, 
            Camera camera, 
            SpriteBatch spriteBatch, 
            GraphicsDevice graphicsDevice, 
            Shader effect, 
            bool renderingForWater)
        {
            CurrentOrientation = SpriteOrientationHelper.CalculateSpriteOrientation(camera, GlobalTransform);

            var s = CurrentAnimationName + SpriteOrientationHelper.OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
            {
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);
                AnimPlayer.Update(gameTime);
            }

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }
    }
}
