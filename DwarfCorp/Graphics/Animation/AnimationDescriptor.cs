using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AnimationDescriptor
    {
        public struct Frame
        {
            public float Speed;
            public int Row;
            public int Column;
        }

        public string Name;
        public List<Frame> Frames;
        public bool Loops = true;

        public Animation CreateAnimation()
        {
            return new Animation
            {
                Frames = Frames.Select(f => new Point(f.Column, f.Row)).ToList(),
                Speeds = Frames.Select(f => f.Speed).ToList(),
                Loops = Loops,
                Name = Name,
                FrameHZ = 5.0f,
            };
        }
    }
}