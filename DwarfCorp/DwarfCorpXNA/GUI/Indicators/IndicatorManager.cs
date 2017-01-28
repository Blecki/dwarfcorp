// IndicatorManager.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{

    public class Indicator
    {
        public enum IndicatorMode
        {
            Indicator2D,
            Indicator3D
        }
        public ImageFrame Image;
        public Vector3 Position;
        public Timer CurrentTime;
        public float MaxScale;
        public Vector2 Offset { get; set; }
        public Color Tint { get; set; }
        public bool Grow = true;
        public bool Flip = false;
        public float Scale { get; set; }
        public IndicatorMode Mode { get; set; }

        public bool ShouldDelete { get; set; }

        public Indicator()
        {
            ShouldDelete = false;
            Mode = IndicatorMode.Indicator3D;
        }

        public virtual void Update(DwarfTime time)
        {
            float growTime = CurrentTime.TargetTimeSeconds * 0.5f;
            float shrinkTime = CurrentTime.TargetTimeSeconds * 0.5f;

            if (CurrentTime.CurrentTimeSeconds < growTime)
            {
                Scale = Easing.CubeInOut(CurrentTime.CurrentTimeSeconds, 0.0f, MaxScale, growTime);
            }
            else if (CurrentTime.CurrentTimeSeconds > shrinkTime)
            {
                Scale = Easing.CubeInOut(CurrentTime.CurrentTimeSeconds - shrinkTime, MaxScale, -MaxScale, CurrentTime.TargetTimeSeconds - shrinkTime);
            }

            if (!Grow)
            {
                Scale = MaxScale;
            }
            CurrentTime.Update(time);
        }

        public virtual void Render()
        {
            switch (Mode)
            {
                case IndicatorMode.Indicator3D:
                    Drawer2D.DrawSprite(Image, Position, new Vector2(Scale, Scale), Offset, Tint, Flip);
                    break;
                case IndicatorMode.Indicator2D:
                   DwarfGame.SpriteBatch.Draw(Image.Image, new Vector2(Position.X, Position.Y), Image.SourceRect, Tint, 0.0f, Offset, new Vector2(Scale, Scale), Flip ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0.0f);
                    break;
            }
            
        }
    }

    public class TextIndicator : Indicator
    {
        public string Text { get; set; }

        public override void Update(DwarfTime time)
        {
            switch (Mode)
            {
                    case IndicatorMode.Indicator3D:
                        Position += Vector3.Up * (float)time.ElapsedGameTime.TotalSeconds;
                         break;
                    case IndicatorMode.Indicator2D:
                        Position += Vector3.Up * (float)time.ElapsedGameTime.TotalSeconds * 50;
                         break;
            }
           
            Tint = new Color(Tint.R, Tint.G, Tint.B, (byte)(255*(1.0f - CurrentTime.CurrentTimeSeconds/CurrentTime.TargetTimeSeconds)));
            CurrentTime.Update(time);
        }

        public override void Render()
        {
            switch (Mode)
            {
                case IndicatorMode.Indicator2D:
                    Drawer2D.DrawAlignedText(DwarfGame.SpriteBatch, Text, WorldManager.GUI.DefaultFont, Tint, Drawer2D.Alignment.Center,  new Rectangle((int)Position.X, (int)Position.Y, 32, 32));
                    break;
                case IndicatorMode.Indicator3D:
                    Drawer2D.DrawText(Text, Position, Tint, Color.Transparent);
                    break;
            }
        }
       
    }

    public class AnimatedIndicator : Indicator
    {
        public Animation Animation;


        public override void Update(DwarfTime time)
        {
            base.Update(time);
            Animation.Update(time);

            Image = new ImageFrame(Animation.SpriteSheet.GetTexture(), Animation.GetCurrentFrameRect());

            if (Animation.IsDone())
            {
                ShouldDelete = true;
            }
        }

       
    }
    /// <summary>
    /// This class exists to draw simple sprites (indicators) to the screen. Indicators
    /// are just a sprite at a location which grows, shrinks, and disappears over time.
    /// </summary>
    public static class IndicatorManager
    {

        public enum StandardIndicators
        {
            Happy,
            Sad,
            Hungry,
            Sleepy,
            Question,
            Exclaim,
            Boom,
            Heart,
            DownArrow,
            UpArrow,
            LeftArrow,
            RightArrow,
            Dots
        }

        public static Dictionary<StandardIndicators, ImageFrame> StandardFrames = new Dictionary<StandardIndicators, ImageFrame>(); 

        public static List<Indicator> Indicators = new List<Indicator>();
        private static readonly Object IndicatorLock = new object();

        public static void SetupStandards()
        {
            Texture2D indicators = TextureManager.GetTexture(ContentPaths.GUI.indicators);
            StandardFrames[StandardIndicators.Happy] = new ImageFrame(indicators, 16, 4, 0);
            StandardFrames[StandardIndicators.Hungry] = new ImageFrame(indicators, 16, 0, 0);
            StandardFrames[StandardIndicators.Sad] = new ImageFrame(indicators, 16, 5, 0);
            StandardFrames[StandardIndicators.Sleepy] = new ImageFrame(indicators, 16, 3, 0);
            StandardFrames[StandardIndicators.Question] = new ImageFrame(indicators, 16, 1, 1);
            StandardFrames[StandardIndicators.Exclaim] = new ImageFrame(indicators, 16, 0, 1);
            StandardFrames[StandardIndicators.Heart] = new ImageFrame(indicators, 16, 4, 1);
            StandardFrames[StandardIndicators.Boom] = new ImageFrame(indicators, 16, 2, 1);
            StandardFrames[StandardIndicators.Dots] = new ImageFrame(indicators, 16, 3, 1);
            StandardFrames[StandardIndicators.DownArrow] = new ImageFrame(indicators, 16, 0, 2);
            StandardFrames[StandardIndicators.UpArrow] = new ImageFrame(indicators, 16, 1, 2);
            StandardFrames[StandardIndicators.LeftArrow] = new ImageFrame(indicators, 16, 2, 2);
            StandardFrames[StandardIndicators.RightArrow] = new ImageFrame(indicators, 16, 3, 2);
        }


        public static void DrawIndicator(StandardIndicators indicator, Vector3 position, float time, float scale, Vector2 offset)
        {
            DrawIndicator(StandardFrames[indicator], position, time, scale, offset, Color.White);
        }


        public static void DrawIndicator(StandardIndicators indicator, Vector3 position, float time, float scale, Vector2 offset, Color tint)
        {
            DrawIndicator(StandardFrames[indicator], position, time,  scale, offset, tint);
        }

        public static void DrawIndicator(ImageFrame image, Vector3 position, float time, float scale, Vector2 offset)
        {
            DrawIndicator(image, position, time, scale, offset, Color.White);
        }


        public static void DrawIndicator(ImageFrame image, Vector3 position, float time, float scale, Vector2 offset, Color tint)
        {
            lock(IndicatorLock)
            {
                Indicators.Add(new Indicator
                {
                    CurrentTime = new Timer(time, true),
                    Image = image,
                    Position = position,
                    MaxScale = scale,
                    Offset = offset,
                    Tint = tint
                });
            }
        }

        public static void DrawIndicator(Animation image, Vector3 position, float time, float scale, Vector2 offset, Color tint, bool flip)
        {
            lock (IndicatorLock)
            {
                Indicators.Add(new AnimatedIndicator
                {
                    CurrentTime = new Timer(time, true),
                    Image = null,
                    Animation = image,
                    Position = position,
                    MaxScale = scale,
                    Offset = offset,
                    Tint = tint,
                    Grow = false,
                    Flip = flip
                });
            }
        }

        public static void Render(DwarfTime time)
        {
            lock (IndicatorLock)
            {
                foreach (Indicator indicator in Indicators)
                {
                    indicator.Render();
                }
            }
        }

        public static void Update(DwarfTime time)
        {
            lock(IndicatorLock)
            {
                Indicators.RemoveAll(indicator => indicator.CurrentTime.HasTriggered || indicator.ShouldDelete);
                foreach (Indicator indicator in Indicators)
                {
                    indicator.Update(time);
                }
            }
        }

        public static void DrawIndicator(string indicator, Vector3 position, float time, Color color, Indicator.IndicatorMode mode = Indicator.IndicatorMode.Indicator3D)
        {
            lock (IndicatorLock)
            {
                Indicators.Add(new TextIndicator()
                {
                    Text = indicator,
                    Tint = color,
                    CurrentTime = new Timer(time, true),
                    Image = null,
                    Position = position,
                    Grow = false,
                    Mode = mode
                });
            }
        }
    }
}
