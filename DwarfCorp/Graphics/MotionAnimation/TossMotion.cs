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

        public TossMotion()
        {
            
        }

        public TossMotion(float animTime, float height, Matrix start, Vector3 end) :
            base(animTime, false)
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
            dx.Y = Start.Translation.Y * (1 - t) + EndPos.Y * (t) + z;
            Matrix toReturn = Start;
            toReturn.Translation = dx;
            return toReturn;
        }
    }

    [JsonObject(IsReference = true)]
    public class BodyTossMotion : MotionAnimation
    {
        public Matrix Start { get; set; }
        public float Height { get; set; }
        public GameComponent Target { get; set; }
        public BodyTossMotion()
        {

        }

        public BodyTossMotion(float animTime, float height, Matrix start, GameComponent end) :
            base(animTime, false)
        {
            Start = start;
            Target = end;
            Height = height;
        }

        public override Matrix GetTransform()
        {
            float t = Time.CurrentTimeSeconds / Time.TargetTimeSeconds;
            float z = Easing.Ballistic(Time.CurrentTimeSeconds, Time.TargetTimeSeconds, Height);

            Vector3 dx = (Target.Position - Start.Translation) * t + Start.Translation;
            dx.Y = Start.Translation.Y * (1 - t) + Target.Position.Y * (t) + z;
            Matrix toReturn = Start;
            toReturn.Translation = dx;
            return toReturn;
        }
    }

    [JsonObject(IsReference = true)]
    public class KnockbackAnimation : MotionAnimation
    {
        public Matrix Start { get; set; }
        public Vector3 Knock { get; set; }

        public KnockbackAnimation()
        {
            
        }

        public KnockbackAnimation(float animTime,  Matrix start, Vector3 knock) :
            base(animTime, false)
        {
            Start = start;
            knock.Y *= 0.001f;
            Knock = knock;
        }

        public override Matrix GetTransform()
        {
            float len = Knock.Length();
            float z = Easing.Ballistic(Time.CurrentTimeSeconds, Time.TargetTimeSeconds, len);
            Vector3 dir = Knock;
            dir /= len;

            Vector3 dx = Start.Translation + dir*z;
            Matrix toReturn = Start;
            toReturn.Translation = dx;
            return toReturn;
        }
    }
}
