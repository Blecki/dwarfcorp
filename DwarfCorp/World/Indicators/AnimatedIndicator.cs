using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    public class AnimatedIndicator : Indicator
    {
        public AnimationPlayer Player = new AnimationPlayer();
        public Animation Animation;
        public SpriteSheet SpriteSheet;
        
        public override void Update(DwarfTime time)
        {
            base.Update(time);

            if (!Player.HasValidAnimation()) Player.Play(Animation);
            Player.Update(time);

            var frame = Animation.Frames[Player.CurrentFrame];
            var frameRect = new Rectangle(frame.X * SpriteSheet.FrameWidth, frame.Y * SpriteSheet.FrameHeight, SpriteSheet.FrameWidth, SpriteSheet.FrameHeight);
            Image = new NamedImageFrame(SpriteSheet.AssetName, frameRect);

            if (Player.IsDone())
                ShouldDelete = true;
        }       
    }
}
