using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, Dictionary<String, Animation>> Animations = new Dictionary<String, Dictionary<String, Animation>>();
        private static Dictionary<String, SimpleAnimationTuple> SimpleAnimations = new Dictionary<string, SimpleAnimationTuple>();

        public class SimpleAnimationTuple
        {
            public SpriteSheet SpriteSheet;
            public Animation Animation;
        }

        public static SimpleAnimationTuple CreateSimpleAnimation(String TextureAsset)
        {
            if (!SimpleAnimations.ContainsKey(TextureAsset))
            {
                // Don't need to worry about failure - texture loading returns
                //  error texture already.
                var spriteSheet = new SpriteSheet(TextureAsset);
                spriteSheet.FrameWidth = Math.Min(spriteSheet.FrameWidth, spriteSheet.FrameHeight);

                var frames = new List<Point>();
                for (var i = 0; i < spriteSheet.Width / spriteSheet.FrameWidth; ++i)
                    frames.Add(new Point(i, 0));

                SimpleAnimations.Add(TextureAsset, new SimpleAnimationTuple
                {
                    SpriteSheet = spriteSheet,
                    Animation = new Animation()
                    {
                        Frames = frames,
                        Name = TextureAsset,
                        FrameHZ = 5.0f,
                        Tint = Color.White
                    }
                });
            }

            return SimpleAnimations[TextureAsset];
        }

        public static Animation CreateAnimation(List<Point> Frames, String UniqueName)
        {
            if (!Animations.ContainsKey(UniqueName))
                Animations.Add(UniqueName, new Dictionary<String, Animation>
                {
                    {
                        "simple",
                        new Animation()
                        {
                            Frames = Frames,
                            Name = UniqueName,
                            FrameHZ = 5.0f,
                            Tint = Color.White
                        }
                    }
                });

            return Animations[UniqueName].Values.FirstOrDefault();
        }

        public static Dictionary<String, Animation> LoadNewLayeredAnimationFormat(String Path)
        {
            if (!Animations.ContainsKey(Path))
            {
                try
                {
                    var anims = FileUtils.LoadJsonListFromMultipleSources<AnimationDescriptor>(Path, null, a => a.Name)
                        .Select(a => a.CreateAnimation()).ToDictionary(a => a.Name);
                    Animations.Add(Path, anims);
                }
                catch
                {
                    Animations.Add(Path, new Dictionary<string, Animation>
                    {
                        {
                            "ERROR",
                            new Animation()
                            {
                                Frames = new List<Point> { Point.Zero },
                                Name = "ERROR"
                            }
                        }
                    });
                }
            }

            return Animations[Path];
        }
    }
}