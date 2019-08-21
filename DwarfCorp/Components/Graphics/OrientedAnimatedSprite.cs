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
        public enum Orientation
        {
            Right = 0,
            Left = 1,
            Forward = 2,
            Backward = 3
        }

        protected static string[] OrientationStrings =
        {
            "RIGHT",
            "LEFT",
            "FORWARD",
            "BACKWARD"
        };

        public Orientation CurrentOrientation { get; set; }

        protected string currentMode = "";

        public override void SetCurrentAnimation(string name, bool Play = false)
        {
            //if (currentMode != name || Play)
            //{
                currentMode = name;
                var s = currentMode + OrientationStrings[(int)CurrentOrientation];
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
            CalculateCurrentOrientation(camera);


            var s = currentMode + OrientationStrings[(int)CurrentOrientation];
            if (Animations.ContainsKey(s))
            {
                AnimPlayer.ChangeAnimation(Animations[s], AnimationPlayer.ChangeAnimationOptions.Play);
                AnimPlayer.Update(gameTime, true);
            }
        }

        public void CalculateCurrentOrientation(Camera camera)
        {
            float xComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Left);
            float yComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Forward);

            // Todo: There should be a way to do this without trig.
            float angle = (float) Math.Atan2(yComponent, xComponent);

            if (angle > 3.0f * MathHelper.PiOver4) // 135 degrees
                CurrentOrientation = Orientation.Right;
            else if (angle > MathHelper.PiOver4) // 45 degrees
                CurrentOrientation = Orientation.Backward;
            else if (angle > -MathHelper.PiOver4) // -45 degrees
                CurrentOrientation = Orientation.Left;
            else if (angle > -3.0f * MathHelper.PiOver4) // -135 degrees
                CurrentOrientation = Orientation.Forward;
            else
                CurrentOrientation = Orientation.Right;
        }
    }

}
