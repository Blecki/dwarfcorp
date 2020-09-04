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
using System.Runtime.Serialization;

namespace DwarfCorp
{

    public class Indicator
    {
        public NamedImageFrame Image;
        public Vector3 Position;
        public Timer CurrentTime;
        public float MaxScale;
        public Vector2 Offset { get; set; }
        public Color Tint { get; set; }
        public bool Grow = true;
        public bool Flip = false;
        public float Scale { get; set; }

        public bool ShouldDelete { get; set; }

        public Indicator()
        {
            ShouldDelete = false;
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
            Drawer2D.DrawSprite(Image, Position, new Vector2(Scale, Scale), Offset, Tint, Flip);
        }
    }

    public class TextIndicator : Indicator
    {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public float Speed { get; set; }

        public TextIndicator(SpriteFont font)
        {
            Font = font;
            Speed = MathFunctions.Rand(0.45f, 2.45f);
        }

        public override void Update(DwarfTime time)
        {
            Position += Speed * Vector3.Up * (float)time.ElapsedGameTime.TotalSeconds;
            Tint = new Color(Tint.R, Tint.G, Tint.B, (byte)(255*(1.0f - CurrentTime.CurrentTimeSeconds/CurrentTime.TargetTimeSeconds)));
            CurrentTime.Update(time);
        }

        public override void Render()
        {
            Drawer2D.DrawText(Text, Position, Tint, Color.Transparent);
        }
       
    }

    public class AnimatedIndicator : Indicator
    {
        public AnimationPlayer Player = new AnimationPlayer();
        public Animation Animation;
        public SpriteSheet SpriteSheet;
        
        public override void Update(DwarfTime time)
        {
            base.Update(time);

            if (!Player.HasValidAnimation()) Player.Play(Animation);
            Player.Update(time, false);

            var frame = Animation.Frames[Player.CurrentFrame];
            var frameRect = new Rectangle(frame.X * SpriteSheet.FrameWidth, frame.Y * SpriteSheet.FrameHeight, SpriteSheet.FrameWidth, SpriteSheet.FrameHeight);
            Image = new NamedImageFrame(SpriteSheet.AssetName, frameRect);

            if (Player.IsDone())
                ShouldDelete = true;
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
