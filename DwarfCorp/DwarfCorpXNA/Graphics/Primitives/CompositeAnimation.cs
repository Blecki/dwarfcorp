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

        public override int GetFrameCount()
        {
            return CompositeFrames.Count;
        }

        public CompositeAnimation()
        {
            CompositeFrames = new List<CompositeFrame>();
        }

        public CompositeAnimation(string composite, List<CompositeFrame> frames) :
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
        }

        public CompositeAnimation(string composite, List<SpriteSheet> layers, List<Color> tints,  int[][] frames) :
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

        public override void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            if (CurrentFrame >= CompositeFrames.Count) return;

            var currentOffset = Composite.PushFrame(CompositeFrames[CurrentFrame]);
            Composite.ApplyBillboard(Primitive, currentOffset);
            SpriteSheet = new SpriteSheet((Texture2D)Composite.Target);
        }

        public override Rectangle GetFrameRect(int Frame)
        {
            Rectangle toReturn = new Rectangle(CurrentOffset.X * Composite.FrameSize.X, CurrentOffset.Y * Composite.FrameSize.Y, FrameWidth, FrameHeight);
            return toReturn;
        }
                
        public override void PreRender()
        {
            SpriteSheet = new SpriteSheet((Texture2D)Composite.Target);
            base.PreRender();
        }

        public override Animation Clone()
        {
            return new CompositeAnimation(CompositeName, CompositeFrames)
            {
                Name = Name,
                FrameHZ = FrameHZ,
                Flipped = Flipped,
                Speeds = new List<float>(Speeds),
                SpriteSheet = SpriteSheet
            };
        }
    }

}