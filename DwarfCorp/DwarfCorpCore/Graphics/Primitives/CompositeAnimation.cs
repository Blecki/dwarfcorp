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
    /// <summary>
    /// An animation flips a billboard sprite between several
    /// frames on a sprite sheet at a fixed rate.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CompositeAnimation : Animation
    {
        [JsonIgnore]
        public Composite Composite 
        { 
            get 
            { 
                return CompositeLibrary.Composites.ContainsKey(CompositeName) ? 
                    CompositeLibrary.Composites[CompositeName] : null; 
            } 
        }

        public string CompositeName { get; set; }
        public List<Composite.Frame> CompositeFrames { get; set; }
        [JsonIgnore]
        public Point CurrentOffset { get; set; }
        [JsonIgnore]
        public bool FirstIter = false;
        [JsonIgnore]
        public BillboardPrimitive Primitive { get; set; }

        private bool HasValidFrame = false;

        public struct Descriptor
        {
            public List<SpriteSheet> Layers { get; set; }
            public List<Color> Tints { get; set; }

            public struct AnimationDescriptor
            {
                public string Name { get; set; }
                public List<List<int>> Frames { get; set; }
                public List<float> Speed { get; set; }
                public bool PlayOnce { get; set; }
            }

            public List<AnimationDescriptor> Animations { get; set; }

            public List<CompositeAnimation> GenerateAnimations(string composite)
            {
                List<CompositeAnimation> toReturn = new List<CompositeAnimation>();

                foreach (AnimationDescriptor descriptor in Animations)
                {
                    int[][] frames = new int[descriptor.Frames.Count][];

                    int i = 0;
                    foreach (List<int> frame in descriptor.Frames)
                    {
                        frames[i]  = new int[frame.Count];

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
                        Loops = !descriptor.PlayOnce,
                        SpriteSheet = Layers[0]
                    };

                    toReturn.Add(animation);
                }

                return toReturn;

            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (!CompositeLibrary.Composites.ContainsKey(CompositeName))
            {
                CompositeLibrary.Composites[CompositeName] = new Composite(CompositeFrames);
            }
        }

        public CompositeAnimation()
        {
            CompositeFrames = new List<Composite.Frame>();
        }

        public CompositeAnimation(string composite, List<Composite.Frame> frames) :
            this()
        {
            if (!CompositeLibrary.Composites.ContainsKey(composite))
            {
                CompositeLibrary.Composites[composite] = new Composite(frames);
            }
            CompositeName = composite;
            CompositeFrames = frames;
            FrameWidth = Composite.FrameSize.X;
            FrameHeight = Composite.FrameSize.Y;
            CreatePrimitive();
            UpdatePrimitive();
            Play();
        }

        public CompositeAnimation(string composite, List<SpriteSheet> layers, List<Color> tints,  int[][] frames) :
            this(composite, CreateFrames(layers, tints, frames))
        {
            
        }

        public static List<Composite.Frame> CreateFrames(List<SpriteSheet> layers, List<Color> tints, params int[][] frames)
        {
            List<Composite.Frame> frameList = new List<Composite.Frame>();
            foreach (int[] frame in frames)
            {
                Composite.Frame currFrame = new Composite.Frame();

                int x = frame[0];
                int y = frame[1];

                currFrame.Position = new Point(x, y);

                for (int j = 2; j < frame.Length; j++)
                {
                    int layer = frame[j];
                    currFrame.Layers.Add(layers[layer]);
                    currFrame.Tints.Add(tints[Math.Min(Math.Max(layer, 0), tints.Count - 1)]);
                }

                frameList.Add(currFrame);
            }

            return frameList;
        }

        public void CreatePrimitive()
        {
            Primitives = new List<BillboardPrimitive>();
        }

        public void UpdatePrimitive()
        {
            if (HasValidFrame && CurrentFrame >= 0 && CurrentFrame < CompositeFrames.Count)
            {
                Primitive = Composite.CreatePrimitive(GameState.Game.GraphicsDevice, CurrentOffset);
                Composite.ApplyBillboard(Primitive, CurrentOffset);
                Primitives.Clear();

                foreach (Composite.Frame frame in CompositeFrames)
                {
                    Primitives.Add(Primitive);
                }
            }
        }

        public override Rectangle GetCurrentFrameRect()
        {
            Rectangle toReturn = new Rectangle(CurrentOffset.X * Composite.FrameSize.X, CurrentOffset.Y * Composite.FrameSize.Y, FrameWidth, FrameHeight);
            return toReturn;
        }

        public override void Update(DwarfTime gameTime, Timer.TimerMode mode = Timer.TimerMode.Game)
        {
            base.Update(gameTime, mode);
            CurrentOffset = Composite.PushFrame(CompositeFrames[CurrentFrame]);
            HasValidFrame = true;
            UpdatePrimitive();
        }

        public override void PreRender()
        {
            SpriteSheet = new SpriteSheet((Texture2D)Composite.Target);
            base.PreRender();
        }

        public override void NextFrame()
        {
            CurrentFrame++;
            InvokeNextFrame(CurrentFrame);
            if (CurrentFrame >= CompositeFrames.Count)
            {
                if (Loops)
                {
                    CurrentFrame = 0;
                    InvokeAnimationLooped();
                }
                else
                {
                    CurrentFrame = CompositeFrames.Count - 1;
                    InvokeAnimationCompleted();
                }
            }
            UpdatePrimitive();
        }

        public override bool IsDone()
        {
            return CurrentFrame >= CompositeFrames.Count - 1;
        }

        public override Animation Clone()
        {
            return new CompositeAnimation(CompositeName, CompositeFrames)
            {
                Name = Name,
                FrameHZ = FrameHZ,
                Flipped = Flipped,
                Loops = Loops,
                CurrentFrame =  CurrentFrame,
                Speeds = new List<float>(Speeds),
                SpriteSheet = SpriteSheet
            };
        }
    }

}