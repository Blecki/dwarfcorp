using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public class MousePointer
    {
        public String Sheet;
        public int[] Frames = null;
        public float Frame = 0;
        public float FrameRate = 4;
        
        public MousePointer(String Sheet, float FrameRate, params int[] Frames)
        {
            this.Sheet = Sheet;
            this.Frames = Frames;
            this.FrameRate = FrameRate;
        }

        public void Update(float Seconds)
        {
            Frame += Seconds * FrameRate;
            if (Frame >= Frames.Length) Frame -= Frames.Length;
        }

        public int AnimationFrame { get { return Frames[ (int)Math.Floor(Frame) ]; } }
    }
}