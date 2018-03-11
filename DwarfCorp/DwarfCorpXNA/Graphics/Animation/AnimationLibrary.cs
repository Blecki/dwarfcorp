// AnimationLibrary.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
        private static Dictionary<String, List<Animation>> Animations = new Dictionary<String, List<Animation>>();

        public static Animation CreateAnimation(Animation.SimpleDescriptor Descriptor)
        {
            // Can't cache these guys, unfortunately. Thankfully, they
            //  are only used by the dialogue screen.
            return new Animation()
            {
                SpriteSheet = new SpriteSheet(Descriptor.AssetName),
                Frames = Descriptor.Frames.Select(s => new Point(s, 0)).ToList(),
                SpeedMultiplier = Descriptor.Speed,
                Speeds = new List<float>()
            };
        }

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

                Animations.Add(TextureAsset, new List<Animation>
                {
                    new Animation()
                    {
                        SpriteSheet = spriteSheet,
                        Frames = frames,
                        Name = TextureAsset,
                        FrameHZ = 15.0f,
                    }
                });
            }

            return Animations[TextureAsset][0];
        }


        public static Animation CreateAnimation(
            SpriteSheet Sheet,
            List<Point> Frames,
            String UniqueName)
        {
            if (!Animations.ContainsKey(UniqueName))
            {
                Animations.Add(UniqueName, new List<Animation>
                {
                    new Animation()
                    {
                        SpriteSheet = Sheet,
                        Frames = Frames,
                        Name = UniqueName,
                        FrameHZ = 5.0f,
                        Tint = Color.White
                    }
                });
            }

            return Animations[UniqueName][0];
        }

        public static List<Animation> LoadCompositeAnimationSet(String Path, String CompositeName)
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
                                Loops = a.Loops,
                                FrameHZ = a.FrameHZ,
                                SpeedMultiplier = a.SpeedMultiplier,
                                //Todo: Support per-cell tint in standard animation?
                                Tint = a.CompositeFrames[0].Cells[0].Tint,
                                Flipped = a.Flipped,
                                Frames = a.CompositeFrames.Select(f => f.Cells[0].Tile).ToList(),
                            };
                        }

                        return a as Animation;
                        
                        }).ToList());
                }
                catch (Exception)
                {
                    var errorAnimations = new List<Animation>();

                    errorAnimations.Add(
                        new Animation()
                        {
                            SpriteSheet = new SpriteSheet(ContentPaths.Error, 32),
                            Frames = new List<Point> { Point.Zero },
                            Name = "ERROR"
                        });

                    
                    Animations.Add(Path, errorAnimations);
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
                    Speeds = descriptor.Speed.Select(s => 1.0f / s).ToList(),
                    Loops = !descriptor.PlayOnce,
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