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

    /// <summary>
    /// Module that blends the output of two source modules using the output
    /// of an weight module as the blending weight.
    /// </summary>
    public class Blend
        : Math, IModule
    {
        /// <summary>
        /// The first module from which to retrieve noise to be blended.
        /// </summary>
        public IModule SourceModule1 { get; set; }
        /// <summary>
        /// The second module from which to retrieve noise to be blended.
        /// </summary>
        public IModule SourceModule2 { get; set; }
        /// <summary>
        /// The module from which to retrieve noise to be used as the blending weight.
        /// </summary>
        public IModule WeightModule { get; set; }

        /// <summary>
        /// Initialises a new instance of the Blend class.
        /// </summary>
        /// <param name="sourceModule1">The first module from which to retrieve noise to be blended.</param>
        /// <param name="sourceModule2">The second module from which to retrieve noise to be blended.</param>
        /// <param name="weightModule">The module from which to retrieve noise to be used as the blending weight.</param>
        public Blend(IModule sourceModule1, IModule sourceModule2, IModule weightModule)
        {
            if (sourceModule1 == null || sourceModule2 == null || weightModule == null)
                throw new ArgumentNullException();

            SourceModule1 = sourceModule1;
            SourceModule2 = sourceModule2;
            WeightModule = weightModule;
        }

        /// <summary>
        /// Returns the result of blending the output of the two source modules using the 
        /// output of the weight module as the blending weight.
        /// </summary>
        public double GetValue(double x, double y, double z)
        {
            if (SourceModule1 == null || SourceModule2 == null || WeightModule == null)
                throw new NullReferenceException();

            return LinearInterpolate(SourceModule1.GetValue(x, y, z),
                SourceModule2.GetValue(x, y, z),
                (WeightModule.GetValue(x, y, z) + 1.0) / 2.0);
        }
    }
}
