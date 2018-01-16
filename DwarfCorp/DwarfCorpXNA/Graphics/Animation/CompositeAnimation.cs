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
        [OnSerialized]
        private void _onSerialized(StreamingContext Context)
        {
            var x = 5;

        }

        [JsonIgnore]
        private Composite _cached_Composite = null;

        [JsonIgnore]
        private Composite Composite
        {
            get
            {
                if (_cached_Composite == null)
                    _cached_Composite = CompositeLibrary.GetComposite(CompositeName,
                        new Point(CompositeFrames.SelectMany(f => f.Cells).Select(c => c.Sheet.FrameWidth).Max(),
                        CompositeFrames.SelectMany(f => f.Cells).Select(c => c.Sheet.FrameHeight).Max()));

                return _cached_Composite;
            }
        }

        public string CompositeName;
        public List<CompositeFrame> CompositeFrames = new List<CompositeFrame>();

        [JsonIgnore]
        public Point CurrentOffset { get; set; }

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

        public override Texture2D GetTexture()
        {
            return Composite.Target;
        }

        public override int GetFrameCount()
        {
            return CompositeFrames.Count;
        }
    }
}