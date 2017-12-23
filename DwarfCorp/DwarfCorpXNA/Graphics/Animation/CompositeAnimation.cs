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
    public class CompositeAnimation : Animation
    {
        [JsonIgnore]
        private Composite _cached_Composite = null;

        [JsonIgnore]
        private Composite Composite
        {
            get
            {
                if (_cached_Composite == null)
                    _cached_Composite = CompositeLibrary.GetComposite(CompositeName, CompositeFrameSize);
                return _cached_Composite;
            }
        }

        public string CompositeName;
        public Point CompositeFrameSize = Point.Zero;
        public List<CompositeFrame> CompositeFrames { get; set; }

        [JsonIgnore]
        public Point CurrentOffset { get; set; }

        public override int GetFrameCount()
        {
            return CompositeFrames.Count;
        }

        public CompositeAnimation()
        {
            CompositeFrames = new List<CompositeFrame>();
        }

        public override void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            if (CurrentFrame >= CompositeFrames.Count)
                return;

            SpriteSheet = new SpriteSheet((Texture2D)Composite.Target);
            CurrentOffset = Composite.PushFrame(CompositeFrames[CurrentFrame]);
            var rect = Composite.GetFrameRect(CurrentOffset);
            Primitive.SetFrame(SpriteSheet, rect, rect.Width / 32.0f, rect.Height / 32.0f, Color.White, Color.White, Flipped);
        }

        public override ImageFrame GetAsImageFrame(int CurrentFrame)
        {
            return new ImageFrame(Composite.Target, Composite.GetFrameRect(CurrentOffset));
        }
    }
}