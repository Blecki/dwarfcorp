using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    /// <summary>
    /// Some static functions for smoothly animating things.
    /// </summary>
    public static class Easing
    {
        public static float CubeInOut(float time, float start, float dest, float duration)
        {
            time /= duration;
            time--;
            return dest * (time * time * time + 1) + start;
        }

        public static float Ballistic(float time, float duration, float height)
        {
            float t = time / duration;

            return (-((t - 0.5f) * (t - 0.5f)) + 0.25f) * height * 4.0f;
        }
    }

}