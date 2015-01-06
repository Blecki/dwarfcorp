using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is a special kind of billboard which has different animations for facing in
    /// four different directions. The correct direction is chosen based on the position
    /// of the camera.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class OrientedAnimation : Sprite
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

        private string currentMode = "";

        public override void SetCurrentAnimation(string name)
        {
            currentMode = name;
        }

        public void AddAnimation(string mode, Animation rightAnimation, Animation leftAnimation, Animation forwardAnimation, Animation backwardAnimation)
        {
            Animations[mode + OrientationStrings[(int) Orientation.Right]] = rightAnimation;
            Animations[mode + OrientationStrings[(int) Orientation.Left]] = leftAnimation;
            Animations[mode + OrientationStrings[(int) Orientation.Forward]] = forwardAnimation;
            Animations[mode + OrientationStrings[(int) Orientation.Backward]] = backwardAnimation;

            if(currentMode == "")
            {
                currentMode = mode;
            }
        }

        public OrientedAnimation()
        {
            
        }

        public OrientedAnimation(ComponentManager manager, string name,
            GameComponent parent, Matrix localTransform) :
                base(manager, name, parent, localTransform, null, false)
        {
        }

        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            CalculateCurrentOrientation(camera);

            string s = currentMode + OrientationStrings[(int) CurrentOrientation];
            if(Animations.ContainsKey(s))
            {
                CurrentAnimation = Animations[s];
                CurrentAnimation.Play();

                SpriteSheet = CurrentAnimation.SpriteSheet;
            }

            base.Update(DwarfTime, chunks, camera);
        }

        public void CalculateCurrentOrientation(Camera camera)
        {
            float xComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Left);
            float yComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Forward);

            float angle = (float) Math.Atan2(yComponent, xComponent);


            if(angle > -MathHelper.PiOver4 && angle < MathHelper.PiOver4)
            {
                CurrentOrientation = Orientation.Left;
            }
            else if(angle > MathHelper.PiOver4 && angle < 3.0f * MathHelper.PiOver4)
            {
                CurrentOrientation = Orientation.Backward;
            }
            else if((angle > 3.0f * MathHelper.PiOver4 || angle < -3.0f * MathHelper.PiOver4))
            {
                CurrentOrientation = Orientation.Right;
            }
            else
            {
                CurrentOrientation = Orientation.Forward;
            }
        }
    }

}