using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static class GameComponentExtensions
    {
        public static Color GetGlobalIDColor(this GameComponent Component)
        {
            int r = (int)(Component.GlobalID >> 24);
            int g = (int)((Component.GlobalID >> 16) & 0x000000FF);
            int b = (int)((Component.GlobalID >> 8) & 0x000000FF);
            int a = (int)((Component.GlobalID) & 0x000000FF);
            return new Color(r, g, b, a);
        }

        public static uint GlobalIDFromColor(Color color)
        {
            uint id = 0;
            id = id | (uint)(color.R << 24);
            id = id | (uint)(color.G << 16);
            id = id | (uint)(color.B << 8);
            id = id | (uint)(color.A);
            return id;
        }
    }
}
