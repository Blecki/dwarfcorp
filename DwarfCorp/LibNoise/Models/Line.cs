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

namespace LibNoise.Models
{
    /// <summary>
    /// Model that maps the output of a module onto a line.
    /// </summary>
    public class Line
    {
        /// <summary>
        /// The module from which to retrieve noise.
        /// </summary>
        public IModule SourceModule { get; set; }

        /// <summary>
        /// Specifies whether to attenuate the noise.  If true, the noise will approach 0.0
        /// as the ends of the line are approached.
        /// </summary>
        public bool Attenuate { get; set; }

        /// @a x coordinate of the start of the line segment.
        double m_x0;

        /// @a x coordinate of the end of the line segment.
        double m_x1;

        /// @a y coordinate of the start of the line segment.
        double m_y0;

        /// @a y coordinate of the end of the line segment.
        double m_y1;

        /// @a z coordinate of the start of the line segment.
        double m_z0;

        /// @a z coordinate of the end of the line segment.
        double m_z1;

        /// <summary>
        /// Initialises a new instance of the Line class.
        /// </summary>
        /// <param name="sourceModule">The module from which to retrieve noise.</param>
        public Line(IModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException("A source module must be provided.");

            SourceModule = sourceModule;

            Attenuate = true;
            m_x0 = 0.0;
            m_x1 = 1.0;
            m_y0 = 0.0;
            m_y1 = 1.0;
            m_z0 = 0.0;
            m_z1 = 1.0;
        }

        /// <summary>
        /// Returns noise mapped to the given point along the length of the line.
        /// </summary>
        public double GetValue(double p)
        {
            if (SourceModule == null)
                throw new NullReferenceException("A source module must be provided.");

            double x = (m_x1 - m_x0) * p + m_x0;
            double y = (m_y1 - m_y0) * p + m_y0;
            double z = (m_z1 - m_z0) * p + m_z0;
            double value = SourceModule.GetValue(x, y, z);

            if (Attenuate)
            {
                return p * (1.0 - p) * 4 * value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Sets the start point of the line in 3D space.
        /// </summary>
        public void SetStartPoint(double x, double y, double z)
        {
            m_x0 = x;
            m_y0 = y;
            m_z0 = z;
        }

        /// <summary>
        /// Sets the end point of the line in 3D space.
        /// </summary>
        public void SetEndPoint(double x, double y, double z)
        {
            m_x1 = x;
            m_y1 = y;
            m_z1 = z;
        }
    }
}
