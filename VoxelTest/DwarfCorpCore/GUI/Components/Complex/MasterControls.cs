using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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
    public class MasterControls : GUIComponent
    {
        public GameMaster Master { get; set; }
        public Dictionary<GameMaster.ToolMode, Button> ToolButtons { get; set; }
        public GameMaster.ToolMode CurrentMode { get; set; }
        public BuildMenu BuildPanel { get; set; }
        public Texture2D Icons { get; set; }
        public int IconSize { get; set; }

        public MasterControls(DwarfGUI gui, GUIComponent parent, GameMaster master, Texture2D icons, GraphicsDevice device, SpriteFont font) :
            base(gui, parent)
        {
            Master = master;
            Icons = icons;
            IconSize = 32;
            CurrentMode = master.CurrentToolMode;
            ToolButtons = new Dictionary<GameMaster.ToolMode, Button>();

            GridLayout layout = new GridLayout(GUI, this, 1, 7)
            {
                EdgePadding = 0
            };

            CreateButton(layout, GameMaster.ToolMode.SelectUnits, "Select", "Click and drag to select dwarves.", 5, 0);
            CreateButton(layout, GameMaster.ToolMode.Dig, "Mine", "Click and drag to designate mines.\nRight click to erase.", 0, 0);
            CreateButton(layout, GameMaster.ToolMode.Build, "Build", "Click to open build menu.", 2, 0);
            CreateButton(layout, GameMaster.ToolMode.Gather, "Gather", "Click on resources to designate them\nfor gathering. Right click to erase.", 6, 0);
            CreateButton(layout, GameMaster.ToolMode.Chop, "Chop", "Click on trees to designate them\nfor chopping. Right click to erase.", 1, 0);
            CreateButton(layout, GameMaster.ToolMode.Guard, "Guard", "Click and drag to designate guard areas.\nRight click to erase.", 4, 0);
            CreateButton(layout, GameMaster.ToolMode.Attack, "Attack", "Click and drag to attack entities.\nRight click to cancel.", 3, 0);
            //CreateButton(layout, GameMaster.ToolMode.CreateStockpiles, "Stock", "Click and drag to designate stockpiles.\nRight click to erase.", 7, 0);
          


            int i = 0;
            foreach(Button b in ToolButtons.Values)
            {
                layout.SetComponentPosition(b, i, 0, 1, 1);
                i++;
            }

      
        

            BuildPanel = new BuildMenu(GUI, GUI.RootComponent, Master)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width - 750, PlayState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350),
                IsVisible = false
            };
        }


        public Button CreateButton(GUIComponent parent, GameMaster.ToolMode mode, string name, string tooltip, int x, int y)
        {
            Button button = new Button(GUI, parent, name, GUI.DefaultFont, Button.ButtonMode.ImageButton, new ImageFrame(Icons, IconSize, x, y))
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

        /*
        private void buildBox_OnSelectionModified(string arg)
        {
            if(arg.Contains("Wall"))
            {
                string voxType = BuildPanel.CurrentWallType;
                if (string.IsNullOrEmpty(voxType)) return;

                Master.Faction.WallBuilder.CurrentVoxelType = VoxelLibrary.GetVoxelType(voxType);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                Master.Faction.RoomBuilder.CurrentRoomData = null;
                Master.Faction.CraftBuilder.IsEnabled = false;
            }
            else if (arg.Contains("Craft"))
            {
                CraftLibrary.CraftItemType itemType = BuildPanel.CurrentCraftType;
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                Master.Faction.RoomBuilder.CurrentRoomData = null;
                Master.Faction.WallBuilder.CurrentVoxelType = null;
                Master.Faction.CraftBuilder.CurrentCraftType = itemType;
                Master.Faction.CraftBuilder.IsEnabled = true;
            }
            else
            {
                if (string.IsNullOrEmpty(arg)) return;

                Master.Faction.RoomBuilder.CurrentRoomData = RoomLibrary.GetData(arg);
                Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                Master.Faction.WallBuilder.CurrentVoxelType = null;
                Master.Faction.CraftBuilder.IsEnabled = false;
            }
        }
         */

        public void ButtonClicked(Button sender)
        {
            sender.IsToggled = true;
            
            foreach(KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons)
            {
                if(pair.Value == sender)
                {
                    CurrentMode = pair.Key;

                   BuildPanel.IsVisible = CurrentMode == GameMaster.ToolMode.Build;
                    BuildPanel.LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width - 750,
                        PlayState.Game.GraphicsDevice.Viewport.Height - 512, 700, 350);
                }
                else
                {
                    pair.Value.IsToggled = false;
                }
            }
        }

        public bool SelectedUnitsHaveCapability(GameMaster.ToolMode tool)
        {
            return Master.Faction.SelectedMinions.Any(minion => minion.Stats.CurrentClass.HasAction(tool));
        }

        public override void Update(GameTime time)
        {

            if(Master.SelectedMinions.Count == 0)
            {

                if(Master.CurrentToolMode != GameMaster.ToolMode.God)
                {
                    Master.CurrentToolMode = GameMaster.ToolMode.SelectUnits;
                }


                foreach(KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = false;
                }

            }
            else
            {
     
                foreach(KeyValuePair<GameMaster.ToolMode, Button> pair in ToolButtons.Where(pair => pair.Key != GameMaster.ToolMode.SelectUnits))
                {
                    pair.Value.IsVisible = SelectedUnitsHaveCapability(pair.Key);
                }
                 
            }

            base.Update(time);
        }
    }

}