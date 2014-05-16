using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class TossMotion : MotionAnimation
    {
        public Matrix Start { get; set; }
        public float Height { get; set; }
        public Vector3 EndPos { get; set; }

        public TossMotion(float time, float height, Matrix start, Vector3 end) :
            base(time, false)
        {
            Start = start;
            EndPos = end;
            Height = height;
        }

        public override Matrix GetTransform()
        {
            float t = Time.CurrentTimeSeconds / Time.TargetTimeSeconds;
            float z = Easing.Ballistic(Time.CurrentTimeSeconds, Time.TargetTimeSeconds, Height);

            Vector3 dx = (EndPos - Start.Translation) * t + Start.Translation;
            dx.Y = Start.Translation.Y + z;
            Matrix toReturn = Start;
            toReturn.Translation = dx;
            return toReturn;
        }
    }
}
