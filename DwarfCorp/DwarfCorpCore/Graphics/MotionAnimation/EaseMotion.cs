using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class EaseMotion : MotionAnimation
    {
        public EaseMotion(float time, Matrix start, Vector3 end) :
            base(time, false)
        {
            Start = start;
            EndPos = end;
        }

        public Matrix Start { get; set; }
        public Vector3 EndPos { get; set; }

        public override Matrix GetTransform()
        {
            float t = Easing.CubeInOut(Time.CurrentTimeSeconds, 0.0f, 1.0f, Time.TargetTimeSeconds);
            Vector3 dx = (EndPos - Start.Translation)*t + Start.Translation;
            Matrix toReturn = Start;
            toReturn.Translation = dx;
            return toReturn;
        }
    }
}