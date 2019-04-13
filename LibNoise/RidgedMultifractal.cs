// 
// Copyright (c) 2013 Jason Bell
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
// 

using System;

namespace LibNoise
{
    public class RidgedMultifractal
        : GradientNoiseBasis, IModule
    {
        public double Frequency { get; set; }
        public NoiseQuality NoiseQuality { get; set; }
        public int Seed { get; set; }
        private int mOctaveCount;
        private double mLacunarity;

        private const int MaxOctaves = 30;

        private double[] SpectralWeights = new double[MaxOctaves];

        public RidgedMultifractal()
        {
            Frequency = 1.0;
            Lacunarity = 2.0;
            OctaveCount = 6;
            NoiseQuality = NoiseQuality.Standard;
            Seed = 0;
        }

        public double GetValue(double x, double y, double z)
        {
            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            double signal = 0.0;
            double value = 0.0;
            double weight = 1.0;

            // These parameters should be user-defined; they may be exposed in a
            // future version of libnoise.
            double offset = 1.0;
            double gain = 2.0;

            for (int currentOctave = 0; currentOctave < OctaveCount; currentOctave++)
            {
                //double nx, ny, nz;

               /* nx = Math.MakeInt32Range(x);
                ny = Math.MakeInt32Range(y);
                nz = Math.MakeInt32Range(z);*/

                long seed = (Seed + currentOctave) & 0x7fffffff;
                signal = GradientCoherentNoise(x, y, z, 
                    (int)seed, NoiseQuality);

                // Make the ridges.
                signal = System.Math.Abs(signal);
                signal = offset - signal;

                // Square the signal to increase the sharpness of the ridges.
                signal *= signal;

                // The weighting from the previous octave is applied to the signal.
                // Larger values have higher weights, producing sharp points along the
                // ridges.
                signal *= weight;

                // Weight successive contributions by the previous signal.
                weight = signal * gain;
                if (weight > 1.0)
                {
                    weight = 1.0;
                }
                if (weight < 0.0)
                {
                    weight = 0.0;
                }

                // Add the signal to the output value.
                value += (signal * SpectralWeights[currentOctave]);

                // Go to the next octave.
                x *= Lacunarity;
                y *= Lacunarity;
                z *= Lacunarity;
            }

            return (value * 1.25) - 1.0;
        }

        public double Lacunarity
        {
            get { return mLacunarity; }
            set
            {
                mLacunarity = value;
                CalculateSpectralWeights();
            }
        }

        public int OctaveCount
        {
            get { return mOctaveCount; }
            set
            {
                if (value < 1 || value > MaxOctaves)
                    throw new ArgumentException("Octave count must be greater than zero and less than " + MaxOctaves);

                mOctaveCount = value;
            }
        }

        private void CalculateSpectralWeights()
        {
            double h = 1.0;

            double frequency = 1.0;
            for (int i = 0; i < MaxOctaves; i++)
            {
                // Compute weight for each frequency.
                SpectralWeights[i] = System.Math.Pow(frequency, -h);
                frequency *= mLacunarity;
            }
        }
    }
}
