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
            set { SetGameSpeed(value); }
        }

        public int MaximumSpeed = 3;
        public int PlaySpeed = 1;

        private Gum.Widget TimeLabel;
        private Gum.Widget PlayPauseButton;

        public override void Construct()
        {
            Border = "border-dark";
            MinimumSize = new Point(128, 36);
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
                        if (PlayState.Paused)
                        {
                            if (PlaySpeed == 0) PlaySpeed = 1;
                            SetGameSpeed(PlaySpeed);
                        }
                        else
                        {
                            PlaySpeed = CurrentSpeed;
                            SetGameSpeed(0);
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
                    SetGameSpeed(CurrentSpeed + 1);
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
                    SetGameSpeed(CurrentSpeed - 1);
                },
                TextVerticalAlign = Gum.VerticalAlign.Center
            });


            base.Construct();
        }

        // Todo: This doesn't belong here. Actual manipulation of the speed should not be handled by the gui.
        private void SetGameSpeed(int NewSpeed)
        {
            _currentSpeed = Math.Min(MaximumSpeed, Math.Max(NewSpeed, 0));
            TimeLabel.Text = String.Format("{0}x", _currentSpeed);
            TimeLabel.Invalidate();

            PlayPauseButton.Text = (_currentSpeed == 0 ? ">" : "||");
            PlayPauseButton.Tooltip = (_currentSpeed == 0 ? "Resume" : "Pause");
            PlayPauseButton.Invalidate();

            DwarfTime.LastTime.Speed = (float)_currentSpeed;
            PlayState.Paused = _currentSpeed == 0;
        }
    }
}