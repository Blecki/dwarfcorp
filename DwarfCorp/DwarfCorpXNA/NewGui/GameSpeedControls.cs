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
        public int CurrentSpeed = 1;
        public int MaximumSpeed = 3;
        public int PlaySpeed = 1;

        private Gum.Widget TimeLabel;

        public override void Construct()
        {
            Border = "border-button";
            MinimumSize = new Point(128, 30);
            Padding = new Gum.Margin(4, 4, 4, 4);

            TimeLabel = AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    MinimumSize = new Point(16, 0),
                    Text = "1x"
                });

            AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = "||",
                OnClick = (sender, args) =>
                    {
                        PlaySpeed = CurrentSpeed;
                        SetGameSpeed(0);
                    }
            });

            AddChild(new Gum.Widget
                {
                    AutoLayout = Gum.AutoLayout.DockLeft,
                    Text = ">",
                    OnClick = (sender, args) =>
                        {
                            if (PlaySpeed == 0) PlaySpeed = 1;
                            SetGameSpeed(PlaySpeed);
                        }
                });

            AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = ">>",
                OnClick = (sender, args) =>
                {
                    SetGameSpeed(CurrentSpeed + 1);
                }
            });

            AddChild(new Gum.Widget
            {
                AutoLayout = Gum.AutoLayout.DockLeft,
                Text = "<<",
                OnClick = (sender, args) =>
                {
                    SetGameSpeed(CurrentSpeed - 1);
                }
            });


            base.Construct();
        }

        // Todo: This doesn't belong here. Actual manipulation of the speed should not be handled by the gui.
        private void SetGameSpeed(int NewSpeed)
        {
            CurrentSpeed = Math.Min(MaximumSpeed, Math.Max(NewSpeed, 0));
            TimeLabel.Text = String.Format("{0}x", CurrentSpeed);
            TimeLabel.Invalidate();

            DwarfTime.LastTime.Speed = (float)CurrentSpeed;
            PlayState.Paused = CurrentSpeed == 0;
        }
    }
}