// MasterControls.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

namespace DwarfCorp
{
    /// <summary>
    /// This is the GUI component responsible for deciding which tool
    /// the player is using.
    /// </summary>
    public class MasterControls : Tray
    {
        public GameMaster Master { get; set; }
        public Dictionary<GameMaster.ToolMode, Button> ToolButtons { get; set; }
        public GameMaster.ToolMode CurrentMode { get; set; }
        public Texture2D Icons { get; set; }
        public int IconSize { get; set; }
        public int NumRows { get; set; }
        public int NumColumns { get; set; }
        public GameSpeedControl SpeedButton { get; set; }

        public MasterControls(DwarfGUI gui, GUIComponent parent, GameMaster master, Texture2D icons, GraphicsDevice device, SpriteFont font) :
            base(gui, parent)
        {
            NumRows = 2;
            NumColumns = 5;
            TrayPosition = Position.BottomRight;
            Master = master;
            Icons = icons;
            IconSize = 32;
            CurrentMode = master.CurrentToolMode;
            ToolButtons = new Dictionary<GameMaster.ToolMode, Button>();

            GridLayout layout = new GridLayout(GUI, this, NumRows, NumColumns)
            {
                EdgePadding = 0
            };

            CreateButton(layout, GameMaster.ToolMode.SelectUnits, "Select", "Click and drag to select dwarves.", 5, 0);
            CreateButton(layout, GameMaster.ToolMode.Dig, "Mine", "Click and drag to designate mines.\nRight click to erase.", 0, 0);
            CreateButton(layout, GameMaster.ToolMode.Build, "Build", "Click to open build menu.", 2, 0);
            CreateButton(layout, GameMaster.ToolMode.Cook, "Cook", "Click to open cooking menu.", 3, 3);
            CreateButton(layout, GameMaster.ToolMode.Farm, "Farm", "Click to open farming menu.", 5, 1);
            CreateButton(layout, GameMaster.ToolMode.Magic, "Magic", "Click to open the magic menu.", 6, 1);
            CreateButton(layout, GameMaster.ToolMode.Gather, "Gather", "Click on resources to designate them\nfor gathering. Right click to erase.", 6, 0);
            CreateButton(layout, GameMaster.ToolMode.Chop, "Chop", "Click on trees to designate them\nfor chopping. Right click to erase.", 1, 0);
            CreateButton(layout, GameMaster.ToolMode.Guard, "Guard", "Click and drag to designate guard areas.\nRight click to erase.", 4, 0);
            CreateButton(layout, GameMaster.ToolMode.Attack, "Attack", "Click and drag to attack entities.\nRight click to cancel.", 3, 0);

            int i = 0;
            foreach (Button b in ToolButtons.Values)
            {
                layout.SetComponentPosition(b, i % NumColumns, i / NumColumns, 1, 1);
                i++;
            }
            SpeedButton = new GameSpeedControl(GUI, this)
            {
                LocalBounds = new Rectangle(-132, 75, 100, 20)
            };
        }


        public Button CreateButton(GUIComponent parent, GameMaster.ToolMode mode, string name, string tooltip, int x, int y)
        {
            Button button = new Button(GUI, parent, name, GUI.SmallFont, Button.ButtonMode.ImageButton, new ImageFrame(Icons, IconSize, x, y))
            {
                CanToggle = true,
                IsToggled = false,
                KeepAspectRatio = true,
                DontMakeBigger = true,
                ToolTip = tooltip,
                TextColor = Color.White,
                HoverTextColor = Color.Yellow,
                DrawFrame = true
            };
            button.OnClicked += () => ButtonClicked(button);
            ToolButtons[mode] = button;

            return button;
        }

        public void ButtonClicked(Button sender)
        {
            sender.IsToggled = true;

            foreach (KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons)
            {
                if (pair.Value == sender)
                {
                    CurrentMode = pair.Key;
                    Master.Tools[pair.Key].OnBegin();

                    if (Master.CurrentTool != Master.Tools[pair.Key])
                    {
                        Master.CurrentTool.OnEnd();
                    }


                }
                else
                {
                    pair.Value.IsToggled = false;
                }
            }
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            base.Render(time, batch);
            foreach (KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons)
            {
                if (!pair.Value.IsVisible)
                {
                    GUI.Skin.RenderButtonFrame(pair.Value.GetImageBounds(), batch);
                }
            }

        }

        public bool SelectedUnitsHaveCapability(GameMaster.ToolMode tool)
        {
            return Master.Faction.SelectedMinions.Any(minion => minion.Stats.CurrentClass.HasAction(tool));
        }

        public override void Update(DwarfTime time)
        {
            if (Master == null) return; // We are shutting down.

            if (Master.SelectedMinions.Count == 0)
            {

                if (Master.CurrentToolMode != GameMaster.ToolMode.God)
                {
                    Master.CurrentToolMode = GameMaster.ToolMode.SelectUnits;
                }


                foreach (KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = false;
                }

            }
            else
            {

                foreach (KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = SelectedUnitsHaveCapability(pair.Key);
                }

            }

            base.Update(time);
        }
    }

    /// <summary>
    /// This is a GUI component for controlling the game speed.
    /// </summary>
    public class GameSpeedControl : Panel
    {
        private readonly int[] _gameSpeeds = { 0, 1, 2, 3 };

        private readonly string[] _labels = {"||", "1x", "2x", "3x"};
        private readonly string[] _rightLabels = {">", ">>", ">>>", ""};
        private readonly string[] _leftLabels = {"", "||", "<", "<"};
        private readonly string[] _tooltips =
        {
            "Game paused.", "Game running at 1x speed.", "Game running at 2x speed.",
            "Game running at 3x speed."
        };

        private readonly string[] _leftTooltips =
        {
            "", "Pause game", "1x speed", "2x speed"
        };

        private readonly string[] _rightTooltips =
        {
            "1x speed", "2x speed", "3x speed", ""
        };
        private int _currSpeed = 0;

        private Button TimeForward { get; set; }
        private Button TimeBackward { get; set; }
        private Label TimeLabel { get; set; }

        public GameSpeedControl(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            Mode = PanelMode.Simple;
            GridLayout layout = new GridLayout(GUI, this, 1, 3);

            TimeLabel = new Label(GUI, layout, "", gui.SmallFont)
            {
                TextColor = Color.White,
                Alignment = Drawer2D.Alignment.Center
            };
            layout.SetComponentPosition(TimeLabel, 1, 0, 1, 1);

            TimeBackward = new Button(GUI, layout, "", gui.SmallFont, Button.ButtonMode.PushButton, null)
            {
                DrawFrame = false,
                TextColor = Color.White
            };
            layout.SetComponentPosition(TimeBackward, 0, 0, 1, 1);

            TimeForward = new Button(GUI, layout, "", gui.SmallFont, Button.ButtonMode.PushButton, null)
            {
                DrawFrame = false,
                TextColor = Color.White
            };
            layout.SetComponentPosition(TimeForward, 2, 0, 1, 1);

            TimeForward.OnClicked += () => SetSpeed(_currSpeed + 1);
            TimeBackward.OnClicked += () => SetSpeed(_currSpeed - 1);
            SetSpeed(1);
            layout.UpdateSizes();
        }

        public void IncrementSpeed()
        {
            SetSpeed(_currSpeed + 1);
        }

        public void DecrementSpeed()
        {
            SetSpeed(_currSpeed - 1);
        }

        public void SetSpecialSpeed(int multiplier)
        {
            TimeForward.IsVisible = false;
            TimeBackward.IsVisible = false;
            DwarfTime.LastTime.Speed = (float) multiplier;
            WorldManager.Paused = false;
            TimeLabel.Text = multiplier + "x";
            TimeLabel.ToolTip = "Game is running at " + multiplier + "x speed";
        }

        /// <summary>
        /// Sets the speed to the given index.
        /// </summary>
        /// <param name="idx">The index into GameSpeeds.</param>
        public void SetSpeed(int idx)
        {
            _currSpeed = Math.Max(Math.Min(idx, _gameSpeeds.Length - 1), 0);
            TimeLabel.Text = _labels[_currSpeed];
            TimeLabel.ToolTip = _tooltips[_currSpeed];
            TimeBackward.Text = _leftLabels[_currSpeed];
            TimeBackward.ToolTip = _leftTooltips[_currSpeed];
            TimeForward.Text = _rightLabels[_currSpeed];
            TimeForward.ToolTip = _rightTooltips[_currSpeed];
            TimeBackward.IsVisible = true;
            TimeForward.IsVisible = true;
            if (_currSpeed == 0)
            {
                TimeBackward.IsVisible = false;
            }
            else if (_currSpeed == _gameSpeeds.Length - 1)
            {
                TimeForward.IsVisible = false;
            }

            DwarfTime.LastTime.Speed = (float)_gameSpeeds[_currSpeed];
            PlayState.Paused = _gameSpeeds[_currSpeed] == 0;
        }

    }

}