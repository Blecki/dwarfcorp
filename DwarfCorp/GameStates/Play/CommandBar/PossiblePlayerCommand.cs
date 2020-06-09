using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.Play
{
    public class PossiblePlayerCommand
    {
        public String Name;
        public Action<PossiblePlayerCommand, InputEventArgs> OnClick;
        public Func<bool> IsAvailable;
        public String Tooltip;
        public ResourceType.GuiGraphic Icon;
        public Gui.TileReference OldStyleIcon;
        public Widget HoverWidget = null;
        public Widget GuiTag = null;
        public ResourceType.GuiGraphic OperationIcon;
    }
}
