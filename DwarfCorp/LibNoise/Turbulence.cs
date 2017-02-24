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
    public class Turbulence
        : IModule
    {
        public IModule SourceModule { get; set; }

        public double Power { get; set; }

        private Perlin XDistort;
        private Perlin YDistort;
        private Perlin ZDistort;

        public Turbulence(IModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException();

            SourceModule = sourceModule;

            XDistort = new Perlin();
            YDistort = new Perlin();
            ZDistort = new Perlin();

            Frequency = 1.0;
            Power = 1.0;
            Roughness = 3;
            Seed = 0;
        }

        public double Frequency
        {
            get { return XDistort.Frequency; }
            set
            {
                XDistort.Frequency = YDistort.Frequency = ZDistort.Frequency = value;
            }
        }

        public double GetValue(double x, double y, double z)
        {
            if (SourceModule == null)
                throw new NullReferenceException();

            // Get the values from the three noise::module::Perlin noise modules and
            // add each value to each coordinate of the input value.  There are also
            // some offsets added to the coordinates of the input values.  This prevents
            // the distortion modules from returning zero if the (x, y, z) coordinates,
            // when multiplied by the frequency, are near an integer boundary.  This is
            // due to a property of gradient coherent noise, which returns zero at
            // integer boundaries.
            double x0, y0, z0;
            double x1, y1, z1;
            double x2, y2, z2;
            x0 = x + (12414.0 / 65536.0);
            y0 = y + (65124.0 / 65536.0);
            z0 = z + (31337.0 / 65536.0);
            x1 = x + (26519.0 / 65536.0);
            y1 = y + (18128.0 / 65536.0);
            z1 = z + (60493.0 / 65536.0);
            x2 = x + (53820.0 / 65536.0);
            y2 = y + (11213.0 / 65536.0);
            z2 = z + (44845.0 / 65536.0);
            double xDistort = x + (XDistort.GetValue(x0, y0, z0)
              * Power);
            double yDistort = y + (YDistort.GetValue(x1, y1, z1)
              * Power);
            double zDistort = z + (ZDistort.GetValue(x2, y2, z2)
              * Power);

            // Retrieve the output value at the offsetted input value instead of the
            // original input value.
            return SourceModule.GetValue(xDistort, yDistort, zDistort);
        }

        public int Roughness
        {
            get { return XDistort.OctaveCount; }
            set
            {
                XDistort.OctaveCount = YDistort.OctaveCount = ZDistort.OctaveCount = value;
            }
        }

        public int Seed
        {
            get { return XDistort.Seed; }
            set
            {
                XDistort.Seed = value;
                YDistort.Seed = value + 1;
                ZDistort.Seed = value + 2;
            }
        }
    }
}
