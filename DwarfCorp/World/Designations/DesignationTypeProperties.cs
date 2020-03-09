using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public enum DesignationDrawType
    {
        FullBox,
        TopBox,
        PreviewVoxel,
    }

    public class DesignationTypeProperties
    {
        public String Name;
        public Color Color;
        public float LineWidth = 0.1f;
        public DesignationDrawType DrawType;
    }
}