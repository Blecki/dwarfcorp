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
            public bool PlayOnce;
        }

        public List<SpriteSheet> Layers;
        public List<Color> Tints;
        public List<AnimationDescriptor> Animations;

        public List<CompositeAnimation> GenerateAnimations(string composite)
        {
            List<CompositeAnimation> toReturn = new List<CompositeAnimation>();

            foreach (AnimationDescriptor descriptor in Animations)
            {
                int[][] frames = new int[descriptor.Frames.Count][];

                int i = 0;
                foreach (List<int> frame in descriptor.Frames)
                {
                    frames[i] = new int[frame.Count];

                    int k = 0;
                    foreach (int j in frame)
                    {
                        frames[i][k] = j;
                        k++;
                    }

                    i++;
                }

                List<float> speeds = new List<float>();

                foreach (float speed in descriptor.Speed)
                {
                    speeds.Add(1.0f / speed);
                }

                CompositeAnimation animation = new CompositeAnimation(composite, Layers, Tints, frames)
                {
                    Name = descriptor.Name,
                    Speeds = speeds,
                    //Loops = !descriptor.PlayOnce,
                    SpriteSheet = Layers[0]
                };

                toReturn.Add(animation);
            }

            return toReturn;

        }
    }
}