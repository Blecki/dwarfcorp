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

namespace LibNoise.Modifiers
{
    public class Power
        : IModule
    {
        public IModule BaseModule { get; set; }
        public IModule PowerModule { get; set; }

        public Power(IModule baseModule, IModule powerModule)
        {
            if (baseModule == null || powerModule == null)
                throw new ArgumentNullException("Base and power modules must be provided.");

            BaseModule = baseModule;
            PowerModule = powerModule;
        }

        public double GetValue(double x, double y, double z)
        {
            if (BaseModule == null || PowerModule == null)
                throw new NullReferenceException("Base and power modules must be provided.");
            return System.Math.Pow(BaseModule.GetValue(x, y, z), PowerModule.GetValue(x, y, z));
        }
    }
}
