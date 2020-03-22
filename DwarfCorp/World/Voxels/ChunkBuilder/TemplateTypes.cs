using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels
{
    public enum TemplateSolidShapes
    {
        SoftCube,
        HardCube,
        LowerSlab
    }

    public enum TemplateFaceShapes
    {
        SoftSquare,
        Square,
        LowerSlab
    }
}