using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AnimationLibrary
    {
        private static Dictionary<String, Dictionary<String, Animation>> Animations = new Dictionary<String, Dictionary<String, Animation>>();

        public static Animation CreateSimpleAnimation(String TextureAsset)
        {
            if (!Animations.ContainsKey(TextureAsset))
            {
                // Don't need to worry about failure - texture loading returns
                //  error texture already.
                var spriteSheet = new SpriteSheet(TextureAsset);
                spriteSheet.FrameWidth = Math.Min(spriteSheet.FrameWidth, spriteSheet.FrameHeight);               

                var frames = new List<Point>();
                for (var i = 0; i < spriteSheet.Width / spriteSheet.FrameWidth; ++i)
                    frames.Add(new Point(i, 0));

                return CreateAnimation(spriteSheet, frames, TextureAsset);
            }

            return Animations[TextureAsset].Values.FirstOrDefault();
        }

        public static Animation CreateAnimation(SpriteSheet Sheet, List<Point> Frames, String UniqueName)
        {
            if (!Animations.ContainsKey(UniqueName))
                Animations.Add(UniqueName, new Dictionary<String, Animation>
                {
                    {
                        "simple",
                        new Animation()
                        {
                            SpriteSheet = Sheet,
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
                    var anims = FileUtils.LoadJsonListFromMultipleSources<NewAnimationDescriptor>(Path, null, a => a.Name)
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
                                SpriteSheet = new SpriteSheet(ContentPaths.Error, 32),
                                Frames = new List<Point> { Point.Zero },
                                Name = "ERROR"
                            }
                        }
                    });
                }
            }

            return Animations[Path];
        }

        public static Dictionary<String, Animation> LoadCompositeAnimationSet(String Path, String CompositeName)
        {
            if (!Animations.ContainsKey(Path))
            {
                try
                {
                    var descriptor = FileUtils.LoadJsonFromResolvedPath<AnimationSetDescriptor>(Path);
                    Animations.Add(Path, GenerateAnimations(CompositeName, descriptor).Select(a =>
                    {
                        bool simplify = true;
                        string asset = null;
                        foreach (var frame in a.CompositeFrames)
                        {
                            if (frame.Cells.Count != 1)
                            {
                                simplify = false;
                                break;
                            }

                            if (asset == null)
                                asset = frame.Cells[0].Sheet.AssetName;
                            else if (asset != frame.Cells[0].Sheet.AssetName)
                            {
                                simplify = false;
                                break;
                            }
                        }

                        if (simplify)
                        {
                            var sheet = a.CompositeFrames[0].Cells[0].Sheet;
                            return new Animation()
                            {
                                SpriteSheet = sheet,
                                Name = a.Name,
                                Speeds = a.Speeds,
                                YOffset = a.YOffset,
                                Loops = a.Loops,
                                FrameHZ = a.FrameHZ,
                                SpeedMultiplier = a.SpeedMultiplier,
                                //Todo: Support per-cell tint in standard animation?
                                Tint = a.CompositeFrames[0].Cells[0].Tint,
                                Flipped = a.Flipped,
                                Frames = a.CompositeFrames.Select(f => f.Cells[0].Tile).ToList(),
                            };
                        }
                        a.PushFrames();
                        return a as Animation;
                        
                        }).ToDictionary(a => a.Name));
                }
                catch (Exception)
                {
                    Animations.Add(Path, new Dictionary<string, Animation>
                    {
                        {
                            "ERROR",
                            new Animation()
                            {
                                SpriteSheet = new SpriteSheet(ContentPaths.Error, 32),
                                Frames = new List<Point> { Point.Zero },
                                Name = "ERROR"
                            }
                        }
                    });
                }
            }

            return Animations[Path];
        }

        private static List<CompositeAnimation> GenerateAnimations(
            string Composite, 
            AnimationSetDescriptor Set)
        {
            return Set.Animations.Select(descriptor =>
                new CompositeAnimation()
                {
                    CompositeName = Composite,
                    CompositeFrames = CreateFrames(Set.Layers, Set.Tints, descriptor.Frames),
                    Name = descriptor.Name,
                    Speeds = descriptor.Speed,
                    Loops = !descriptor.PlayOnce,
                    YOffset = descriptor.YOffset
                }).ToList();
        }

        private static List<CompositeFrame> CreateFrames(
            List<SpriteSheet> Layers, 
            List<Color> Tints, 
            List<List<int>> Frames)
        {
            var frameList = new List<CompositeFrame>();

            foreach (var frame in Frames)
            {
                var currentFrame = new CompositeFrame();
                
                //[0, 1, 2, 3]
                //Values 0 and 1 are the tile to draw.
                //2 and beyond are the layers to draw.

                for (int j = 2; j < frame.Count; j++)
                {
                    var cell = new CompositeCell
                    {
                        Tile = new Point(frame[0], frame[1]),
                        Sheet = Layers[frame[j]],
                        Tint = Tints[Math.Min(Math.Max(frame[j], 0), Tints.Count - 1)]
                    };

                    currentFrame.Cells.Add(cell);
                }

                frameList.Add(currentFrame);
            }

            return frameList;
        }
    }
}