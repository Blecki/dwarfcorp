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
        public override bool CanUseInstancing { get { return false; } }
        
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
        public Composite.FrameID CurrentOffset { get; set; }

        public void PushFrames()
        {
            foreach (var frame in CompositeFrames)
            {
                Composite.PushFrame(frame);
            }
        }

        public override void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
        {
            SpriteSheet = new SpriteSheet(Composite.GetTarget(CurrentOffset));
            if (CurrentFrame >= CompositeFrames.Count)
                return;
            PushFrames();
            CurrentOffset = Composite.PushFrame(CompositeFrames[CurrentFrame]);
            var rect = Composite.GetFrameRect(CurrentOffset);
            Primitive.SetFrame(SpriteSheet, rect, rect.Width / 32.0f, rect.Height / 32.0f, Color.White, Color.White, Flipped);
        }

        public override NamedImageFrame GetAsImageFrame(int CurrentFrame)
        {
            return new NamedImageFrame(Composite.GetTarget(CurrentOffset), Composite.GetFrameRect(CurrentOffset));
        }

        public override Texture2D GetTexture()
        {
            return Composite.GetTarget(CurrentOffset);
        }

        public override int GetFrameCount()
        {
            return CompositeFrames.Count;
        }
    }
}