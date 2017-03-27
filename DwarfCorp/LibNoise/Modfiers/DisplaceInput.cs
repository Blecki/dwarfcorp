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
    public class DisplaceInput
        : IModule
    {
        public IModule SourceModule { get; set; }
        public IModule XDisplaceModule { get; set; }
        public IModule YDisplaceModule { get; set; }
        public IModule ZDisplaceModule { get; set; }

        public DisplaceInput(IModule sourceModule, IModule xDisplaceModule, IModule yDisplaceModule, IModule zDisplaceModule)
        {
            if (sourceModule == null || xDisplaceModule == null || yDisplaceModule == null || zDisplaceModule == null)
                throw new ArgumentNullException("Source and X, Y, and Z displacement modules must be provided.");

            SourceModule = sourceModule;
            XDisplaceModule = xDisplaceModule;
            YDisplaceModule = yDisplaceModule;
            ZDisplaceModule = zDisplaceModule;
        }

        public double GetValue(double x, double y, double z)
        {
            if (SourceModule == null || XDisplaceModule == null || YDisplaceModule == null || ZDisplaceModule == null)
                throw new NullReferenceException("Source and X, Y, and Z displacement modules must be provided.");

            x += XDisplaceModule.GetValue(x, y, z);
            y += YDisplaceModule.GetValue(x, y, z);
            z += ZDisplaceModule.GetValue(x, y, z);

            return SourceModule.GetValue(x, y, z);
        }
    }
}
