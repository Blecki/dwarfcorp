using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System;

namespace DwarfCorp.Gui.Widgets
{
    public class ColorOptionsEditor : Widget
    {
        private string _colorProfile = "Default";
        private GameSettings.Settings _settings = null;
        public GameSettings.Settings Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
                Invalidate();
            }
        }


        private IEnumerable<Color> EnumerateDefaultColors()
        {
            for (int h = 0; h < 255; h += 16)
                for (int v = 64; v < 255; v += 64)
                    for (int s = 128; s < 255; s += 32)
                        yield return new HSLColor((float)h, (float)s, (float)v);
        }

        public override void Construct()
        {
            ResetState();
            Padding = new Margin(2, 2, 2, 2);
            base.Construct();
        }

        public void ResetState()
        {
            Clear();
            var list = AddChild(new WidgetListView()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(550, 400),
            }) as WidgetListView;

            foreach(var color in _settings.Colors.Colors)
            {
                var row = list.AddChild(new Widget()
                {
                    Background = new TileReference("basic", 1),
                    MinimumSize = new Point(640, 32)
                });
                var tile = row.AddChild(new Widget
                {
                    Background = new TileReference("basic", 1),
                    BackgroundColor = color.Value.ToVector4(),
                    MinimumSize = new Point(30, 30),
                    MaximumSize = new Point(30, 30),
                    AutoLayout = AutoLayout.DockLeftCentered,
                });
                var label = row.AddChild(new Widget()
                {
                    Text = " " + color.Key,
                    AutoLayout = AutoLayout.DockLeft,
                    TextVerticalAlign = VerticalAlign.Center,
                });

                row.OnClick = (sender, args) =>
                {
                    var chooser = new Gui.Widgets.GridChooser
                    {
                        ItemSize = new Point(16, 16),
                        ItemSpacing = new Point(4, 4),
                        ItemSource = EnumerateDefaultColors()
                            .Select(c => new Widget
                            {
                                Background = new TileReference("basic", 1),
                                BackgroundColor = new Vector4(c.ToVector3(), 1),
                            }),
                        OnClose = (s2) =>
                        {
                            var gc = s2 as Gui.Widgets.GridChooser;
                            if (gc.DialogResult == Gui.Widgets.GridChooser.Result.OKAY &&
                                gc.SelectedItem != null)
                            {
                                tile.BackgroundColor = gc.SelectedItem.BackgroundColor;
                                tile.Invalidate();
                                _settings.Colors.SetColor(color.Key, new Microsoft.Xna.Framework.Color(gc.SelectedItem.BackgroundColor));
                            }
                        },
                        PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                    };
                    Root.ShowModalPopup(chooser);
                };

                label.OnClick = row.OnClick;
                tile.OnClick = row.OnClick;

            }

            var buttonRow = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(550, 32)
            });

            var okButton = buttonRow.AddChild(new Button()
            {
                Font = "font10",
                AutoLayout = AutoLayout.DockRight,
                Text = Library.GetString("okay"),
                OnClick = (sender, args) =>
                {
                    Close();
                }
            });

            buttonRow.AddChild(new Widget()
            {
                Text = Library.GetString("color-profile"),
                Font = "font10",
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 32)
            });

            var profiles = buttonRow.AddChild(new ComboBox()
            {
                TextColor = new Vector4(0, 0, 0, 1),
                AutoLayout = AutoLayout.DockLeft,
                Font = "font10",
                MinimumSize = new Point(128, 32),
                Items = ColorSettings.Profiles.Keys.ToList(),
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.Colors = ColorSettings.Profiles[(sender as ComboBox).SelectedItem].Clone();
                    _colorProfile = (sender as ComboBox).SelectedItem;
                    ResetState();
                }

            }) as ComboBox;

            profiles.Text = _colorProfile;

            Layout();
        }
    }
}