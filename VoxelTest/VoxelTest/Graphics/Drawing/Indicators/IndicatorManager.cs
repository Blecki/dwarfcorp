using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This class exists to draw simple sprites (indicators) to the screen. Indicators
    /// are just a sprite at a location which grows, shrinks, and disappears over time.
    /// </summary>
    public static class IndicatorManager
    {
        public class Indicator
        {
            public ImageFrame Image;
            public Vector3 Position;
            public Timer CurrentTime;
            public float MaxScale;
            public Vector2 Offset { get; set; }
        }

        public enum StandardIndicators
        {
            Happy,
            Sad,
            Hungry,
            Sleepy,
            Question,
            Exclaim,
            Boom,
            Heart
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
        }

        public static void DrawIndicator(StandardIndicators indicator, Vector3 position, float time, float scale, Vector2 offset)
        {
            DrawIndicator(StandardFrames[indicator], position, time,  scale, offset);
        }

        public static void DrawIndicator(ImageFrame image, Vector3 position, float time, float scale, Vector2 offset)
        {
            lock(IndicatorLock)
            {
                Indicators.Add(new Indicator
                {
                    CurrentTime = new Timer(time, true),
                    Image = image,
                    Position = position,
                    MaxScale = scale,
                    Offset = offset
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
                    indicator.CurrentTime.Update(time);

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
                    Drawer2D.DrawSprite(indicator.Image, indicator.Position, new Vector2(scale, scale), indicator.Offset);

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
