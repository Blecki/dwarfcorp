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
    public class ValueNoiseBasis        
    {
        private const int XNoiseGen = 1619;
        private const int YNoiseGen = 31337;
        private const int ZNoiseGen = 6971;
        private const int SeedNoiseGen = 1013;
        private const int ShiftNoiseGen = 8;

        public int IntValueNoise(int x, int y, int z, int seed)
        {
            // All constants are primes and must remain prime in order for this noise
            // function to work correctly.
            int n = (
                XNoiseGen * x
              + YNoiseGen * y
              + ZNoiseGen * z
              + SeedNoiseGen * seed)
              & 0x7fffffff;
            n = (n >> 13) ^ n;
            return (n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff;
        }

        public double ValueNoise(int x, int y, int z)
        {
            return ValueNoise(x, y, z, 0);
        }

        public double ValueNoise(int x, int y, int z, int seed)
        {
            return 1.0 - ((double)IntValueNoise(x, y, z, seed) / 1073741824.0);
        }     
    }
}
