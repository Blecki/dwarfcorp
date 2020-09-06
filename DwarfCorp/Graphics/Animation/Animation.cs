using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Animation
    {

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            //throw new InvalidOperationException();
        }

        public string Name { get; set; }
        public List<Point> Frames { get; set; }
        public Color Tint { get; set; }
        public float FrameHZ { get; set; }
        public List<float> Speeds { get; set; } 
        public float SpeedMultiplier { get; set; }
        public bool Loops = false;

        public virtual int GetFrameCount()
        {
            return Frames.Count;
        }

        public Animation()
        {
            Frames = new List<Point>();
            Speeds = new List<float>();
            SpeedMultiplier = 1.0f;
            Tint = Color.White;
        }
    }
}