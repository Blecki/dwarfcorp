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

    public struct AnimationSetDescriptor
    {
        // Can't actually change member names without breaking JSON.
        public struct AnimationDescriptor
        {
            public string Name;
            public List<List<int>> Frames;
            public List<float> Speed;
            public List<float> YOffset;
            public bool PlayOnce;
        }

        public List<SpriteSheet> Layers;
        public List<Color> Tints;
        public List<AnimationDescriptor> Animations;
    }
}