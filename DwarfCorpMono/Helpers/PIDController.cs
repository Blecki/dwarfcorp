using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class PIDController
    {
        public float KP { get; set; }
        public float KD { get; set; }
        public float KI { get; set; }
        
        public Vector3 LastError { get; set; }
        public Vector3 SumError { get; set; }

        public PIDController(float kp, float kd, float ki)
        {
            KP = kp;
            KD = kd;
            KI = ki;

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
            toReturn += KP * error;
            toReturn += KD * (error - LastError);
            toReturn += KI * (SumError);


            LastError = error;
            SumError += error;

            return toReturn;
        }
    }
}
