﻿// 
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

namespace LibNoise.Models
{
    /// <summary>
    /// Model that maps the output of a module onto a cylinder.
    /// </summary>
    public class Cylinder
    {
        /// <summary>
        /// The module from which to retrieve noise.
        /// </summary>
        public IModule SourceModule { get; set; }

        /// <summary>
        /// Initialises a new instance of the Cylinder class.
        /// </summary>
        /// <param name="sourceModule">The module from which to retrieve noise.</param>
        public Cylinder(IModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException("A source module must be provided.");

            SourceModule = sourceModule;
        }

        /// <summary>
        /// Returns noise mapped to the given angle and height along the cylinder.
        /// </summary>
        public double GetValue(double angle, double height)
        {
            if (SourceModule == null)
                throw new NullReferenceException("A source module must be provided.");

            double x, y, z;
            x = System.Math.Cos(angle);
            y = height;
            z = System.Math.Sin(angle);
            return SourceModule.GetValue(x, y, z);
        }
    }
}
