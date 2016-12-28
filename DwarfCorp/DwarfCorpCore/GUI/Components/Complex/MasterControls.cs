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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This is the GUI component responsible for deciding which tool
    ///     the player is using.
    /// </summary>
    public class MasterControls : Tray
    {
        public MasterControls(DwarfGUI gui, GUIComponent parent, GameMaster master, Texture2D icons,
            GraphicsDevice device, SpriteFont font) :
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

            var layout = new GridLayout(GUI, this, NumRows, NumColumns)
            {
                EdgePadding = 0
            };

            CreateButton(layout, GameMaster.ToolMode.SelectUnits, "Select", "Click and drag to select dwarves.", 5, 0);
            CreateButton(layout, GameMaster.ToolMode.Dig, "Mine",
                "Click and drag to designate mines.\nRight click to erase.", 0, 0);
            CreateButton(layout, GameMaster.ToolMode.Build, "Build", "Click to open build menu.", 2, 0);
            CreateButton(layout, GameMaster.ToolMode.Cook, "Cook", "Click to open cooking menu.", 3, 3);
            CreateButton(layout, GameMaster.ToolMode.Farm, "Farm", "Click to open farming menu.", 5, 1);
            CreateButton(layout, GameMaster.ToolMode.Magic, "Magic", "Click to open the magic menu.", 6, 1);
            CreateButton(layout, GameMaster.ToolMode.Gather, "Gather",
                "Click on resources to designate them\nfor gathering. Right click to erase.", 6, 0);
            CreateButton(layout, GameMaster.ToolMode.Chop, "Chop",
                "Click on trees to designate them\nfor chopping. Right click to erase.", 1, 0);
            CreateButton(layout, GameMaster.ToolMode.Guard, "Guard",
                "Click and drag to designate guard areas.\nRight click to erase.", 4, 0);
            CreateButton(layout, GameMaster.ToolMode.Attack, "Attack",
                "Click and drag to attack entities.\nRight click to cancel.", 3, 0);

            int i = 0;
            foreach (Button b in ToolButtons.Values)
            {
                layout.SetComponentPosition(b, i%NumColumns, i/NumColumns, 1, 1);
                i++;
            }
        }

        public GameMaster Master { get; set; }
        public Dictionary<GameMaster.ToolMode, Button> ToolButtons { get; set; }
        public GameMaster.ToolMode CurrentMode { get; set; }
        public Texture2D Icons { get; set; }
        public int IconSize { get; set; }
        public int NumRows { get; set; }
        public int NumColumns { get; set; }


        public Button CreateButton(GUIComponent parent, GameMaster.ToolMode mode, string name, string tooltip, int x,
            int y)
        {
            var button = new Button(GUI, parent, name, GUI.SmallFont, Button.ButtonMode.ImageButton,
                new ImageFrame(Icons, IconSize, x, y))
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

            foreach (var pair in ToolButtons)
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
            foreach (var pair in ToolButtons)
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
            if (Master.SelectedMinions.Count == 0)
            {
                if (Master.CurrentToolMode != GameMaster.ToolMode.God)
                {
                    Master.CurrentToolMode = GameMaster.ToolMode.SelectUnits;
                }


                foreach (var pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = false;
                }
            }
            else
            {
                foreach (var pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = SelectedUnitsHaveCapability(pair.Key);
                }
            }

            base.Update(time);
        }
    }
}