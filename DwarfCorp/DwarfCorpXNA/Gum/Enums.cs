using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Gum
{
    public enum HorizontalAlign
    {
        Left,
        Right,
        Center
    }

    public enum VerticalAlign
    {
        Top,
        Bottom,
        Center
    }

    public enum AutoLayout
    {
        None,
        DockTop,
        DockRight,
        DockBottom,
        DockLeft,
        DockFill,
        FloatCenter,
        FloatTop,
        FloatRight,
        FloatBottom,
        FloatLeft,
        FloatTopRight,
        FloatTopLeft,
        FloatBottomRight,
        FloatBottomLeft,
    }

}