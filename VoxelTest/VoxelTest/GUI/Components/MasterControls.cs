using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    /// <summary>
    /// This is the GUI component responsible for deciding which tool
    /// the player is using.
    /// </summary>
    public class MasterControls : GUIComponent
    {
        public GameMaster Master { get; set; }
        public Dictionary<GameMaster.ToolMode, Button> ToolButtons { get; set; }
        public GameMaster.ToolMode CurrentMode { get; set; }
        public Panel BuildPanel { get; set; }


        public MasterControls(DwarfGUI gui, GUIComponent parent, GameMaster master, Texture2D icons, GraphicsDevice device, SpriteFont font) :
            base(gui, parent)
        {
            int iconSize = 32;
            int buttonSize = 52;
            CurrentMode = master.CurrentToolMode;
            ToolButtons = new Dictionary<GameMaster.ToolMode, Button>();

            Button mineButton = new Button(GUI, this, "Mine", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 0, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 100 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = true,
                KeepAspectRatio = true,
                ConstrainSize = true,
                ToolTip = "Click and drag to designate mines.\nRight click to erase."
            };
            mineButton.OnClicked += () => ButtonClicked(mineButton);

            Button chopButton = new Button(GUI, this, "Chop", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 1, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 160 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = false,
                ConstrainSize = true,
                KeepAspectRatio = true,
                ToolTip = "Click on trees to designate them\nfor chopping. Right click to erase."
            };
            chopButton.OnClicked += () => ButtonClicked(chopButton);

            Button guardButton = new Button(GUI, this, "Guard", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 4, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 220 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = false,
                ConstrainSize = true,
                KeepAspectRatio = true,
                ToolTip = "Click and drag to designate guard areas.\nRight click to erase."
            };
            guardButton.OnClicked += () => ButtonClicked(guardButton);


            Button stockButton = new Button(GUI, this, "Stock", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 7, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 280 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = false,
                KeepAspectRatio = true,
                ConstrainSize = true,
                ToolTip = "Click and drag to designate stockpiles.\nRight click to erase."
            };
            stockButton.OnClicked += () => ButtonClicked(stockButton);


            Button gatherButton = new Button(GUI, this, "Gather", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 6, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 340 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = false,
                ConstrainSize = true,
                KeepAspectRatio = true,
                ToolTip = "Click on resources to designate them\nfor gathering. Right click to erase."
            };
            gatherButton.OnClicked += () => ButtonClicked(gatherButton);


            Button buildButton = new Button(GUI, this, "Build", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 2, 0))
            {
                CanToggle = true,
                LocalBounds = new Rectangle(device.Viewport.Width - 400 + 45, device.Viewport.Height - 100, buttonSize, buttonSize),
                IsToggled = false,
                KeepAspectRatio = true,
                ConstrainSize = true,
                ToolTip = "Click to open build menu."
            };
            buildButton.OnClicked += () => ButtonClicked(buildButton);

            ToolButtons[GameMaster.ToolMode.Dig] = mineButton;
            ToolButtons[GameMaster.ToolMode.Chop] = chopButton;
            ToolButtons[GameMaster.ToolMode.Guard] = guardButton;
            ToolButtons[GameMaster.ToolMode.CreateStockpiles] = stockButton;
            ToolButtons[GameMaster.ToolMode.Gather] = gatherButton;
            ToolButtons[GameMaster.ToolMode.Build] = buildButton;

            foreach(Button b in ToolButtons.Values)
            {
                b.TextColor = Color.White;
                b.HoverTextColor = Color.Yellow;
            }

            BuildPanel = new Panel(gui, Parent);
            GroupBox buildGroup = new GroupBox(gui, BuildPanel, "Build");
            BuildPanel.LocalBounds = new Rectangle(device.Viewport.Width - 300, 32, 225, 150);
            buildGroup.LocalBounds = new Rectangle(5, 5, 200, 125);
            ComboBox buildBox = new ComboBox(gui, buildGroup);
            buildBox.LocalBounds = new Rectangle(2, 20, 180, 40);

            foreach(string room in RoomLibrary.GetRoomTypes())
            {
                buildBox.AddValue(room);
            }

            foreach(VoxelType vox in VoxelLibrary.PrimitiveMap.Keys)
            {
                if(vox.IsBuildable)
                {
                    buildBox.AddValue(vox.Name + " Wall");
                }
            }

            buildBox.OnSelectionModified += buildBox_OnSelectionModified;

            BuildPanel.IsVisible = false;
        }

        private void buildBox_OnSelectionModified(string arg)
        {
            if(arg.Contains("Wall"))
            {
                string voxType = arg.Substring(0, arg.Length - "Wall".Length - 1);
                Master.Faction.PutDesignator.CurrentVoxelType = VoxelLibrary.GetVoxelType(voxType);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                Master.Faction.RoomDesignator.CurrentRoomType = null;
            }
            else if(arg != "")
            {
                Master.Faction.RoomDesignator.CurrentRoomType = RoomLibrary.GetType(arg);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                Master.Faction.PutDesignator.CurrentVoxelType = null;
            }
        }

        public void ButtonClicked(Button sender)
        {
            sender.IsToggled = true;

            foreach(KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons)
            {
                if(pair.Value == sender)
                {
                    CurrentMode = pair.Key;

                    if(CurrentMode == GameMaster.ToolMode.Build)
                    {
                        BuildPanel.IsVisible = true;
                    }
                    else
                    {
                        BuildPanel.IsVisible = false;
                    }
                }
                else
                {
                    pair.Value.IsToggled = false;
                }
            }
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }
    }

}