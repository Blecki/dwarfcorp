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
    public class DesignationDrawer
    {
        public DesignationType VisibleTypes = DesignationType._All;

        public class DesignationTypeProperties
        {
            public Color Color;
            public Color ModulatedColor;
            public NamedImageFrame Icon;
            public float LineWidth = 0.1f;
            public enum DrawBoxType
            {
                FullBox,
                TopBox,
                PreviewVoxel,
            }
            public DrawBoxType DrawType;
        }

        [JsonIgnore]
        public static Dictionary<DesignationType, DesignationTypeProperties> DesignationProperties = new Dictionary<DesignationType, DesignationTypeProperties>();

        public static DesignationTypeProperties DefaultProperties = new DesignationTypeProperties
        {
            Color = Color.Gray,
            Icon = null,
            ModulatedColor = new Color(255, 255, 255, 128)
        };

        public DesignationDrawer()
        {
            DesignationProperties = new Dictionary<DesignationType, DesignationTypeProperties>();

            DesignationProperties.Add(DesignationType.Dig, new DesignationTypeProperties
            {
                Color = Color.Red,
                Icon = new NamedImageFrame("newgui/pointers", 32, 1, 0)
            });

            DesignationProperties.Add(DesignationType.Guard, new DesignationTypeProperties
            {
                Color = new Color(10, 10, 205),
                Icon = new NamedImageFrame("newgui/pointers", 32, 3, 0),
                DrawType = DesignationTypeProperties.DrawBoxType.TopBox
            });

            DesignationProperties.Add(DesignationType.Chop, new DesignationTypeProperties
            {
                Color = Color.LightGreen,
                Icon = new NamedImageFrame("newgui/pointers", 32, 5, 0)
            });

            DesignationProperties.Add(DesignationType.Gather, new DesignationTypeProperties
            {
                Color = Color.Orange,
                Icon = new NamedImageFrame("newgui/pointers", 32, 6, 0),
                LineWidth = 0.02f,
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

            DesignationProperties.Add(DesignationType.Plant, new DesignationTypeProperties
            {
                Color = Color.LimeGreen,
                Icon = new NamedImageFrame("newgui/pointers", 32, 4, 1),
                DrawType = DesignationTypeProperties.DrawBoxType.TopBox
            });

            DesignationProperties.Add(DesignationType.Craft, new DesignationTypeProperties
            {
                Color = Color.AntiqueWhite,
                Icon = new NamedImageFrame("newgui/pointers", 32, 4, 0)
            });

            DesignationProperties.Add(DesignationType.Put, new DesignationTypeProperties
            {
                Color = new Color(1.0f, 0.0f, 0.0f, 1.0f),
                DrawType = DesignationTypeProperties.DrawBoxType.PreviewVoxel
            });
        }

        public void DrawHilites(
            DesignationSet Set,
            Action<Vector3, Vector3, Color, float, bool> DrawBoxCallback,
            Action<Vector3, VoxelType> DrawPhantomCallback)
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


            foreach (var entity in Set.EnumerateEntityDesignations())
            {
                if ((entity.Type & VisibleTypes) == entity.Type)
                {
                    var props = DefaultProperties;
                    if (DesignationProperties.ContainsKey(entity.Type))
                        props = DesignationProperties[entity.Type];

                    // Todo: More consistent drawing?
                    if (entity.Type == DesignationType.Craft)
                    {
                        entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, true);
                        if (!entity.Body.Active)
                            entity.Body.SetVertexColorRecursive(props.ModulatedColor);
                    }
                    else
                    {
                        var box = entity.Body.GetBoundingBox();
                        DrawBoxCallback(box.Min, box.Max - box.Min, props.ModulatedColor, props.LineWidth, false);
                        entity.Body.SetVertexColorRecursive(props.ModulatedColor);
                    }
                }
                else if (entity.Type == DesignationType.Craft) // Make the ghost object invisible if these designations are turned off.
                    entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, false);
            }
        }
    }
}
