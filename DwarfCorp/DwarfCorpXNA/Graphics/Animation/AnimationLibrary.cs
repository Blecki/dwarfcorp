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
                        FrameHZ = 5.0f,
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
                        FrameHZ = 5.0f
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
                    var descriptor = ContentPaths.LoadFromJson<AnimationSetDescriptor>(Path);
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

        private static List<CompositeAnimation> GenerateAnimations(string composite, AnimationSetDescriptor Set)
        {
            List<CompositeAnimation> toReturn = new List<CompositeAnimation>();

            var compositeWidth = Set.Layers.Select(l => l.FrameWidth).Max();
            var compositeHeight = Set.Layers.Select(l => l.FrameHeight).Max();

            foreach (var descriptor in Set.Animations)
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

                CompositeAnimation animation = new CompositeAnimation()
                {
                    CompositeName = composite,
                    CompositeFrames = CreateFrames(Set.Layers, Set.Tints, frames),
                    Name = descriptor.Name,
                    Speeds = speeds,
                    Loops = !descriptor.PlayOnce,
                    SpriteSheet = Set.Layers[0],
                    CompositeFrameSize = new Point(compositeWidth, compositeHeight)
                };

                toReturn.Add(animation);
            }

            return toReturn;

        }

        private static List<CompositeFrame> CreateFrames(List<SpriteSheet> layers, List<Color> tints, params int[][] frames)
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
    }
}