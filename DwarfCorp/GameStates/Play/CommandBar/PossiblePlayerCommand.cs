using DwarfCorp.Gui;
using System;
using System.Collections.Generic;

namespace DwarfCorp.Play
{
    public class PossiblePlayerCommand
    {
        public List<String> ID;
        public String DisplayName;
        public Action<PossiblePlayerCommand, InputEventArgs> OnClick;
        public Func<bool> IsAvailable;
        public String Tooltip;
        public ResourceType.GuiGraphic Icon;
        public Gui.TileReference OldStyleIcon;
        public Widget HoverWidget = null;
        public Widget GuiTag = null;
        public ResourceType.GuiGraphic OperationIcon; // Todo: Add category icon.
    }
}
