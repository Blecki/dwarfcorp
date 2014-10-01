using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ColorSelector : GUIComponent
    {
        public GridLayout Layout { get; set; }
        public int PanelWidth = 32;
        public int PanelHeight = 32;
        public Color CurrentColor { get; set; }
        public delegate void ColorSelected(Color arg);

        public event ColorSelected OnColorSelected;


        public ColorSelector(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            OnColorSelected += ColorSelector_OnColorSelected;
        }

        void ColorSelector_OnColorSelected(Color arg)
        {
           
        }

        public void InitializeColorPanels(List<Color> colors)
        {
            Layout = new GridLayout(GUI, this, GlobalBounds.Height / PanelHeight, GlobalBounds.Width / PanelWidth);

            int rc = Math.Max((int)(Math.Sqrt(colors.Count)), 2);

            for (int i = 0; i < colors.Count; i++)
            {
                ColorPanel panel = new ColorPanel(GUI, Layout)
                {
                    CurrentColor = colors[i]
                };

                int row = i / rc;
                int col = i % rc;
                panel.OnClicked += () => panel_OnClicked(panel.CurrentColor);

                Layout.SetComponentPosition(panel, col, row, 1, 1);
            }
        }

        void panel_OnClicked(Color color)
        {
            CurrentColor = color;
            OnColorSelected.Invoke(color);
        }
    }
}
