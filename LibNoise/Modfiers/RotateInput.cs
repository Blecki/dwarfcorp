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
    public class RotateInput
        : IModule
    {
        public IModule SourceModule { get; set; }

        private double XAngle;
        private double YAngle;
        private double ZAngle;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_x1Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_x2Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_x3Matrix;


        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_y1Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_y2Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_y3Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_z1Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_z2Matrix;

        /// An entry within the 3x3 rotation matrix used for rotating the
        /// input value.
        double m_z3Matrix;

        public RotateInput(IModule sourceModule, double xAngle, double yAngle, double zAngle)
        {
            if (sourceModule == null)
                throw new ArgumentNullException("A source module must be provided.");

            SourceModule = sourceModule;
            XAngle = xAngle;
            YAngle = yAngle;
            ZAngle = zAngle;
        }

        public void SetAngles(double xAngle, double yAngle, double zAngle)
        {
            XAngle = xAngle;
            YAngle = yAngle;
            ZAngle = zAngle;

            double xCos, yCos, zCos, xSin, ySin, zSin;
            xCos = System.Math.Cos(xAngle);
            yCos = System.Math.Cos(yAngle);
            zCos = System.Math.Cos(zAngle);
            xSin = System.Math.Sin(xAngle);
            ySin = System.Math.Sin(yAngle);
            zSin = System.Math.Sin(zAngle);

            m_x1Matrix = ySin * xSin * zSin + yCos * zCos;
            m_y1Matrix = xCos * zSin;
            m_z1Matrix = ySin * zCos - yCos * xSin * zSin;
            m_x2Matrix = ySin * xSin * zCos - yCos * zSin;
            m_y2Matrix = xCos * zCos;
            m_z2Matrix = -yCos * xSin * zCos - ySin * zSin;
            m_x3Matrix = -ySin * xCos;
            m_y3Matrix = xSin;
            m_z3Matrix = yCos * xCos;
        }

        public double GetValue(double x, double y, double z)
        {
            if (SourceModule == null)
                throw new NullReferenceException("A source module must be provided.");

            double nx = (m_x1Matrix * x) + (m_y1Matrix * y) + (m_z1Matrix * z);
            double ny = (m_x2Matrix * x) + (m_y2Matrix * y) + (m_z2Matrix * z);
            double nz = (m_x3Matrix * x) + (m_y3Matrix * y) + (m_z3Matrix * z);
            return SourceModule.GetValue(nx, ny, nz);
        }
    }
}
