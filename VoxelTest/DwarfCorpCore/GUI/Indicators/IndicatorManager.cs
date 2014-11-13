using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{

    public class Indicator
    {
        public ImageFrame Image;
        public Vector3 Position;
        public Timer CurrentTime;
        public float MaxScale;
        public Vector2 Offset { get; set; }
        public Color Tint { get; set; }
        public bool Grow = true;
        public bool Flip = false;

        public virtual void Update(GameTime time)
        {
            CurrentTime.Update(time);
        }
    }

    public class AnimatedIndicator : Indicator
    {
        public Animation Animation;


        public override void Update(GameTime time)
        {
            base.Update(time);
            Animation.Update(time);

            Image = new ImageFrame(Animation.SpriteSheet, Animation.GetCurrentFrameRect());
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

        public static void Update(GameTime time)
        {
            lock(IndicatorLock)
            {
                List<Indicator> removals = new List<Indicator>();
                foreach(Indicator indicator in Indicators)
                {
                    indicator.Update(time);

                    float scale = 1.0f;
                    float growTime = indicator.CurrentTime.TargetTimeSeconds * 0.5f;
                    float shrinkTime = indicator.CurrentTime.TargetTimeSeconds * 0.5f;

                    if(indicator.CurrentTime.CurrentTimeSeconds < growTime)
                    {
                         scale = Easing.CubeInOut(indicator.CurrentTime.CurrentTimeSeconds, 0.0f, indicator.MaxScale, growTime);
                    }
                    else if(indicator.CurrentTime.CurrentTimeSeconds > shrinkTime)
                    {
                        scale = Easing.CubeInOut(indicator.CurrentTime.CurrentTimeSeconds - shrinkTime, indicator.MaxScale, -indicator.MaxScale, indicator.CurrentTime.TargetTimeSeconds - shrinkTime);
                    }

                    if (!indicator.Grow)
                    {
                        scale = indicator.MaxScale;
                    }

                    Drawer2D.DrawSprite(indicator.Image, indicator.Position, new Vector2(scale, scale), indicator.Offset, indicator.Tint, indicator.Flip);

                    if(indicator.CurrentTime.HasTriggered)
                    {
                        removals.Add(indicator);
                    }
                }

                foreach(Indicator indicator in removals)
                {
                    Indicators.Remove(indicator);
                }
            }
        }
    }
}
