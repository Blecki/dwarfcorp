// ColorGradient.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     A particular position and color in a gradient.
    /// </summary>
    public struct ColorStop
    {
        public Color m_color;
        public float m_position;
    }

    /// <summary>
    ///     Used for creating colorful textures from a single value. Linearly interpolates
    ///     between nearby colors.
    /// </summary>
    public class ColorGradient
    {
        public ColorGradient(Color first, Color last, int numStops)
        {
            Stops = new List<ColorStop>();
            var color1 = new Vector4(first.R/255.0f, first.G/255.0f, first.B/255.0f, first.A/255.0f);
            var color2 = new Vector4(last.R/255.0f, last.G/255.0f, last.B/255.0f, last.A/255.0f);

            Vector4 norm = color2 - color1;
            float length = norm.Length();
            float dStop = length/numStops;
            norm.Normalize();

            float currentStop = 0.0f;
            for (int i = 0; i < numStops; i++)
            {
                var stop = new ColorStop();
                Vector4 colorVec = color1 + currentStop*norm;
                stop.m_color = new Color(colorVec.X, colorVec.Y, colorVec.Y, colorVec.W);
                currentStop += dStop;
                Stops.Add(stop);
            }
        }

        public ColorGradient(List<ColorStop> stops)
        {
            Stops = stops;
        }

        public List<ColorStop> Stops { get; set; }

        // Convenience method to convert a byte into a color for gradients
        // with only a resolution of 255
        public Color GetColor(byte position)
        {
            if (Stops.Count < 254)
            {
                return GetColor(position/255.0f);
            }

            if (position >= Stops.Count)
            {
                position = (byte) (Stops.Count - 1);
            }
            return GetColor((int) position);
        }

        // index into the array
        public Color GetColor(int position)
        {
            if (position >= Stops.Count)
            {
                position = Stops.Count - 1;
            }

            return Stops[position].m_color;
        }

        // Scaled between 0 and 1. Gets nearest stop. This is actually necessary because stops
        // might not be evenly spaced
        public Color GetColor(float position)
        {
            Color averageColor = Color.Black;

            Vector3 sumColor = Vector3.Zero;
            float sumWeights = 0.0f;

            foreach (ColorStop stop in Stops)
            {
                float diff = 1.0f/((stop.m_position - position)*(stop.m_position - position) + 0.001f);
                sumWeights += diff;

                sumColor += new Vector3(stop.m_color.R, stop.m_color.G, stop.m_color.B)*(diff);
            }

            Vector3 averageVector = sumColor/sumWeights;
            averageColor = new Color((byte) averageVector.X, (byte) averageVector.Y, (byte) averageVector.Z);
            return averageColor;
        }

        public static Color Multiply(Color A, Color B)
        {
            return new Color(Math.Min((A.R/255.0f)*(B.R/255.0f), 255), Math.Min(A.G/255.0f*(B.G/255.0f), 255),
                Math.Min((A.B/255.0f*B.B/255.0f), 255), Math.Min(A.A/255.0f*(B.A/255.0f), 255));
        }

        public static Color AdditiveBlend(Color A, Color B)
        {
            return new Color(Math.Min(A.R + B.R, 255), Math.Min(A.G + B.G, 255), Math.Min(A.B + B.B, 255),
                Math.Min(A.A + B.A, 255));
        }
    }
}