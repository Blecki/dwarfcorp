using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public enum SpriteOrientation
    {
        Right = 0,
        Left = 1,
        Forward = 2,
        Backward = 3
    }

    public static class SpriteOrientationHelper
    {
        public static string[] OrientationStrings =
        {
            "RIGHT",
            "LEFT",
            "FORWARD",
            "BACKWARD"
        };

        public static SpriteOrientation CalculateSpriteOrientation(Camera camera, Matrix Transform)
        {
            float xComponent = Vector3.Dot(camera.ViewMatrix.Forward, Transform.Left);
            float yComponent = Vector3.Dot(camera.ViewMatrix.Forward, Transform.Forward);

            // Todo: There should be a way to do this without trig.
            float angle = (float)Math.Atan2(yComponent, xComponent);

            if (angle > 3.0f * MathHelper.PiOver4) // 135 degrees
                return SpriteOrientation.Right;
            else if (angle > MathHelper.PiOver4) // 45 degrees
                return SpriteOrientation.Backward;
            else if (angle > -MathHelper.PiOver4) // -45 degrees
                return SpriteOrientation.Left;
            else if (angle > -3.0f * MathHelper.PiOver4) // -135 degrees
                return SpriteOrientation.Forward;
            else
                return SpriteOrientation.Right;
        }
    }
}