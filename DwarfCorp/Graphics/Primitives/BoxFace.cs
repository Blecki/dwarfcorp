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

    /// <summary>
    /// Specifies one of the 6 faces of a box.
    /// </summary>
    public enum BoxFace
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        Front = 4,
        Back = 5
    }
}