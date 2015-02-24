using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class AnimatedImagePanel : ImagePanel
    {
        public Animation Animation { get; set; }
        public AnimatedImagePanel(DwarfGUI gui, GUIComponent parent, Texture2D image) 
            : base(gui, parent, image)
        {

        }

        public AnimatedImagePanel(DwarfGUI gui, GUIComponent parent, ImageFrame image) 
            : base(gui, parent, image)
        {

        }

        public AnimatedImagePanel(DwarfGUI gui, GUIComponent parent, Animation animtion)
            :base(gui, parent, new ImageFrame(animtion.SpriteSheet.GetTexture(), animtion.GetCurrentFrameRect()))
        {
            Animation = animtion;
        }

        public override void Update(DwarfTime time)
        {
            if (Animation != null)
            {
                Animation.Update(time, Timer.TimerMode.Real);
                Image.Image = Animation.SpriteSheet.GetTexture();
                Image.SourceRect = Animation.GetCurrentFrameRect();
            }
            base.Update(time);
        }
    }
}
