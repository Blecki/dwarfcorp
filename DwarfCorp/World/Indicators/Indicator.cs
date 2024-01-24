using Microsoft.Xna.Framework;

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
}
