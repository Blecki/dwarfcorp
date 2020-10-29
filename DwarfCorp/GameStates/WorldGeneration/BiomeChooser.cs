using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class BiomeChooser : Widget
    {
        private Overworld Settings;
        private Gui.Widgets.GridPanel BiomeList;

        public BiomeChooser(Overworld Settings) 
        {
            this.Settings = Settings;
        }

        private void CreateBiomeEntries()
        {
            foreach (var biome in Library.EnumerateBiomes().Where(b => !b.Underground))
            {
                var bar = BiomeList.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(0, 32),
                    Tag = biome
                });

                bar.AddChild(new Gui.Widgets.CheckBox
                {
                    CheckState = true,
                    AutoLayout = AutoLayout.DockLeft
                });

                bar.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill,
                    Text = biome.Name,
                    TextVerticalAlign = VerticalAlign.Center
                });
            }
        }

        private void SetCheckboxes()
        {
            foreach (var child in BiomeList.Children)
                if (child.Tag is BiomeData biome)
                {
                    if (Settings.InstanceSettings.SelectedBiomes.Any(b => b.Name == biome.Name))
                        (child.Children[0] as Gui.Widgets.CheckBox).CheckState = true;
                    else
                        (child.Children[0] as Gui.Widgets.CheckBox).CheckState = false;
                }
        }

        private void SaveBiomeSelection()
        {
            var r = new List<BiomeData>();
            foreach (var child in BiomeList.Children)
                if (child.Tag is BiomeData biome)
                    if ((child.Children[0] as Gui.Widgets.CheckBox).CheckState == true)
                        r.Add(biome);
            Settings.InstanceSettings.SelectedBiomes = r;
        }
        
        public override void Construct()
        {
            PopupDestructionType = PopupDestructionType.Keep;
            Padding = new Margin(2, 2, 2, 2);
            //Set size and center on screen.
            var center = Root.RenderData.VirtualScreen.Center;
            Rect = new Rectangle(center.X - 256, center.Y - 128, 512, 256);

            Border = "border-fancy";

            var ValidationLabel = AddChild(new Widget
            {
                MinimumSize = new Point(0, 48),
                AutoLayout = AutoLayout.DockBottom,
                Font = "font16"
            });

            ValidationLabel.AddChild(new Gui.Widget
            {
                Text = "Okay",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    SaveBiomeSelection();
                    if (Settings.InstanceSettings.SelectedBiomes.Count > 0)
                        this.Close();
                    else
                    {
                        var popup = new Gui.Widgets.Confirm()
                        {
                            Text = "Select at least 1 biome.",
                            CancelText = ""
                        };
                        Root.ShowModalPopup(popup);
                    }
                }
            });

            BiomeList = AddChild(new Gui.Widgets.GridPanel()
            {
                ItemSize = new Point(128, 32),
                ItemSpacing = new Point(2, 2),
                AutoLayout = AutoLayout.DockFill,
                EnableScrolling = false
            }) as Gui.Widgets.GridPanel;

            CreateBiomeEntries();
            SetCheckboxes();

            Layout();
            base.Construct();
        }
    }
}