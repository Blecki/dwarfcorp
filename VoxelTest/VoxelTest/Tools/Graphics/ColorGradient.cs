﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// A particular position and color in a gradient.
    /// </summary>
    public struct ColorStop
    {
        public float m_position;
        public Color m_color;
    }

    /// <summary>
    /// Used for creating colorful textures from a single value. Linearly interpolates
    /// between nearby colors.
    /// </summary>
    public class ColorGradient
    {
        public List<ColorStop> Stops { get; set; }

        public ColorGradient(Color first, Color last, int numStops)
        {
            this.Stops = new List<ColorStop>();
            Vector4 color1 = new Vector4((float) first.R / 255.0f, (float) first.G / 255.0f, (float) first.B / 255.0f, (float) first.A / 255.0f);
            Vector4 color2 = new Vector4((float) last.R / 255.0f, (float) last.G / 255.0f, (float) last.B / 255.0f, (float) last.A / 255.0f);

            Vector4 norm = color2 - color1;
            float length = norm.Length();
            float dStop = length / numStops;
            norm.Normalize();

            float currentStop = 0.0f;
            for(int i = 0; i < numStops; i++)
            {
                ColorStop stop = new ColorStop();
                Vector4 colorVec = color1 + currentStop * norm;
                stop.m_color = new Color(colorVec.X, colorVec.Y, colorVec.Y, colorVec.W);
                currentStop += dStop;
                Stops.Add(stop);
            }
        }

        public ColorGradient(List<ColorStop> stops)
        {
            this.Stops = stops;
        }

        // Convenience method to convert a byte into a color for gradients
        // with only a resolution of 255
        public Color GetColor(byte position)
        {
            if(Stops.Count < 254)
            {
                return GetColor((float) (position) / 255.0f);
            }

            if(position >= Stops.Count)
            {
                position = (byte) (Stops.Count - 1);
            }
            return GetColor((int) position);
        }

        // index into the array
        public Color GetColor(int position)
        {
            if(position >= Stops.Count)
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

            foreach(ColorStop stop in Stops)
            {
                float diff = 1.0f / ((stop.m_position - position) * (stop.m_position - position) + 0.001f);
                sumWeights += diff;

                sumColor += new Vector3(stop.m_color.R, stop.m_color.G, stop.m_color.B) * (diff);
            }

            Vector3 averageVector = sumColor / sumWeights;
            averageColor = new Color((byte) averageVector.X, (byte) averageVector.Y, (byte) averageVector.Z);
            return averageColor;
        }

        public static Color Multiply(Color A, Color B)
        {
            return new Color(Math.Min(((float) A.R / 255.0f) * ((float) B.R / 255.0f), 255), Math.Min((float) (A.G / 255.0f) * (float) (B.G / 255.0f), 255), Math.Min(((float) A.B / 255.0f * (float) B.B / 255.0f), 255), Math.Min((float) (A.A / 255.0f) * (float) (B.A / 255.0f), 255));
        }

        public static Color AdditiveBlend(Color A, Color B)
        {
            return new Color(Math.Min(A.R + B.R, 255), Math.Min(A.G + B.G, 255), Math.Min(A.B + B.B, 255), Math.Min(A.A + B.A, 255));
        }
    }

}