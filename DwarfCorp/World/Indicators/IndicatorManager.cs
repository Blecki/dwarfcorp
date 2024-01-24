using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
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

        public static Dictionary<StandardIndicators, NamedImageFrame> StandardFrames = new Dictionary<StandardIndicators, NamedImageFrame>(); 

        public static List<Indicator> Indicators = new List<Indicator>();
        private static readonly Object IndicatorLock = new object();

        public static SpriteFont DefaultFont { get; set; }

        public static void SetupStandards()
        {
            StandardFrames[StandardIndicators.Happy] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 4, 0);
            StandardFrames[StandardIndicators.Hungry] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 0, 0);
            StandardFrames[StandardIndicators.Sad] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 5, 0);
            StandardFrames[StandardIndicators.Sleepy] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 3, 0);
            StandardFrames[StandardIndicators.Question] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 1, 1);
            StandardFrames[StandardIndicators.Exclaim] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 0, 1);
            StandardFrames[StandardIndicators.Heart] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 4, 1);
            StandardFrames[StandardIndicators.Boom] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 2, 1);
            StandardFrames[StandardIndicators.Dots] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 3, 1);
            StandardFrames[StandardIndicators.DownArrow] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 0, 2);
            StandardFrames[StandardIndicators.UpArrow] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 1, 2);
            StandardFrames[StandardIndicators.LeftArrow] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 2, 2);
            StandardFrames[StandardIndicators.RightArrow] = new NamedImageFrame(ContentPaths.GUI.indicators, 16, 3, 2);
        }


        public static void DrawIndicator(StandardIndicators indicator, Vector3 position, float time, float scale, Vector2 offset)
        {
            DrawIndicator(StandardFrames[indicator], position, time, scale, offset, Color.White);
        }


        public static void DrawIndicator(StandardIndicators indicator, Vector3 position, float time, float scale, Vector2 offset, Color tint)
        {
            DrawIndicator(StandardFrames[indicator], position, time,  scale, offset, tint);
        }

        public static void DrawIndicator(NamedImageFrame image, Vector3 position, float time, float scale, Vector2 offset)
        {
            DrawIndicator(image, position, time, scale, offset, Color.White);
        }


        public static void DrawIndicator(NamedImageFrame image, Vector3 position, float time, float scale, Vector2 offset, Color tint)
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

        public static void DrawIndicator(SpriteSheet Sheet, Animation image, Vector3 position, float time, float scale, Vector2 offset, Color tint, bool flip)
        {
            lock (IndicatorLock)
            {
                Indicators.Add(new AnimatedIndicator
                {
                    CurrentTime = new Timer(time, true),
                    Image = null,
                    Animation = image,
                    SpriteSheet = Sheet,
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

        public static void DrawIndicator(string indicator, Vector3 position, float time, Color color)
        {
            lock (IndicatorLock)
            {
                Indicators.Add(new TextIndicator(DefaultFont)
                {
                    Text = indicator,
                    Tint = color,
                    CurrentTime = new Timer(time, true, Timer.TimerMode.Real),
                    Image = null,
                    Position = position,
                    Grow = false,
                });
            }
        }
    }
}
