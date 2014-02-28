using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A simple way of applying forces to things to move them around smoothly.
    /// Very robust and easy to create. Uses a proportional (P), Integral (I) and Derivative (D)
    /// term to control the force.
    /// </summary>
    public class PIDController
    {
        public float Kp { get; set; }
        public float Kd { get; set; }
        public float Ki { get; set; }

        public Vector3 LastError { get; set; }
        public Vector3 SumError { get; set; }

        public PIDController(float kp, float kd, float ki)
        {
            Kp = kp;
            Kd = kd;
            Ki = ki;

            LastError = Vector3.Zero;
            SumError = Vector3.Zero;
        }

        public void Reset()
        {
            LastError = Vector3.Zero;
            SumError = Vector3.Zero;
        }

        public Vector3 GetOutput(float dt, Vector3 target, Vector3 position)
        {
            Vector3 toReturn = Vector3.Zero;
            Vector3 error = (target - position);
            toReturn += Kp * error;
            toReturn += Kd * (error - LastError);
            toReturn += Ki * (SumError);


            LastError = error;
            SumError += error;

            return toReturn;
        }
    }

}