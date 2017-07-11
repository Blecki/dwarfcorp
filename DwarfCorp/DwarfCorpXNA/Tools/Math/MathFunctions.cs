// MathFunctions.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Some static helper functions which are primarily mathematical.
    /// </summary>
    public static class MathFunctions
    {
        public static bool RandEvent(float probability)
        {
            return Rand(0, 1) < probability;
        }

        public static float Rand()
        {
            return (float) Random.NextDouble();
        }

        public static float Rand(float min, float max)
        {
            return Rand()*(max - min) + min;
        }

        public static Vector3 PeriodicRand(float time)
        {
            return new Vector3((float) Math.Cos(time + Rand()*0.1f), (float) Math.Sin(time + Rand()*0.1f),
                (float) Math.Sin(time - 0.5f + Rand()*0.1f));
        }

        public static Vector3 GetClosestPointOnLineSegment(Vector3 A, Vector3 B, Vector3 P)
        {
            Vector3 AP = P - A; //Vector from A to P   
            Vector3 AB = B - A; //Vector from A to B  

            float magnitudeAB = AB.LengthSquared(); //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector3.Dot(AP, AB); //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct/magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0) //Check if P projection is over vectorAB     
            {
                return A;
            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB*distance;
            }
        }

        /// <summary>
        /// Gets the absolute transform given a parent transform.
        /// </summary>
        /// <param Name="parentTransform">The parent transform.</param>
        /// <param Name="myTransform">Relative transform of the child.</param>
        /// <returns>The absolute transform</returns>
        public static Matrix GetAbsoluteTransform(Matrix parentTransform, Matrix myTransform)
        {
            Matrix result = parentTransform*myTransform;
            result.Translation = parentTransform.Translation + myTransform.Translation;
            return result;
        }

        /// <summary>
        /// Gets the relative transform given a parent's transform and a child's absolute transform.
        /// </summary>
        /// <param Name="parentTransform">The parent transform.</param>
        /// <param Name="absoluteTransform">The absolute transform of the child.</param>
        /// <returns>The relative transform</returns>
        public static Matrix GetRelativeTransform(Matrix parentTransform, Matrix absoluteTransform)
        {
            return absoluteTransform/parentTransform;
        }

        /// <summary>
        /// Applies a rotation around the z axis to a transform.
        /// </summary>
        /// <param Name="radians">The radians to rotate by.</param>
        /// <param Name="transform">The transform.</param>
        public static void ApplyRotation(float radians, ref Matrix transform)
        {
            Matrix result = (Matrix.CreateRotationZ(radians));
            result.Translation = transform.Translation;
            transform = result;
        }

        /// <summary>
        /// Computes a transformation matrix from an angle and a position.
        /// </summary>
        /// <param Name="angle">The angle.</param>
        /// <param Name="position">The position.</param>
        /// <returns>A new homogenous transformation matrix</returns>
        public static Matrix GetTransform(float angle, Vector2 position)
        {
            Matrix result = Matrix.CreateRotationZ(angle);
            result.Translation = new Vector3(position, 0);
            return result;
        }

        /// <summary>
        /// Gets the angle of rotation about the Z axis of a matrix.
        /// </summary>
        /// <param Name="rotationMatrix">The rotation matrix.</param>
        /// <returns>The angle of rotation about the Z axis.</returns>
        public static float GetAngle(Matrix rotationMatrix)
        {
            return (float) Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
        }



        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        public static uint Clamp(uint value, uint min, uint max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        public static byte Clamp(byte value, byte min, byte max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Clamps the specified vector to a bounding box.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>A vector inside the bounds.</returns>
        public static Vector3 Clamp(Vector3 value, BoundingBox bounds)
        {
            return new Vector3(Clamp(value.X, bounds.Min.X, bounds.Max.X), 
                               Clamp(value.Y, bounds.Min.Y, bounds.Max.Y),
                               Clamp(value.Z, bounds.Min.Z, bounds.Max.Z));
        }

        /// <summary>
        /// Clamps the specified vector to the given magnitude.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="magnitude">The magnitude.</param>
        /// <returns></returns>
        public static Vector3 Clamp(Vector3 value, float magnitude)
        {
            float norm = value.Length();
            if (norm > magnitude)
            {
                value.Normalize();
                value *= magnitude;
                return value;
            }
            return value;
        }


        /// <summary>
        /// Clamps the specified vector to a rectangle.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>A vector clamped to the rectangle bounds.</returns>
        public static Vector2 Clamp(Vector2 value, Rectangle bounds)
        {
            return new Vector2(Clamp(value.X, bounds.Left, bounds.Right), Clamp(value.Y, bounds.Top, bounds.Bottom));
        }

        /// <summary>
        /// Finds the intersection of inner and outer.
        /// </summary>
        /// <param name="inner">The inner.</param>
        /// <param name="outer">The outer.</param>
        /// <returns>A rectangle which is the intersection of inner and outer.</returns>
        public static Rectangle Clamp(Rectangle inner, Rectangle outer)
        {
            Rectangle rect = inner;
            rect.X = Clamp(inner.X, outer.X, outer.X + outer.Width);
            rect.Y = Clamp(inner.Y, outer.Y, outer.Y + outer.Height);
            rect.Width = Clamp(rect.Width, 0, outer.Width - rect.X);
            rect.Height = Clamp(rect.Height, 0, outer.Height - rect.Y);
            return rect;
        }

        /// <summary>
        /// Gets the Pitch, yaw and roll of a rotation matrix.
        /// </summary>
        /// <param Name="rotationMatrix">The rotation matrix.</param>
        /// <returns>The pitch, yaw, and roll as a vector3</returns>
        public static Vector3 PitchYawRoll(Matrix rotationMatrix)
        {
            float alpha = (float) Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
            float beta =
                (float)
                    Math.Atan2(-rotationMatrix.M31,
                        Math.Sqrt(rotationMatrix.M32*rotationMatrix.M32 + rotationMatrix.M33*rotationMatrix.M33));
            float gamma = (float) Math.Atan2(rotationMatrix.M32, rotationMatrix.M33);

            return new Vector3(beta, alpha, gamma);
        }


        /// <summary>
        /// Gets the Pitch, yaw and roll of a quaternion
        /// </summary>
        /// <param Name="q">The quaternion.</param>
        /// <returns>The pitch, yaw and roll as a vector3</returns>
        public static Vector3 PitchYawRoll(Quaternion q)
        {
            float heading = (float) Math.Atan2(2*q.Y*q.W - 2*q.X*q.Z, 1 - 2*(q.Y*q.Y) - 2*(q.Z*q.Z));
            float attitude = (float) Math.Asin(2*q.X*q.Y + 2*q.Z*q.W);
            float bank = (float) Math.Atan2(2*q.X*q.W - 2*q.Y*q.Z, 1 - 2*(q.X*q.X) - 2*(q.Z*q.Z));

            return new Vector3(bank, heading, attitude);
        }


        /// <summary>
        /// Gets the closest point to a line segement to a given point.
        /// </summary>
        /// <param Name="p">The point.</param>
        /// <param Name="a">The start of the line segment.</param>
        /// <param Name="b">The end of the line segment..</param>
        /// <param Name="lookahead">Add this much to the parametric estimate for the closest point (for pure pursuit algorithms).</param>
        /// <returns>The closest point on the line segment to the point in question.</returns>
        public static Vector3 ClosestPointToLineSegment(Vector3 p, Vector3 a, Vector3 b, float lookahead)
        {
            Vector3 normal = b - a;
            float l = normal.Length();

            if (l <= 1e-12)
            {
                return a;
            }

            normal.Normalize();
            float t = -(Vector3.Dot((a - p), (b - a))/(b - a).LengthSquared());
            t *= l;
            t += lookahead;
            if (t > l)
            {
                t = l;
            }
            if (t < 0)
            {
                t = 0;
            }
            normal *= t;
            normal += a;
            return normal;
        }

        /// <summary>
        /// Returns a random vector within the unit cube.
        /// </summary>
        /// <returns></returns>
        public static Vector3 RandVector3Cube()
        {
            return new Vector3(Rand() - 0.5f, Rand() - 0.5f, Rand() - 0.5f);
        }

        /// <summary>
        /// Returns a uniform random vector within an axis aligned box.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The max x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The max y.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxZ">The max z.</param>
        /// <returns></returns>
        public static Vector3 RandVector3Box(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            return new Vector3(Rand(minX, maxX), Rand(minY, maxY), Rand(minZ, maxZ));
        }

        /// <summary>
        /// Returns a uniform random vector within a box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <returns></returns>
        public static Vector3 RandVector3Box(BoundingBox box)
        {
            return RandVector3Box(box.Min.X, box.Max.X, box.Min.Y, box.Max.Y, box.Min.Z, box.Max.Z);
        }

        /// <summary>
        /// Returns a uniform random vector within a unit square centered on the origin.
        /// </summary>
        /// <returns></returns>
        public static Vector2 RandVector2Square()
        {
            return new Vector2(Rand() - 0.5f, Rand() - 0.5f);
        }

        /// <summary>
        /// Returns a uniform random vector within a unit circle centered on the origin.
        /// </summary>
        /// <returns></returns>
        public static Vector2 RandVector2Circle()
        {
            Vector2 toReturn;
            do
            {
                toReturn.X = Rand(-1, 1);
                toReturn.Y = Rand(-1, 1);
            } while (toReturn.Length() > 1);
            toReturn.Normalize();
            return toReturn;
        }


        /// <summary>
        /// Converts polar coordinates to a min in rectangular
        /// </summary>
        /// <param Name="theta">the angle of the polar coordinate</param>
        /// <param Name="r">the distance of the polar coordinate</param>
        /// <returns>A cartesian representation of this polar coordinate.</returns>
        public static Vector2 PolarToRectangular(float theta, float r)
        {
            return new Vector2((float) (r*Math.Cos(theta)), (float) (r*Math.Sin(theta)));
        }

        /// <summary>
        /// Converts a polar representation vector into a rectangular one.
        /// </summary>
        /// <param Name="other">The polar representation vector (angle, distance).</param>
        /// <returns>A cartesian representation of this polar coordinate</returns>
        public static Vector2 PolarToRectangular(Vector2 other)
        {
            return new Vector2((float) (other.Y*Math.Cos(other.X)), (float) (other.Y*Math.Sin(other.X)));
        }

        /// <summary>
        /// Converts a cartesian (X, Y) vector to a polar (angle, distance) vector.
        /// </summary>
        /// <param Name="other">The cartesian vector</param>
        /// <returns>A polar vector.</returns>
        public static Vector2 RectangularToPolar(Vector2 other)
        {
            return new Vector2((float) other.Length(), (float) Math.Atan2(other.Y, other.X));
        }

        /// <summary>
        /// Averages the specified vectors.
        /// </summary>
        /// <param Name="vectors">The vectors to average.</param>
        /// <returns>Average of the vectors</returns>
        public static Vector3 Average(List<Vector3> vectors)
        {
            Vector3 toReturn = vectors.Aggregate(Vector3.Zero, (current, vector) => current + vector);
            toReturn /= vectors.Count;

            return toReturn;
        }

        /// <summary>
        /// Averages the specified vectors.
        /// </summary>
        /// <param Name="vectors">The vectors to average.</param>
        /// <returns>Average of the vectors</returns>
        public static Vector2 Average(List<Vector2> vectors)
        {
            Vector2 toReturn = vectors.Aggregate(Vector2.Zero, (current, vector) => current + vector);
            toReturn /= vectors.Count;

            return toReturn;
        }

        ///<summary>
        ///Creates a rotation matrix so that the object faces another in 3D space.
        /// O is your object's position
        /// P is the position of the object to face
        /// U is the nominal "up" vector (typically Vector3.Y)
        /// Note: this does not work when O is straight below or straight above P
        /// </summary>
        public static Matrix CreateFacing(Vector3 pointToFace, Vector3 position, Vector3 u)
        {
            Vector3 d = (pointToFace - position);
            Vector3 right = Vector3.Cross(u, d);
            Vector3.Normalize(ref right, out right);
            Vector3 backwards = Vector3.Cross(right, u);
            Vector3.Normalize(ref backwards, out backwards);
            Vector3 up = Vector3.Cross(backwards, right);
            Matrix rot = new Matrix(right.X, right.Y, right.Z, 0, up.X, up.Y, up.Z, 0, backwards.X, backwards.Y,
                backwards.Z, 0, 0, 0, 0, 1);
            return rot;
        }

        /// <summary>
        /// Gets the positive root of a quadratic given by the quadratic formula.
        /// </summary>
        /// <param Name="a">The A in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param Name="b">The B in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param Name="c">The C in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <returns>The positive root of a quadratic</returns>
        public static float PositiveRootOfQuadratic(float a, float b, float c)
        {
            return (b + (float) Math.Sqrt(b*b - 4*a*c))/(2*a);
        }

        /// <summary>
        /// Gets the negative root of a quadratic given by the quadratic formula.
        /// </summary>
        /// <param Name="a">The A in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param Name="b">The B in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param Name="c">The C in x = B + SQRT(B^2 - 4AC)/2A.</param>
        public static float NegativeRootOfQuadratic(float a, float b, float c)
        {
            return (b - (float) Math.Sqrt(b*b - 4*a*c))/(2*a);
        }

        /// <summary>
        /// Gets the bounding box of the specified sphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <returns></returns>
        public static BoundingBox GetBoundingBox(BoundingSphere sphere)
        {
            return new BoundingBox(sphere.Center - new Vector3(sphere.Radius, sphere.Radius, sphere.Radius), sphere.Center + new Vector3(sphere.Radius, sphere.Radius, sphere.Radius));
        }

        /// <summary>
        /// Gets the bounds of a set of 3D points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns></returns>
        public static BoundingBox GetBoundingBox(IEnumerable<Vector3> points)
        {
            Vector3 maxPos = new Vector3(-Single.MaxValue, -Single.MaxValue, -Single.MaxValue);
            Vector3 minPos = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);

            foreach (Vector3 point in points)
            {
                minPos.X = Math.Min(minPos.X, point.X);
                minPos.Y = Math.Min(minPos.Y, point.Y);
                minPos.Z = Math.Min(minPos.Z, point.Z);
                maxPos.X = Math.Max(maxPos.X, point.X);
                maxPos.Y = Math.Max(maxPos.Y, point.Y);
                maxPos.Z = Math.Max(maxPos.Z, point.Z);
            }

            return new BoundingBox(minPos, maxPos);
        }

        public static BoundingBox GetBoundingBox(IEnumerable<IBoundedObject> objects)
        {
            return GetBoundingBox(objects.Select(item => item.GetBoundingBox()));
        }

        /// <summary>
        /// Gets the bounding box of a set of bounding boxes.
        /// </summary>
        /// <param name="boxes">The boxes.</param>
        /// <returns></returns>
        public static BoundingBox GetBoundingBox(IEnumerable<BoundingBox> boxes)
        {
            Vector3 maxPos = new Vector3(-Single.MaxValue, -Single.MaxValue, -Single.MaxValue);
            Vector3 minPos = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);

            foreach (BoundingBox box in boxes)
            {
                if (box.Min.X < minPos.X)
                {
                    minPos.X = box.Min.X;
                }

                if (box.Min.Y < minPos.Y)
                {
                    minPos.Y = box.Min.Y;
                }

                if (box.Min.Z < minPos.Z)
                {
                    minPos.Z = box.Min.Z;
                }

                if (box.Max.X > maxPos.X)
                {
                    maxPos.X = box.Max.X;
                }

                if (box.Max.Y > maxPos.Y)
                {
                    maxPos.Y = box.Max.Y;
                }

                if (box.Max.Z > maxPos.Z)
                {
                    maxPos.Z = box.Max.Z;
                }
            }

            return new BoundingBox(minPos, maxPos);
        }

        /// <summary>
        /// Gets the distance in X and Z to a 3D bounding box of a 3d point.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public static float Dist2D(BoundingBox bounds, Vector3 pos)
        {
            float dx = Math.Min(Math.Abs(pos.X - bounds.Min.X), Math.Abs(bounds.Max.X - pos.X));
            float dz = Math.Min(Math.Abs(pos.Z - bounds.Min.Z), Math.Abs(bounds.Max.Z - pos.Z));
            return Math.Min(dx, dz);
        }

        /// <summary>
        /// Gets the bounding rectangle of a set of rectangles.
        /// </summary>
        /// <param name="boxes">The boxes.</param>
        /// <returns></returns>
        public static Rectangle GetBoundingRectangle(IEnumerable<Rectangle> boxes)
        {
            Point maxPos = new Point(-Int32.MaxValue, -Int32.MaxValue);
            Point minPos = new Point(Int32.MaxValue, Int32.MaxValue);

            foreach (Rectangle box in boxes)
            {
                if (box.X <= minPos.X)
                {
                    minPos.X = box.X;
                }

                if (box.Y <= minPos.Y)
                {
                    minPos.Y = box.Y;
                }
                if (box.Right >= maxPos.X)
                {
                    maxPos.X = box.Right;
                }

                if (box.Bottom >= maxPos.Y)
                {
                    maxPos.Y = box.Bottom;
                }

            }

            return new Rectangle(minPos.X, minPos.Y, maxPos.X - minPos.X, maxPos.Y - minPos.Y);
        }


        /// <summary>
        /// Bilinearly interpolates between values in a grid.
        /// </summary>
        /// <returns></returns>
        public static float LinearCombination(float x, float y, float x1, float y1, float x2, float y2, float q11,
            float q12, float q21, float q22)
        {
            return (1.0f/((x2 - x1)*(y2 - y1)))*(q11*(x2 - x)*(y2 - y) +
                                                 q21*(x - x1)*(y2 - y) +
                                                 q12*(x2 - x)*(y - y1) +
                                                 q22*(x - x1)*(y - y1));
        }

        /// <summary>
        /// Bilinear interpolation in a 2D float map.
        /// </summary>
        /// <param name="position">The position to get the value at. Clamped to be inside the map.</param>
        /// <param name="map">The map.</param>
        /// <returns>Bilinearly interpolated value at that map position.</returns>
        public static float LinearInterpolate(Vector2 position, float[,] map)
        {
            float x = position.X;
            float y = position.Y;
            float x1 = (int) Clamp((float) Math.Ceiling(x), 0, map.GetLength(0) - 2);
            float y1 = (int) Clamp((float) Math.Ceiling(y), 0, map.GetLength(1) - 2);
            float x2 = (int) Clamp((float) Math.Floor(x), 0, map.GetLength(0) - 2);
            float y2 = (int) Clamp((float) Math.Floor(y), 0, map.GetLength(1) - 2);

            if (Math.Abs(x1 - x2) < 0.5f)
            {
                x1 = x1 + 1;
            }

            if (Math.Abs(y1 - y2) < 0.5f)
            {
                y1 = y1 + 1;
            }

            float q11 = map[(int) x1, (int) y1];
            float q12 = map[(int) x1, (int) y2];
            float q21 = map[(int) x2, (int) y1];
            float q22 = map[(int) x2, (int) y2];

            return LinearCombination(x, y, x1, y1, x2, y2, q11, q12, q21, q22);
        }


        /// <summary>
        /// Gets the distance of point p to a line segment made up vectors v and w.
        /// </summary>

        public static float PointLineDistance2D(Vector2 v, Vector2 w, Vector2 p)
        {
            Vector2 dw = w - v;
            Vector2 dp = p - v;

            // Return minimum distance between line segment vw and point p
            float l2 = dw.LengthSquared(); // i.e. |w-v|^2 -  avoid a sqrt
            if (Math.Abs(l2) < 1e-10)
            {
                return dp.LengthSquared(); // v == w case
            }

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            float t = Vector2.Dot(p - v, w - v)/l2;
            if (t < 0.0)
            {
                return dp.LengthSquared(); // Beyond the 'v' end of the segment
            }

            if (t > 1.0)
            {
                return (p - w).LengthSquared(); // Beyond the 'w' end of the segment
            }

            Vector2 projection = v + t*dw; // Projection falls on the segment
            return (p - projection).LengthSquared();
        }

        /// <summary>
        /// Returns the component-wise minimum of a specified set of vectors.
        /// </summary>
        /// <param name="vecs">The vecs.</param>
        /// <returns></returns>
        public static Vector3 Min(params Vector3[] vecs)
        {
            Vector3 toReturn = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);

            for (int i = 0; i < vecs.Length; i++)
            {
                toReturn = new Vector3(Math.Min(toReturn.X, vecs[i].X), Math.Min(toReturn.Y, vecs[i].Y),
                    Math.Min(toReturn.Z, vecs[i].Z));
            }

            return toReturn;
        }

        /// <summary>
        /// Returns the maximmum of a specified set of vectors.
        /// </summary>
        /// <param name="vecs">The vecs.</param>
        /// <returns></returns>
        public static Vector3 Max(params Vector3[] vecs)
        {
            Vector3 toReturn = new Vector3(-Single.MaxValue, -Single.MaxValue, -Single.MaxValue);

            for (int i = 0; i < vecs.Length; i++)
            {
                toReturn = new Vector3(Math.Max(toReturn.X, vecs[i].X), Math.Max(toReturn.Y, vecs[i].Y),
                    Math.Max(toReturn.Z, vecs[i].Z));
            }

            return toReturn;
        }

        /// <summary>
        /// Returns the L1 (manhattan) norm between vectors a and b.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static float L1(Vector3 a, Vector3 b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
        }

        /// <summary>
        /// Float equivalent of the modulus function.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="modulus">The modulus.</param>
        /// <returns></returns>
        private static float Mod(float value, float modulus)
        {
            return (value%modulus + modulus)%modulus;
        }

        // Find the smallest positive t such that s+t*ds is an integer.
        public static float IntBound(float s, float ds)
        {
            while (true)
            {
                // Find the smallest positive t such that s+t*ds is an integer.
                if (ds < 0)
                {
                    s = -s;
                    ds = -ds;
                }
                else
                {
                    s = Mod(s, 1);
                    // problem is now s+t*ds = 1
                    return (1 - s)/ds;
                }
            }
        }

        /// <summary>
        /// Floors the int.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        public static int FloorInt(float s)
        {
            return (int) Math.Floor(s);
        }

        /// <summary>
        /// Determines whether the specified f has nan values.
        /// </summary>
        /// <param name="f">The f.</param>
        /// <returns>
        ///   <c>true</c> if the specified f has nan; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasNan(Vector3 f)
        {
            return Single.IsNaN(f.X) || Single.IsNaN(f.Y) || Single.IsNaN(f.Z);
        }

        /// <summary>
        /// Rasterizes the line, producing a list of Point3's that intersect the line segment.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns></returns>
        public static IEnumerable<Point3> RasterizeLine(Vector3 start, Vector3 end)
        {
            // From "A Fast DestinationVoxel Traversal Algorithm for Ray Tracing"
            // by John Amanatides and Andrew Woo, 1987
            // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
            // <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
            // Extensions to the described algorithm:
            //   • Imposed a distance limit.
            //   • The face passed through to reach the current cube is provided to
            //     the callback.

            // The foundation of this algorithm is a parameterized representation of
            // the provided ray,
            //                    origin + t * direction,
            // except that t is not actually stored; rather, at any given point in the
            // traversal, we keep track of the *greater* t values which we would have
            // if we took a step sufficient to cross a cube boundary along that axis
            // (i.e. change the integer part of the coordinate) in the variables
            // tMaxX, tMaxY, and tMaxZ.

            // Cube containing origin point.
            var x = start.X;
            var y = start.Y;
            var z = start.Z;
            Vector3 direction = new Vector3(end.X, end.Y, end.Z) - new Vector3(start.X, start.Y, start.Z);

            if (L1(start, end) < 1e-12 || HasNan(start) || HasNan(end))
            {
                yield break;
            }

            float d1 = direction.Length();

            direction.Normalize();
            // Break out direction vector.
            var dx = direction.X;
            var dy = direction.Y;
            var dz = direction.Z;
            // Direction to increment x,y,z when stepping.
            var stepX = Math.Sign(dx);
            var stepY = Math.Sign(dy);
            var stepZ = Math.Sign(dz);
            // See description above. The initial values depend on the fractional
            // part of the origin.
            var tMaxX = IntBound(x, dx);
            var tMaxY = IntBound(y, dy);
            var tMaxZ = IntBound(z, dz);
            // The change in t when taking a step (always positive).
            var tDeltaX = stepX/dx;
            var tDeltaY = stepY/dy;
            var tDeltaZ = stepZ/dz;
            Vector3 curr = new Vector3(x, y, z);
            while (true)
            {
                curr.X = x;
                curr.Y = y;
                curr.Z = z;
                float len = (curr - end).Length();
                yield return new Point3(FloorInt(x), FloorInt(y), FloorInt(z));
                if (FloorInt(x) == FloorInt(end.X) && FloorInt(y) == FloorInt(end.Y) && FloorInt(z) == FloorInt(end.Z)) yield break;
                if (len > d1 * 1.1f) yield break;
                // tMaxX stores the t-value at which we cross a cube boundary along the
                // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
                // chooses the closest cube boundary. Only the first case of the four
                // has been commented in detail.
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        // Update which cube we are now in.
                        x += stepX;
                        // Adjust tMaxX to the next X-oriented boundary crossing.
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        y += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        // Identical to the second case, repeated for simplicity in
                        // the conditionals.
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
            }
        }

        /// <summary>
        /// Clamps a rectangle given by its minimum and max to an outer rectangle, while preserving the rectangle's size.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="outer">The outer.</param>
        /// <returns></returns>
        public static Rectangle SnapRect(Vector2 min, Vector2 max, Rectangle outer)
        {
            Rectangle inner = new Rectangle((int)min.X, (int)min.Y, (int)max.X, (int)max.Y);
            return SnapRect(inner, outer);
        }

        public static Rectangle SnapRect(Rectangle inner, Rectangle outer)
        {
            return new Rectangle(Clamp(inner.X, outer.X, outer.Right - inner.Width), Clamp(inner.Y, outer.Y, outer.Bottom - inner.Height), inner.Width, inner.Height);
        }

        /// <summary>
        /// Linearly interpolate between a and b.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="t">Scalar between 0 and 1.</param>
        /// <returns></returns>
        public static float Lerp(float a, float b, float t)
        {
            return t*b + (1 - t)*a;
        }

        /// <summary>
        /// Linearly interpolate a rectangle.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="t">The t scalar (between 0 and 1).</param>
        /// <returns></returns>
        public static Rectangle Lerp(Rectangle start, Rectangle end, float t)
        {
            return new Rectangle((int)Lerp(start.X, end.X, t), (int)Lerp(start.Y, end.Y, t), (int)Lerp(start.Width, end.Width, t), (int)Lerp(start.Height, end.Height, t));
        }

        /// <summary>
        /// Creates a random transform within the given bounding box. Just uses Euler angles...
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <returns></returns>
        public static Matrix RandomTransform(BoundingBox bounds)
        {
            Matrix tf = Matrix.Identity;
            tf *= Matrix.CreateRotationX(Rand((float)-Math.PI, (float)Math.PI));
            tf *= Matrix.CreateRotationY(Rand((float)-Math.PI, (float)Math.PI));
            tf *= Matrix.CreateRotationZ(Rand((float)-Math.PI, (float)Math.PI));
            tf.Translation = RandVector3Box(bounds);
            return tf;
        }

        /// <summary>
        /// Returns a random integer between min and max.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns></returns>
        public static int RandInt(int min, int max)
        {
            return Random.Next(min, max);
        }

        /// <summary>
        /// Gets or sets the static random seed shared by all functions using this random generator..
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public static int Seed { get; set; }
        /// <summary>
        /// The random generator used by everything in the game. Why does it have the same seed everywhere?
        /// so that randomly generated worlds are exactly the same. Even though this is called "ThreadSafe"
        /// it should probably NOT be called from threads other than the main thread to maintain seed consistency.
        /// </summary>
        public static ThreadSafeRandom Random = new ThreadSafeRandom();

        public static Vector3 ProjectOutOfHalfPlane(Vector3 position, Vector3 origin, float dist)
        {
            float dy = position.Y - origin.Y;
            if (dy < dist)
            {
                return new Vector3(position.X, origin.Y + dist, position.Z);
            }
            return position;
        }

        public static Vector3 ProjectOutOfCylinder(Vector3 position, Vector3 origin, float radius)
        {
            Vector3 pos2d = new Vector3(position.X - origin.X, 0, position.Z - origin.Z);
            bool isInCylinder = pos2d.Length() < radius;

            if (isInCylinder)
            {
                pos2d.Normalize();
                pos2d *= radius;
                return new Vector3(origin.X + pos2d.X, position.Y, origin.Z + pos2d.Z);
            }
            return position;
        }

        public static Vector3 ProjectToSphere(Vector3 position, float radius, Vector3 target)
        {
            Vector3 normDiff = position - target;
            normDiff.Normalize();
            normDiff *= radius;
            return normDiff + target;
        }

        public static Vector2 LinearToSpherical(Vector3 position, float radius, Vector3 target)
        {
            Vector3 p = position - target;
            return new Vector2((float)Math.Atan2(p.Y, p.X), (float)Math.Acos(p.Z / radius));
        }

        public static Vector3 ClampXZ(Vector3 velocity, float f)
        {
            Vector2 xz = new Vector2(velocity.X, velocity.Z);
            if (xz.LengthSquared() > f*f)
            {
                xz.Normalize();
                xz *= f;
            }
            return new Vector3(xz.X, velocity.Y, xz.Y);
        }

        public static T RandEnum<T>()
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(MathFunctions.RandInt(0, values.Length));
        }

        public static float RandNormalDist(float mean, float stdDev)
        {
            // Box-mueller transform
            double u1 = Rand(0, 1); 
            double u2 = Rand(0, 1);
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal); 
        }
    }
}