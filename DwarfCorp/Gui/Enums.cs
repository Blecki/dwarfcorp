using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp.Gui
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
        Center,
        Below
    }

    public enum AutoLayout
    {
        None,
        DockTop,
        DockTopCentered,
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
        DockLeftCentered,
        DockRightCentered,
    }

    public enum PopupDestructionType
    {
        Keep,
        DestroyOnOffClick,
    }

}