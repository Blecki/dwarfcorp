using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ColorDialog : Dialog
    {
        public ColorSelector Selector { get; set; }
        private List<Color> Colors { get; set; } 

        public delegate void ColorSelected(Color arg);
        public event ColorSelector.ColorSelected OnColorSelected;

        public static ColorDialog Popup(DwarfGUI gui, List<Color> colors)
        {
            int w = gui.Graphics.Viewport.Width - 128;
            int h = gui.Graphics.Viewport.Height - 128;
            ColorDialog toReturn = new ColorDialog(gui, gui.RootComponent, colors)
            {
                LocalBounds =
                    new Rectangle(gui.Graphics.Viewport.Width / 2 - w / 2, gui.Graphics.Viewport.Height / 2 - h / 2, w, h)
            };
            toReturn.Initialize(ButtonType.Cancel, "Select Colors", "");
            
            return toReturn;
        }

        public ColorDialog(DwarfGUI gui, GUIComponent parent, List<Color> colors) 
            : base(gui, parent)
        {
            Colors = colors;
        }

        public override void Initialize(ButtonType buttons, string title, string message)
        {
            base.Initialize(buttons, title, message);
            Selector = new ColorSelector(GUI, Layout);
            Layout.LocalBounds = new Rectangle(0, 0, LocalBounds.Width, LocalBounds.Height);
            Layout.SetComponentPosition(Selector, 0, 1, 4, 2);
            Layout.UpdateSizes();
            Selector.InitializeColorPanels(Colors);
            OnColorSelected += ColorSelectedInvoker;
            Selector.OnColorSelected += ColorDialog_OnColorSelected;
        }

        private void ColorSelectedInvoker(Color arg)
        {
        }

        void ColorDialog_OnColorSelected(Color arg)
        {
            OnColorSelected.Invoke(arg);
            Close(ReturnStatus.Ok);
        }

        
    }
}
