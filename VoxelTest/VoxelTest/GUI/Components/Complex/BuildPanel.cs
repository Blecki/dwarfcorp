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
            
            GridLayout layout = new GridLayout(GUI, this,  1, roomTypes.Count + 1);


            int i = 0; 
            foreach(string s in roomTypes)
            {
                RoomType type = RoomLibrary.GetType(s);
                Button roomButton = new Button(GUI, layout, "", GUI.SmallFont, Button.ButtonMode.ImageButton, type.Icon)
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

            wallButton.OnClicked += () => roomButton_OnClicked(wallButton);
            Buttons["Wall"] = wallButton;
            layout.SetComponentPosition(wallButton, i, 0, 1, 1);

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

            combo.OnSelectionModified += combo_OnSelectionModified;

            layout.SetComponentPosition(combo, 0, 2, 3, 1);
        }

        void combo_OnSelectionModified(string arg)
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

            RoomType type = RoomLibrary.GetType(name);


            Label description = new Label(GUI, layout, type.Description, GUI.SmallFont)
            {
                TextColor = Color.White,
                StrokeColor = Color.Transparent,
                WordWrap = true
            };


            layout.SetComponentPosition(description, 0, 1, 3, 2);

            ImagePanel image = new ImagePanel(GUI, layout, type.Icon)
            {
                KeepAspectRatio = true
            };

            layout.SetComponentPosition(image, 3, 0, 1, 1);

            string requirementsText = "Requires (per tile):\n";

            foreach(KeyValuePair<string, ResourceAmount> pair in type.RequiredResources)
            {
                requirementsText += pair.Key + ": " + pair.Value.NumResources + "\n";
            }


            if(type.RequiredResources.Count == 0)
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
