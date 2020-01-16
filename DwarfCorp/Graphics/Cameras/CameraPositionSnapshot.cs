using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CameraPositiionSnapshot
    {
        public Vector3 Target;
        public Vector3 Position;
        public int SliceLevel;
        public Matrix ViewMatrix;
    }
}