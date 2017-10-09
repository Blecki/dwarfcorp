// Drawer3D.cs
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
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is a convenience class for drawing lines, boxes, etc. to the screen.
    /// </summary>
    [Saving.SaveableObject(0)]
    public class DesignationDrawer : Saving.ISaveableObject
    {
        [JsonProperty]
        private Dictionary<DesignationType, List<GlobalVoxelCoordinate>> HilitedVoxels = new Dictionary<DesignationType, List<GlobalVoxelCoordinate>>();

        private class HilitedBody
        {
            public Body Body;
            public DesignationType DesignationType;
        }

        [JsonProperty]
        private List<HilitedBody> HilitedBodies = new List<HilitedBody>();

        private class DesignationTypeProperties
        {
            public Color Color;
            public Color ModulatedColor;
            public NamedImageFrame Icon;
        }

        private Dictionary<DesignationType, DesignationTypeProperties> DesignationProperties = new Dictionary<DesignationType, DesignationTypeProperties>();

        private static DesignationTypeProperties DefaultProperties = new DesignationTypeProperties
        {
            Color = Color.Gray,
            Icon = null
        };

        public DesignationDrawer()
        {
            DesignationProperties.Add(DesignationType.Dig, new DesignationTypeProperties
            {
                Color = Color.Red,
                Icon = new NamedImageFrame("newgui/pointers", 32, 1, 0)
            });

            DesignationProperties.Add(DesignationType.Chop, new DesignationTypeProperties
            {
                Color = Color.LightGreen,
                Icon = new NamedImageFrame("newgui/pointers", 32, 5, 0)
            });

            DesignationProperties.Add(DesignationType.Gather, new DesignationTypeProperties
            {
                Color = Color.Orange,
                Icon = new NamedImageFrame("newgui/pointers", 32, 6, 0)
            });

            DesignationProperties.Add(DesignationType.Attack, new DesignationTypeProperties
            {
                Color = Color.Red,
                Icon = new NamedImageFrame("newgui/pointers", 32, 2, 0)
            });

            DesignationProperties.Add(DesignationType.Wrangle, new DesignationTypeProperties
            {
                Color = Color.Tomato,
                Icon = new NamedImageFrame("newgui/pointers", 32, 4, 1)
            });
        }

        public void HiliteVoxel(GlobalVoxelCoordinate Coordinate, DesignationType Type)
        {
            if (!HilitedVoxels.ContainsKey(Type))
                HilitedVoxels.Add(Type, new List<GlobalVoxelCoordinate>());
            HilitedVoxels[Type].Add(Coordinate);
        }

        public void UnHiliteVoxel(GlobalVoxelCoordinate Coordinate, DesignationType Type)
        {
            if (HilitedVoxels.ContainsKey(Type))
                HilitedVoxels[Type].RemoveAll(v => v == Coordinate);
        }

        public void HiliteEntity(Body Entity, DesignationType Type)
        {
            HilitedBodies.Add(new HilitedBody
            {
                Body = Entity,
                DesignationType = Type
            });
        }

        public void UnHiliteEntity(Body Entity, DesignationType Type)
        {
            HilitedBodies.RemoveAll(b => Object.ReferenceEquals(b.Body, Entity) && Type == b.DesignationType);
        }

        public void EnumerateHilites(Action<Vector3, Vector3, Color, float> Callback)
        {
            var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 2.0f));
            foreach (var properties in DesignationProperties)
            {
                properties.Value.ModulatedColor = new Color(
                    (byte)(MathFunctions.Clamp((float)(properties.Value.Color.R * colorModulation + 50), 0.0f, 255.0f)),
                    (byte)(MathFunctions.Clamp((float)(properties.Value.Color.G * colorModulation + 50), 0.0f, 255.0f)),
                    (byte)(MathFunctions.Clamp((float)(properties.Value.Color.B * colorModulation + 50), 0.0f, 255.0f)),
                    255);
            }

            foreach (var group in HilitedVoxels)
            {
                var props = DefaultProperties;
                if (DesignationProperties.ContainsKey(group.Key))
                    props = DesignationProperties[group.Key];

                foreach (var voxel in group.Value)
                {
                    var v = voxel.ToVector3();
                    Callback(v, Vector3.One, props.ModulatedColor, 0.1f);
                    if (props.Icon != null)
                    {
                        Drawer2D.DrawSprite(props.Icon, v + Vector3.One * 0.5f, Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                    }
                }
            }

            foreach (var entity in HilitedBodies)
            {
                var props = DefaultProperties;
                if (DesignationProperties.ContainsKey(entity.DesignationType))
                    props = DesignationProperties[entity.DesignationType];

                var box = entity.Body.GetBoundingBox();
                Callback(box.Min, box.Max - box.Min, props.ModulatedColor, 0.1f);
                if (props.Icon != null)
                {
                    Drawer2D.DrawSprite(props.Icon, entity.Body.Position + Vector3.One * 0.5f, Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                }
            }
        }

        public class SaveNugget : Saving.Nugget
        {
            public class SavedHilitedBody
            {
                public Saving.Nugget Body;
                public DesignationType DesignationType;
            }

            public Dictionary<DesignationType, List<GlobalVoxelCoordinate>> HilitedVoxels;
            public List<SavedHilitedBody> HilitedBodies;
        }

        Saving.Nugget Saving.ISaveableObject.SaveToNugget(Saving.Saver SaveSystem)
        {
            return new SaveNugget
            {
                HilitedVoxels = HilitedVoxels,
                HilitedBodies = HilitedBodies.Select(h => new SaveNugget.SavedHilitedBody
                {
                    Body = SaveSystem.SaveObject(h.Body),
                    DesignationType = h.DesignationType
                }).ToList()
            };
        }

        void Saving.ISaveableObject.LoadFromNugget(Saving.Loader SaveSystem, Saving.Nugget From)
        {
            var n = From as SaveNugget;
            HilitedVoxels = n.HilitedVoxels;
            HilitedBodies = n.HilitedBodies.Select(h => new HilitedBody
            {
                Body = SaveSystem.LoadObject(h.Body) as Body,
                DesignationType = h.DesignationType
            }).ToList();
        }
    }
}
