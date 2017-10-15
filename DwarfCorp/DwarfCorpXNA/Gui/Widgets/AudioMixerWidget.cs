using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class AudioMixerWidget : Widget
    {
        public override void Construct()
        {

            var bottomBar = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(Rect.Width - 128, 24)
            })
            ;

            bottomBar.AddChild(new Button()
            {
                Text = "Back",
                OnClick = (sender, args) => this.Close(),
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 24)
            });


            if (SoundManager.Mixer == null)
            {
                Text = "Error. Failed to load audio mixer :(";
                return;
            }

            bottomBar.AddChild(new Button()
            {
                Text = "Save",
                OnClick = (sender, args) => FileUtils.SaveBasicJson(SoundManager.Mixer, ContentPaths.mixer),
                AutoLayout = AutoLayout.DockRight,
                MinimumSize = new Point(64, 24)
            });


            var listView = AddChild(new WidgetListView()
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(Rect.Width - 128, 512),
                ItemHeight = 24
            });

            foreach (var level in SoundManager.Mixer.Gains)
            {

                var row = listView.AddChild(new Widget()
                {
                    AutoLayout = AutoLayout.DockTop,
                    MinimumSize = new Point(512, 24)
                });

                row.AddChild(new Widget()
                {
                    Text = level.Key,
                    TextColor = Color.Black.ToVector4(),
                    AutoLayout = AutoLayout.DockLeft,
                    TextVerticalAlign = VerticalAlign.Center,
                    TextHorizontalAlign = HorizontalAlign.Right,
                    MinimumSize = new Point(256, 24)
                });

                KeyValuePair<string, SFXMixer.Levels> level1 = level;
                row.AddChild(new HorizontalFloatSlider()
                {
                    ScrollArea = 1.0f,
                    ScrollPosition = level.Value.Volume,
                    MinimumSize = new Point(256, 24),
                    AutoLayout = AutoLayout.DockLeft,
                    OnSliderChanged = (sender) =>
                    {
                        SFXMixer.Levels levels = SoundManager.Mixer.GetOrCreateLevels(level1.Key);
                        SoundManager.Mixer.SetLevels(level1.Key,
                            new SFXMixer.Levels()
                            {
                                RandomPitch = level1.Value.RandomPitch,
                                Volume = (sender as HorizontalFloatSlider).ScrollPosition
                            });
                        if (MathFunctions.RandEvent(0.15f))
                        {
                            SoundManager.PlaySound(level1.Key);
                        }
                    }
                });
            }
            Layout();

            base.Construct();
        }
    }
}
