using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp.NewGui
{
    public class GameSpeedControls : Gum.Widget
    {
        private int _currentSpeed = 1;
        public int CurrentSpeed
        {
            get { return _currentSpeed; }
            set
            {
                _currentSpeed = Math.Min(MaximumSpeed, Math.Max(value, 0));
                Root.SafeCall(this.OnSpeedChanged, this, _currentSpeed);
                UpdateLabels();
            }
        }

        public int MaximumSpeed = 3;
        public int PlaySpeed = 1;

        private Gum.Widget TimeLabel;
        private Gum.Widget PlayPauseButton;
        public Action<Gum.Widget, int> OnSpeedChanged;

        public void Pause()
        {
            PlaySpeed = Math.Max(CurrentSpeed, 1);
            CurrentSpeed = 0;
        }

        public void Resume()
        {
            CurrentSpeed = PlaySpeed;
        }

        public PlayState PlayState { get; set; }

        public override void Construct()
        {
            Border = "border-dark";
            MinimumSize = new Point(128, 40);
            Padding = new Gum.Margin(2, 2, 4, 4);
            Font = "font2x";

            TimeLabel = AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    MinimumSize = new Point(16, 0),
                    Text = "1x",
                    Tooltip = "Current Game Speed",
                    TextVerticalAlign = Gum.VerticalAlign.Center,
                    TextColor = new Vector4(1,1,1,1)
                });

            PlayPauseButton = AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Border = "border-thin",
                Text = "||",
                Tooltip = "Pause",
                OnClick = (sender, args) =>
                    {
                        if (CurrentSpeed == 0)
                        {
                            if (PlaySpeed == 0) PlaySpeed = 1;
                            CurrentSpeed = PlaySpeed;
                        }
                        else
                        {
                            PlaySpeed = CurrentSpeed;
                            CurrentSpeed = 0;
                        }
                    },
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
            
            AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = ">>",
                Border = "border-thin",
                Tooltip = "Increase Speed",
                OnClick = (sender, args) =>
                {
                    CurrentSpeed += 1;
                },
                TextVerticalAlign = Gum.VerticalAlign.Center
            });

            AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = "<<",
                Border = "border-thin",
                Tooltip = "Decrease Speed",
                OnClick = (sender, args) =>
                {
                    CurrentSpeed -= 1;
                },
                TextVerticalAlign = Gum.VerticalAlign.Center
            });


            base.Construct();
        }

        private void UpdateLabels()
        {
            TimeLabel.Text = String.Format("{0}x", _currentSpeed);
            TimeLabel.Invalidate();

            PlayPauseButton.Text = (_currentSpeed == 0 ? ">" : "||");
            PlayPauseButton.Tooltip = (_currentSpeed == 0 ? "Resume" : "Pause");
            PlayPauseButton.Invalidate();
        }
    }
}