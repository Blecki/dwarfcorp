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
using System.Collections.Generic;

namespace LibNoise.Modifiers
{
    public class Terrace
        : Math, IModule
    {
        public IModule SourceModule { get; set; }
        public List<double> ControlPoints = new List<double>();
        public bool InvertTerraces { get; set; }

        public Terrace(IModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException("A source module must be provided.");

            SourceModule = sourceModule;

            InvertTerraces = false;
        }

        public double GetValue(double x, double y, double z)
        {
            if (SourceModule == null)
                throw new NullReferenceException("A source module must be provided.");
            if (ControlPoints.Count < 2)
                throw new Exception("Two or more control points must be specified.");

            // Get the output value from the source module.
            double sourceModuleValue = SourceModule.GetValue(x, y, z);

            int controlPointCount = ControlPoints.Count;

            // Find the first element in the control point array that has a value
            // larger than the output value from the source module.
            int indexPos;
            for (indexPos = 0; indexPos < controlPointCount; indexPos++)
            {
                if (sourceModuleValue < ControlPoints[indexPos])
                {
                    break;
                }
            }

            // Find the two nearest control points so that we can map their values
            // onto a quadratic curve.
            int index0 = Math.ClampValue(indexPos - 1, 0, controlPointCount - 1);
            int index1 = Math.ClampValue(indexPos, 0, controlPointCount - 1);

            // If some control points are missing (which occurs if the output value from
            // the source module is greater than the largest value or less than the
            // smallest value of the control point array), get the value of the nearest
            // control point and exit now.
            if (index0 == index1)
            {
                return ControlPoints[index1];
            }

            // Compute the alpha value used for linear interpolation.
            double value0 = ControlPoints[index0];
            double value1 = ControlPoints[index1];
            double alpha = (sourceModuleValue - value0) / (value1 - value0);
            if (InvertTerraces)
            {
                alpha = 1.0 - alpha;
                Math.SwapValues(ref value0, ref value1);
            }

            // Squaring the alpha produces the terrace effect.
            alpha *= alpha;

            // Now perform the linear interpolation given the alpha value.
            return LinearInterpolate(value0, value1, alpha);
        }
    }
}
