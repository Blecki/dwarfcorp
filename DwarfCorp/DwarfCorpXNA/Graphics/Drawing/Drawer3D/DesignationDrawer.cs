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

namespace DwarfCorp
{
    /// <summary>
    /// This is a convenience class for drawing lines, boxes, etc. to the screen.
    /// </summary>
    public class DesignationDrawer
    {
        public enum DesignationType
        {
            Dig, // Red
            Farm, // LimeGreen
            Guard, // Blue
            Chop // Red
        }

        private Dictionary<DesignationType, List<GlobalVoxelCoordinate>> HilitedVoxels = new Dictionary<DesignationType, List<GlobalVoxelCoordinate>>();
        private List<Body> HilitedBodies = new List<Body>();

        private Dictionary<DesignationType, Color> BaseColor = new Dictionary<DesignationType, Color>();

        public DesignationDrawer()
        {
            BaseColor.Add(DesignationType.Dig, Color.Red);
            BaseColor.Add(DesignationType.Farm, Color.LimeGreen);
            BaseColor.Add(DesignationType.Guard, Color.Blue);
            BaseColor.Add(DesignationType.Chop, Color.Red);
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
            HilitedBodies.Add(Entity);
        }

        public void UnHiliteEntity(Body Entity, DesignationType Type)
        {
            HilitedBodies.RemoveAll(b => Object.ReferenceEquals(b, Entity));
        }

        public void EnumerateHilites(Action<Vector3, Vector3, Color, float> Callback)
        {
            var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 2.0f));
            var modulatedColors = new Dictionary<DesignationType, Color>();
            foreach (var color in BaseColor)
            {
                modulatedColors.Add(color.Key, new Color(
                    (byte)(color.Value.R * colorModulation + 50),
                    (byte)(color.Value.G * colorModulation + 50),
                    (byte)(color.Value.B * colorModulation + 50),
                    255));
            }

            foreach (var group in HilitedVoxels)
            {
                var color = modulatedColors[group.Key];

                foreach (var voxel in group.Value)
                    Callback(voxel.ToVector3(), Vector3.One, color, 0.1f);
            }

            foreach (var entity in HilitedBodies)
            {
                var box = entity.GetBoundingBox();
                Callback(box.Min, box.Max - box.Min, modulatedColors[DesignationType.Chop], 0.1f);
            }
        }
    }
}
