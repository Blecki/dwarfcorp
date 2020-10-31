using DwarfCorp.Gui;
using System;
using System.Collections.Generic;

namespace DwarfCorp.Play
{
    public class CommandMenuItem
    {
        public enum CommandMenuItemTypes
        {
            Category,
            Leaf
        }

        public List<String> ID;
        public String DisplayName;
        public Action<CommandMenuItem, InputEventArgs> OnClick;
        public Func<bool> IsAvailable;
        public String Tooltip;
        public ResourceType.GuiGraphic Icon;
        public Gui.TileReference OldStyleIcon;
        public Widget HoverWidget = null;
        public Widget GuiTag = null;
        public ResourceType.GuiGraphic OperationIcon; // Todo: Add category icon.
        public bool EnableHotkeys = true;
    }
}
