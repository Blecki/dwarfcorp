using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    class LinearMathHelpers
    {
        public static Vector3 GetClosestPointOnLineSegment(Vector3 A, Vector3 B, Vector3 P)
        {
            Vector3 AP = P - A;       //Vector from A to P   
            Vector3 AB = B - A;       //Vector from A to B  

            float magnitudeAB = AB.LengthSquared();     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector3.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;

            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }

        /// <summary>
        /// Gets the absolute transform given a parent transform.
        /// </summary>
        /// <param name="parentTransform">The parent transform.</param>
        /// <param name="myTransform">Relative transform of the child.</param>
        /// <returns>The absolute transform</returns>
        public static Matrix GetAbsoluteTransform(Matrix parentTransform, Matrix myTransform)
        {
            Matrix result = parentTransform * myTransform;
            result.Translation = parentTransform.Translation + myTransform.Translation;
            return result;
        }

        /// <summary>
        /// Gets the relative transform given a parent's transform and a child's absolute transform.
        /// </summary>
        /// <param name="parentTransform">The parent transform.</param>
        /// <param name="absoluteTransform">The absolute transform of the child.</param>
        /// <returns>The relative transform</returns>
        public static Matrix GetRelativeTransform(Matrix parentTransform, Matrix absoluteTransform)
        {
            return absoluteTransform / parentTransform;
        }

        /// <summary>
        /// Applies a rotation around the z axis to a transform.
        /// </summary>
        /// <param name="radians">The radians to rotate by.</param>
        /// <param name="transform">The transform.</param>
        public static void ApplyRotation(float radians, ref Matrix transform)
        {
            Matrix result = (Matrix.CreateRotationZ(radians));
            result.Translation = transform.Translation;
            transform = result;
        }

        /// <summary>
        /// Computes a transformation matrix from an angle and a position.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <param name="position">The position.</param>
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
        /// <param name="rotationMatrix">The rotation matrix.</param>
        /// <returns>The angle of rotation about the Z axis.</returns>
        public static float GetAngle(Matrix rotationMatrix)
        {
            return (float)Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
        }

        /// <summary>
        /// DEPRECATED. DOES NOTHING.
        /// </summary>
        public static float NormalRelativeAngle(float angle)
        {
            return angle;
        }

        /// <summary>
        /// DEPRECATED. DOES NOTHING.
        /// </summary>
        public static float NormalRelativeAngleDegrees(float angle)
        {
            return angle;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>Returns the clamped value.</returns>
        public static float Clamp(float value, float min, float max)
        {
            // Clamp the value be the min and max values.
            value = value > max ? max : value;
            value = value < min ? min : value;

            // Return the clamped value.
            return value;
        }

        /// <summary>
        /// Gets the Pitch, yaw and roll of a rotation matrix.
        /// </summary>
        /// <param name="rotationMatrix">The rotation matrix.</param>
        /// <returns>The pitch, yaw, and roll as a vector3</returns>
        public static Vector3 PitchYawRoll(Matrix rotationMatrix)
        {
            float alpha = (float)Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
            float beta = (float)Math.Atan2(-rotationMatrix.M31, Math.Sqrt(rotationMatrix.M32 * rotationMatrix.M32 + rotationMatrix.M33 * rotationMatrix.M33));
            float gamma = (float)Math.Atan2(rotationMatrix.M32, rotationMatrix.M33);

            return new Vector3(beta, alpha, gamma);
        }


        /// <summary>
        /// Gets the Pitch, yaw and roll of a quaternion
        /// </summary>
        /// <param name="q">The quaternion.</param>
        /// <returns>The pitch, yaw and roll as a vector3</returns>
        public static Vector3 PitchYawRoll(Quaternion q)
        {

            float heading = (float)Math.Atan2(2 * q.Y * q.W - 2 * q.X * q.Z, 1 - 2 * (q.Y * q.Y) - 2 * (q.Z * q.Z));
            float attitude = (float)Math.Asin(2 * q.X * q.Y + 2 * q.Z * q.W);
            float bank = (float)Math.Atan2(2 * q.X * q.W - 2 * q.Y * q.Z, 1 - 2 * (q.X * q.X) - 2 * (q.Z * q.Z));

            return new Vector3(bank, heading, attitude);
        }


        /// <summary>
        /// Gets the closest point to a line segement to a given point.
        /// </summary>
        /// <param name="P">The point.</param>
        /// <param name="A">The start of the line segment.</param>
        /// <param name="B">The end of the line segment..</param>
        /// <param name="lookahead">Add this much to the parametric estimate for the closest point (for pure pursuit algorithms).</param>
        /// <returns>The closest point on the line segment to the point in question.</returns>
        public static Vector3 ClosestPointToLineSegment(Vector3 P, Vector3 A, Vector3 B, float lookahead)
        {
            Vector3 normal = B - A;
            float l = normal.Length();
            normal.Normalize();
            float t = -(Vector3.Dot((A - P), (B - A)) / (B - A).LengthSquared());
            t *= l;
            t += lookahead;
            if (t > l) t = l;
            if (t < 0) t = 0;
            normal *= t;
            normal += A;
            return normal;
        }

        public static Vector3 RandVector3Cube()
        {
            return new Vector3((float)PlayState.random.NextDouble() - 0.5f,
                               (float)PlayState.random.NextDouble() - 0.5f,
                               (float)PlayState.random.NextDouble() - 0.5f);
        }

        public static Vector2 RandVector2Square()
        {
            return new Vector2((float)PlayState.random.NextDouble() - 0.5f, (float)PlayState.random.NextDouble() - 0.5f);
        }

        public static Vector2 RandVector2Circle()
        {
            return PolarToRectangular((float)(PlayState.random.NextDouble() * Math.PI * 2), 1);
        }


        /// <summary>
        /// Converts polar coordinates to a vector2 in rectangular
        /// </summary>
        /// <param name="theta">the angle of the polar coordinate</param>
        /// <param name="r">the distance of the polar coordinate</param>
        /// <returns>A cartesian representation of this polar coordinate.</returns>
        public static Vector2 PolarToRectangular(float theta, float r)
        {
            return new Vector2((float)(r * Math.Cos(theta)), (float)(r * Math.Sin(theta)));
        }

        /// <summary>
        /// Converts a polar representation vector into a rectangular one.
        /// </summary>
        /// <param name="other">The polar representation vector (angle, distance).</param>
        /// <returns>A cartesian representation of this polar coordinate</returns>
        public static Vector2 PolarToRectangular(Vector2 other)
        {
            return new Vector2((float)(other.Y * Math.Cos(other.X)), (float)(other.Y * Math.Sin(other.X)));
        }

        /// <summary>
        /// Converts a cartesian (X, Y) vector to a polar (angle, distance) vector.
        /// </summary>
        /// <param name="other">The cartesian vector</param>
        /// <returns>A polar vector.</returns>
        public static Vector2 RectangularToPolar(Vector2 other)
        {
            return new Vector2((float)other.Length(), (float)Math.Atan2(other.Y, other.X));
        }

        /// <summary>
        /// Averages the specified vectors.
        /// </summary>
        /// <param name="vectors">The vectors to average.</param>
        /// <returns>Average of the vectors</returns>
        public static Vector3 Average(List<Vector3> vectors)
        {
            Vector3 toReturn = Vector3.Zero;
            foreach (Vector3 vector in vectors)
            {
                toReturn += vector;
            }
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
        public static Matrix CreateFacing(Vector3 PointToFace, Vector3 Position, Vector3 U)
        {
            Vector3 D = (PointToFace - Position);
            Vector3 Right = Vector3.Cross(U, D);
            Vector3.Normalize(ref Right, out Right);
            Vector3 Backwards = Vector3.Cross(Right, U);
            Vector3.Normalize(ref Backwards, out Backwards);
            Vector3 Up = Vector3.Cross(Backwards, Right);
            Matrix rot = new Matrix(Right.X, Right.Y, Right.Z, 0, Up.X, Up.Y, Up.Z, 0, Backwards.X, Backwards.Y, Backwards.Z, 0, 0, 0, 0, 1);
            return rot;
        }
        /// <summary>
        /// Gets the positive root of a quadratic given by the quadratic formula.
        /// </summary>
        /// <param name="A">The A in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param name="B">The B in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param name="C">The C in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <returns>The positive root of a quadratic</returns>
        public static float PositiveRootOfQuadratic(float A, float B, float C)
        {
            return (B + (float)Math.Sqrt(B * B - 4 * A * C)) / (2 * A);
        }

        /// <summary>
        /// Gets the negative root of a quadratic given by the quadratic formula.
        /// </summary>
        /// <param name="A">The A in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param name="B">The B in x = B + SQRT(B^2 - 4AC)/2A.</param>
        /// <param name="C">The C in x = B + SQRT(B^2 - 4AC)/2A.</param>
        public static float NegativeRootOfQuadratic(float A, float B, float C)
        {
            return (B - (float)Math.Sqrt(B * B - 4 * A * C)) / (2 * A);
        }

        public static BoundingBox GetBoundingBox(List<BoundingBox> boxes)
        {
            Vector3 maxPos = new Vector3(-999999, -9999999, -9999999);
            Vector3 minPos = new Vector3(9999999, 99999999, 9999999);

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


        public static float LinearCombination(float x, float y, float x1, float y1, float x2, float y2, float q11, float q12, float q21, float q22)
        {
            return (1.0f / ((x2 - x1) * (y2 - y1))) * (q11 * (x2 - x) * (y2 - y) +
                                           q21 * (x - x1) * (y2 - y) +
                                           q12 * (x2 - x) * (y - y1) +
                                           q22 * (x - x1) * (y - y1));
        }

        public static float LinearInterpolate(Vector2 position, float[,] map)
        {
            float x = position.X;
            float y = position.Y;
            float x1 = (int)Clamp((float)Math.Ceiling(x), 0, map.GetLength(0) - 2);
            float y1 = (int)Clamp((float)Math.Ceiling(y), 0, map.GetLength(1) - 2);
            float x2 = (int)Clamp((float)Math.Floor(x), 0, map.GetLength(0) - 2);
            float y2 = (int)Clamp((float)Math.Floor(y), 0, map.GetLength(1) - 2);

            if (Math.Abs(x1 - x2) < 0.5f)
            {
                x1 = x1 + 1;
            }

            if (Math.Abs(y1 - y2) < 0.5f)
            {
                y1 = y1 + 1;
            }


            float q11 = map[(int)x1, (int)y1];
            float q12 = map[(int)x1, (int)y2];
            float q21 = map[(int)x2, (int)y1];
            float q22 = map[(int)x2, (int)y2];

            return LinearCombination(x, y, x1, y1, x2, y2, q11, q22, q21, q22);

        }


        public static float PointLineDistance2D(Vector2 v, Vector2 w, Vector2 p)
        {
            // Return minimum distance between line segment vw and point p
            float l2 = (w - v).LengthSquared();  // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) return (p - v).LengthSquared();   // v == w case

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            float t = Vector2.Dot(p - v, w - v) / l2;
            if (t < 0.0) return (p - v).LengthSquared();       // Beyond the 'v' end of the segment
            else if (t > 1.0) return (p - w).LengthSquared();  // Beyond the 'w' end of the segment
            Vector2  projection = v + t * (w - v);  // Projection falls on the segment
            return (p - projection).LengthSquared();
        }
    }
}
