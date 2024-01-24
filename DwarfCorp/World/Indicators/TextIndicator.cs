using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
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
}
