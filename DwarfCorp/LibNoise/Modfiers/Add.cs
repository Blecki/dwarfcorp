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
    /// Module that returns the output of two source modules added together.
    /// </summary>
    public class Add
        : IModule
    {
        /// <summary>
        /// The first module from which to retrieve noise.
        /// </summary>
        public IModule SourceModule1 { get; set; }
        /// <summary>
        /// The second module from which to retrieve noise.
        /// </summary>
        public IModule SourceModule2 { get; set; }

        /// <summary>
        /// Initialises a new instance of the Add class.
        /// </summary>
        /// <param name="sourceModule1">The first module from which to retrieve noise.</param>
        /// <param name="sourceModule2">The second module from which to retrieve noise.</param>
        public Add(IModule sourceModule1, IModule sourceModule2)
        {
            if (sourceModule1 == null || sourceModule2 == null)
                throw new ArgumentNullException("Source modules must be provided.");

            SourceModule1 = sourceModule1;
            SourceModule2 = sourceModule2;
        }

        /// <summary>
        /// Returns the output of the two source modules added together.
        /// </summary>
        public double GetValue(double x, double y, double z)
        {
            if (SourceModule1 == null || SourceModule2 == null)
                throw new NullReferenceException("Source modules must be provided.");

            return SourceModule1.GetValue(x, y, z) + SourceModule2.GetValue(x, y, z);
        }
    }
}
