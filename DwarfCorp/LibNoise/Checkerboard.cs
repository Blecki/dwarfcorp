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
    public class Checkerboard
        : IModule
    {
        public double GetValue(double x, double y, double z)
        {
           /* x = Math.MakeInt32Range(x);
            y = Math.MakeInt32Range(y);
            z = Math.MakeInt32Range(z);*/

            int x0 = (x > 0.0 ? (int)x : (int)x - 1);
            int y0 = (y > 0.0 ? (int)y : (int)y - 1);
            int z0 = (z > 0.0 ? (int)z : (int)z - 1);

            int result = ((x0 & 1 ^ y0 & 1 ^ z0 & 1));
            if(result > 0) 
                return -1.0;
            else
                return 1.0;
        }
    }
}
