using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BuildPanel : GUIComponent
    {
        public Dictionary<string, Button> Buttons { get; set; }
        public Faction Faction { get; set; }
        public Panel InfoPanel { get; set; }
        public string CurrentWallType { get; set; }
        public delegate void SelectionChanged(string selection);
        public event SelectionChanged OnSelectionChanged;
        public CraftLibrary.CraftItemType CurrentCraftType { get; set; }

        protected virtual void OnOnSelectionChanged(string selection)
        {
            SelectionChanged handler = OnSelectionChanged;
            if(handler != null)
            {
                handler(selection);
            }
        }


        public BuildPanel() 
            : base()
        {
            
        }

        public BuildPanel(DwarfGUI gui, GUIComponent parent, Faction faction) 
            : base(gui, parent)
        {
            CurrentWallType = "";
            Buttons = new Dictionary<string, Button>();
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            List<string> roomTypes = RoomLibrary.GetRoomTypes().ToList();
            
            GridLayout layout = new GridLayout(GUI, this,  1, roomTypes.Count + 2);


            int i = 0; 
            foreach(string s in roomTypes)
            {
                RoomData data = RoomLibrary.GetData(s);
                Button roomButton = new Button(GUI, layout, "", GUI.SmallFont, Button.ButtonMode.ImageButton, data.Icon)
                {
                    ToolTip = "Build a " + s,
                    KeepAspectRatio = true
                };

                roomButton.OnClicked += () => roomButton_OnClicked(roomButton);
                Buttons[s] = roomButton;

                layout.SetComponentPosition(roomButton, i, 0, 1, 1);
                i++;
            }

            Button wallButton = new Button(GUI, layout, "", GUI.SmallFont, Button.ButtonMode.ImageButton, new ImageFrame(roomIcons, 16, 1, 2))
            {
                ToolTip = "Build a wall",
                KeepAspectRatio = true
            };

            Button craftbutton = new Button(GUI, layout, "", GUI.SmallFont, Button.ButtonMode.ImageButton, new ImageFrame(roomIcons, 16, 2, 2))
            {
                ToolTip = "Craft items",
                KeepAspectRatio = true
            };

            wallButton.OnClicked += () => roomButton_OnClicked(wallButton);
            Buttons["Wall"] = wallButton;
            layout.SetComponentPosition(wallButton, i, 0, 1, 1);

            craftbutton.OnClicked += () => roomButton_OnClicked(craftbutton);
            Buttons["Craft"] = craftbutton;
            layout.SetComponentPosition(craftbutton, i + 1, 0, 1, 1);

            Faction = faction;

            InfoPanel = new Panel(GUI, this)
            {
                Mode = Panel.PanelMode.Simple
            };

        }

        string GetType(Button sender)
        {
            return (from button in Buttons
                where sender == button.Value
                select button.Key).FirstOrDefault();
        }

        public void SetupWallPanel(GridLayout layout)
        {
            Label description = new Label(GUI, layout, "Build a wall of type: ", GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent,
                WordWrap = true
            };


            layout.SetComponentPosition(description, 0, 1, 3, 1);

            ImagePanel image = new ImagePanel(GUI, layout, Buttons["Wall"].Image)
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(image, 3, 0, 1, 1);


            ComboBox combo = new ComboBox(GUI, layout);

            List<VoxelType> types = VoxelLibrary.GetTypes();
            foreach(VoxelType voxType in types)
            {
                if(voxType.IsBuildable)
                {
                    combo.AddValue(voxType.Name);
                }
            }

            combo.CurrentIndex = 0;
            combo.CurrentValue = combo.Values[0];
            CurrentWallType = combo.CurrentValue;

            combo.OnSelectionModified += wallcombo_OnSelectionModified;

            layout.SetComponentPosition(combo, 0, 2, 3, 1);
        }

        void wallcombo_OnSelectionModified(string arg)
        {
            CurrentWallType = arg;
            OnSelectionChanged("Wall");
        }

        public void SetupInfoPanel(string name)
        {
            InfoPanel.IsVisible = true;
            InfoPanel.ClearChildren();

            const int width = 256;
            const int height = 256;

            int globalX = GUI.RootComponent.LocalBounds.X + GameState.Game.GraphicsDevice.Viewport.Width - width;
            int globalY = GlobalBounds.Y - height - 5;

            int localX = globalX - GlobalBounds.X;
            int localY = globalY - GlobalBounds.Y;

            InfoPanel.LocalBounds = new Rectangle(localX, localY, width, height);

            GridLayout layout = new GridLayout(GUI, InfoPanel, 5, 4)
            {
                EdgePadding = 15
            };

            Label label = new Label(GUI, layout, " " + name, GUI.DefaultFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent
            };

            layout.SetComponentPosition(label, 0, 0, 3, 1);

            if(name == "Wall")
            {
                SetupWallPanel(layout);
                return;
            }
            else if (name == "Craft")
            {
                SetupCraftPanel(layout);
                return;
            }

            RoomData data = RoomLibrary.GetData(name);


            Label description = new Label(GUI, layout, data.Description, GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent,
                WordWrap = true
            };


            layout.SetComponentPosition(description, 0, 1, 3, 2);

            ImagePanel image = new ImagePanel(GUI, layout, data.Icon)
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(image, 3, 0, 1, 1);

            string requirementsText = "Requires (per 4 tiles):\n";

            foreach(KeyValuePair<ResourceLibrary.ResourceType, ResourceAmount> pair in data.RequiredResources)
            {
                requirementsText += pair.Key + ": " + pair.Value.NumResources + "\n";
            }


            if(data.RequiredResources.Count == 0)
            {
                requirementsText += "Nothing";
            }

            Label requirements = new Label(GUI, layout, requirementsText, GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent,
                WordWrap = true
            };

            layout.SetComponentPosition(requirements, 0, 3, 3, 2);

        }

        private void SetupCraftPanel(GridLayout layout)
        {
            Label description = new Label(GUI, layout, "Build a mechanism of type: ", GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent,
                WordWrap = true
            };

            layout.SetComponentPosition(description, 0, 1, 3, 1);

            ImagePanel image = new ImagePanel(GUI, layout, Buttons["Craft"].Image)
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(image, 3, 0, 1, 1);


            ComboBox combo = new ComboBox(GUI, layout);

            foreach (KeyValuePair<CraftLibrary.CraftItemType, CraftItem> craftitem in CraftLibrary.CraftItems)
            {
                combo.AddValue(craftitem.Value.Name);
            }    

            combo.CurrentIndex = 0;
            combo.CurrentValue = combo.Values[0];
            CurrentWallType = combo.CurrentValue;

            combo.OnSelectionModified += craftcombo_OnSelectionModified;

            layout.SetComponentPosition(combo, 0, 2, 3, 1);
        }

        private void craftcombo_OnSelectionModified(string arg)
        {
            CurrentCraftType = CraftLibrary.GetType(arg);
            OnSelectionChanged("Craft");

        }

        public void roomButton_OnClicked(Button sender)
        {
            sender.IsToggled = true;

            foreach(Button button in Buttons.Values.Where(button => button != sender))
            {
                button.IsToggled = false;
            }

            string selection = GetType(sender);
            OnSelectionChanged(selection);
            SetupInfoPanel(selection);
        }
    }
}
