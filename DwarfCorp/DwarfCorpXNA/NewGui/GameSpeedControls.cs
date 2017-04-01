using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Gum;
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
        private Gum.Widget FastButton;
        private Gum.Widget SlowButton;
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
                TextColor = new Vector4(1, 1, 1, 1)
            });

            SlowButton = AddChild(new Widget()
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = "<<",
                TextColor = Color.White.ToVector4(),
                Tooltip = "Decrease Speed",
                HoverTextColor = Color.DarkRed.ToVector4(),
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    CurrentSpeed -= 1;
                },
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
            

            PlayPauseButton = AddChild(new Widget()
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                TextColor = Color.White.ToVector4(),
                Text = "||",
                Tooltip = "Pause",
                HoverTextColor = Color.DarkRed.ToVector4(),
                ChangeColorOnHover = true,
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

            FastButton = AddChild(new Widget()
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = ">>",
                TextColor = Color.White.ToVector4(),
                Tooltip = "Increase Speed",
                HoverTextColor = Color.DarkRed.ToVector4(),
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    CurrentSpeed += 1;
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

            if (_currentSpeed <= 0)
            {
                SlowButton.Hidden = true;
                SlowButton.Invalidate();
            }
            else
            {
                SlowButton.Hidden = false;
                SlowButton.Invalidate();;
            }

            if (_currentSpeed >= 3)
            {
                FastButton.Hidden = true;
                FastButton.Invalidate();
            }
            else
            {
                FastButton.Hidden = false;
                FastButton.Invalidate();
            }
        }
    }
}