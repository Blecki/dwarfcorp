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

    public class MasterControls : SillyGUIComponent
    {
        public GameMaster Master { get; set; }
        public Dictionary<GameMaster.ToolMode, Button> ToolButtons { get; set; }
        public GameMaster.ToolMode CurrentMode { get; set; }
        public Panel BuildPanel { get; set; }


        public MasterControls(SillyGUI gui, SillyGUIComponent parent, GameMaster master, Texture2D icons, GraphicsDevice device, SpriteFont font) :
            base(gui, parent)
        {
            int iconSize = 32;
            CurrentMode = master.CurrentTool;
            ToolButtons = new Dictionary<GameMaster.ToolMode, Button>();

            Button mineButton = new Button(GUI, this, "Mine", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 0, 0));
            mineButton.CanToggle = true;
            mineButton.LocalBounds = new Rectangle(device.Viewport.Width - 100 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            mineButton.IsToggled = true;
            mineButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(mineButton); });

            Button chopButton = new Button(GUI, this, "Chop", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 1, 0));
            chopButton.CanToggle = true;
            chopButton.LocalBounds = new Rectangle(device.Viewport.Width - 160 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            chopButton.IsToggled = false;
            chopButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(chopButton); });

            Button guardButton = new Button(GUI, this, "Guard", font, Button.ButtonMode.ImageButton,new ImageFrame(icons, iconSize, 4, 0));
            guardButton.CanToggle = true;
            guardButton.LocalBounds = new Rectangle(device.Viewport.Width - 220 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            guardButton.IsToggled = false;
            guardButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(guardButton); });


            Button stockButton = new Button(GUI, this, "Stock", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 7, 0));
            stockButton.CanToggle = true;
            stockButton.LocalBounds = new Rectangle(device.Viewport.Width - 280 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            stockButton.IsToggled = false;
            stockButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(stockButton); });


            Button gatherButton = new Button(GUI, this, "Gather", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 6, 0));
            gatherButton.CanToggle = true;
            gatherButton.LocalBounds = new Rectangle(device.Viewport.Width - 340 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            gatherButton.IsToggled = false;
            gatherButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(gatherButton); });


            Button buildButton = new Button(GUI, this, "Build", font, Button.ButtonMode.ImageButton, new ImageFrame(icons, iconSize, 2, 0));
            buildButton.CanToggle = true;
            buildButton.LocalBounds = new Rectangle(device.Viewport.Width - 400 + 45, device.Viewport.Height - 100, iconSize, iconSize);
            buildButton.IsToggled = false;
            buildButton.OnClicked += new ClickedDelegate(delegate { ButtonClicked(buildButton); });

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

            foreach (string room in RoomLibrary.GetRoomTypes())
            {
                buildBox.AddValue(room);
            }

            foreach (VoxelType vox in VoxelLibrary.PrimitiveMap.Keys)
            {
                if (vox.isBuildable)
                {
                    buildBox.AddValue(vox.name + " Wall");
                }
            }

            buildBox.OnSelectionModified += new ComboBoxSelector.Modified(buildBox_OnSelectionModified);

            BuildPanel.IsVisible = false;

        }

        void buildBox_OnSelectionModified(string arg)
        {
            if (arg.Contains("Wall"))
            {
                string voxType = arg.Substring(0, arg.Length - "Wall".Length - 1);
                Master.PutDesignator.CurrentVoxelType = VoxelLibrary.GetVoxelType(voxType);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                Master.RoomDesignator.CurrentRoomType = null;
            }
            else if (arg != "") 
            {
                Master.RoomDesignator.CurrentRoomType = RoomLibrary.GetType(arg);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                Master.PutDesignator.CurrentVoxelType = null;
            }
        }

        public void ButtonClicked(Button sender)
        {
            sender.IsToggled = true;

            foreach (KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons)
            {
                if (pair.Value == sender)
                {
                    CurrentMode = pair.Key;

                    if (CurrentMode == GameMaster.ToolMode.Build)
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
