// OrientedAnimation.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
    public class OrientedAnimation : Sprite, IUpdateableComponent
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

        //public delegate void FrameEvent(Animation animation, Orientation orientation, int frame);

        //public event FrameEvent OnFrame;

        //protected virtual void InvokeOnFrame(Animation animation, Orientation orientation, int frame)
        //{
        //    FrameEvent handler = OnFrame;
        //    if (handler != null) handler(animation, orientation, frame);
        //}

        public Orientation CurrentOrientation { get; set; }

        protected string currentMode = "";

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
            Matrix localTransform) :
                base(manager, name, localTransform, null, false)
        {
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            CalculateCurrentOrientation(camera);

            //foreach (string orient in OrientationStrings)
            //{
            //    string animationName = currentMode + orient;

            //    if (!Animations.ContainsKey(animationName)) continue;

            //    Animation animation = Animations[animationName];
            //    animation.Update(gameTime);
            //}

            string s = currentMode + OrientationStrings[(int) CurrentOrientation];
            if(Animations.ContainsKey(s))
            {
                var previousAnimation = CurrentAnimation;
                CurrentAnimation = Animations[s];
                if (previousAnimation != null && previousAnimation.Name.StartsWith(currentMode))
                    CurrentAnimation.Sychronize(previousAnimation);
                SpriteSheet = CurrentAnimation.SpriteSheet;
            }

            base.Update(gameTime, chunks, camera);

            //if (CurrentAnimation != null && CurrentAnimation.LastFrame != CurrentAnimation.CurrentFrame)
            //{
            //    InvokeOnFrame(CurrentAnimation, CurrentOrientation, CurrentAnimation.CurrentFrame);
            //}
        }

        public void CalculateCurrentOrientation(Camera camera)
        {
            float xComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Left);
            float yComponent = Vector3.Dot(camera.ViewMatrix.Forward, GlobalTransform.Forward);

            // Todo: There should be a way to do this without trig.
            float angle = (float) Math.Atan2(yComponent, xComponent);

            if (angle > 3.0f * MathHelper.PiOver4)
                CurrentOrientation = Orientation.Right;
            else if (angle > MathHelper.PiOver4)
                CurrentOrientation = Orientation.Backward;
            else if (angle > -MathHelper.PiOver4)
                CurrentOrientation = Orientation.Left;
            else if (angle > -3.0f * MathHelper.PiOver4)
                CurrentOrientation = Orientation.Forward;
            else
                CurrentOrientation = Orientation.Right;
        }
    }

}
