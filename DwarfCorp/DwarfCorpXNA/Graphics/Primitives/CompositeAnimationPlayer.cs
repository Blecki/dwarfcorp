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
    public class CompositeAnimationPlayer : Animation
    {
        [JsonIgnore]
        private Composite Composite 
        { 
            get 
            { 
                return CompositeLibrary.Composites.ContainsKey(CompositeName) ? 
                    CompositeLibrary.Composites[CompositeName] : null; 
            } 
        }

        public string CompositeName { get; set; }
        public List<CompositeFrame> CompositeFrames { get; set; }
        [JsonIgnore]
        public Point CurrentOffset { get; set; }
        [JsonIgnore]
        public bool FirstIter = false;
        [JsonIgnore]
        public BillboardPrimitive Primitive { get; set; }
        private Point lastOffset = new Point(-1, -1);
        private bool HasValidFrame = false;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (!CompositeLibrary.Composites.ContainsKey(CompositeName))
            {
                CompositeLibrary.Composites[CompositeName] = new Composite(CompositeFrames);
            }
        }

        public CompositeAnimationPlayer()
        {
            CompositeFrames = new List<CompositeFrame>();
        }

        public CompositeAnimationPlayer(string composite, List<CompositeFrame> frames) :
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

        public CompositeAnimationPlayer(string composite, List<SpriteSheet> layers, List<Color> tints,  int[][] frames) :
            this(composite, CreateFrames(layers, tints, frames))
        {
            
        }

        public static List<CompositeFrame> CreateFrames(List<SpriteSheet> layers, List<Color> tints, params int[][] frames)
        {
            List<CompositeFrame> frameList = new List<CompositeFrame>();
            foreach (int[] frame in frames)
            {
                CompositeFrame currFrame = new CompositeFrame();

                int x = frame[0];
                int y = frame[1];

                for (int j = 2; j < frame.Length; j++)
                {
                    var cell = new CompositeCell();
                    cell.Tile = new Point(x, y);
                    cell.Sheet = layers[frame[j]];
                    cell.Tint = tints[Math.Min(Math.Max(frame[j], 0), tints.Count - 1)];
                    currFrame.Cells.Add(cell);
                }

                frameList.Add(currFrame);
            }

            return frameList;
        }

        public void CreatePrimitive()
        {
            Primitives = new List<BillboardPrimitive>();
        }

        public void UpdatePrimitive(bool force = false)
        {
            if (HasValidFrame && CurrentFrame >= 0 && CurrentFrame < CompositeFrames.Count && lastOffset != CurrentOffset)
            {
                Primitive = Composite.CreatePrimitive(GameState.Game.GraphicsDevice, CurrentOffset);
                Composite.ApplyBillboard(Primitive, CurrentOffset);
                Primitives.Clear();

                foreach (CompositeFrame frame in CompositeFrames)
                {
                    Primitives.Add(Primitive);
                }
                lastOffset = CurrentOffset;
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
            CurrentFrame = Math.Min(Math.Max(CurrentFrame, 0), CompositeFrames.Count - 1);
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
            //InvokeNextFrame(CurrentFrame);
            if (CurrentFrame >= CompositeFrames.Count)
            {
                if (Loops)
                {
                    CurrentFrame = 0;
                    //InvokeAnimationLooped();
                }
                else
                {
                    CurrentFrame = CompositeFrames.Count - 1;
                    //InvokeAnimationCompleted();
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